using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectLink.EditorTools
{
    public sealed class ProjectLinkUIImageResourceExtractor : EditorWindow
    {
        const string DefaultOutputFolder = "Assets/Resources/UI";
        const int PreviewSize = 96;

        Texture2D sourceTexture;
        Texture2D processedPreview;
        readonly List<ExtractedResource> resources = new();
        string sourcePath = string.Empty;
        string outputFolder = DefaultOutputFolder;
        string baseFileName = "ui_resource";
        string status = "No image processed.";
        Vector2 scroll;
        bool sourceIsExternal;
        bool removeBackground = true;
        int backgroundTolerance = 32;
        int alphaThreshold = 8;
        int minPixelArea = 16;
        int padding = 1;

        sealed class ExtractedResource
        {
            public RectInt Bounds { get; set; }
            public Texture2D Texture { get; set; }
            public int PixelArea { get; set; }
        }

        [MenuItem("Tools/Project Link/UI Assets/Image Resource Extractor")]
        public static void Open()
        {
            GetWindow<ProjectLinkUIImageResourceExtractor>("UI Resource Extractor");
        }

        void OnDisable()
        {
            ClearGeneratedTextures();
            if (sourceIsExternal && sourceTexture != null)
                DestroyImmediate(sourceTexture);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var selectedTexture = (Texture2D)EditorGUILayout.ObjectField("UI Image", sourceTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
                SetSourceTexture(selectedTexture, AssetDatabase.GetAssetPath(selectedTexture), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load PNG/JPG"))
                    LoadImageFile();

                using (new EditorGUI.DisabledScope(sourceTexture == null))
                {
                    if (GUILayout.Button("Analyze"))
                        AnalyzeSource();
                }
            }

            if (!string.IsNullOrEmpty(sourcePath))
                EditorGUILayout.LabelField("Path", sourcePath);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Processing", EditorStyles.boldLabel);
            removeBackground = EditorGUILayout.Toggle("Remove Background", removeBackground);
            backgroundTolerance = EditorGUILayout.IntSlider("Background Tolerance", backgroundTolerance, 0, 96);
            alphaThreshold = EditorGUILayout.IntSlider("Alpha Threshold", alphaThreshold, 0, 255);
            minPixelArea = EditorGUILayout.IntField("Min Pixel Area", Mathf.Max(1, minPixelArea));
            padding = EditorGUILayout.IntSlider("Padding", padding, 0, 16);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                outputFolder = EditorGUILayout.TextField("Folder", outputFolder);
                if (GUILayout.Button("Select", GUILayout.Width(72f)))
                    SelectOutputFolder();
            }

            baseFileName = EditorGUILayout.TextField("Base File Name", baseFileName);

            using (new EditorGUI.DisabledScope(resources.Count == 0))
            {
                if (GUILayout.Button($"Save {resources.Count} Resource(s)"))
                    SaveResources();
            }

            EditorGUILayout.HelpBox(status, MessageType.Info);
            DrawPreview();
        }

        void LoadImageFile()
        {
            string path = EditorUtility.OpenFilePanel("Select UI image", Application.dataPath, "png,jpg,jpeg");
            if (string.IsNullOrEmpty(path))
                return;

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileNameWithoutExtension(path),
                hideFlags = HideFlags.HideAndDontSave
            };

            if (!ImageConversion.LoadImage(texture, bytes))
            {
                DestroyImmediate(texture);
                status = "Failed to load image file.";
                return;
            }

            SetSourceTexture(texture, path, true);
            AnalyzeSource();
        }

        void SetSourceTexture(Texture2D texture, string path, bool external)
        {
            if (sourceIsExternal && sourceTexture != null && sourceTexture != texture)
                DestroyImmediate(sourceTexture);

            sourceTexture = texture;
            sourcePath = path ?? string.Empty;
            sourceIsExternal = external;
            ClearGeneratedTextures();
            status = texture == null ? "No image selected." : "Image selected. Click Analyze.";
        }

        void AnalyzeSource()
        {
            if (sourceTexture == null)
            {
                status = "Select a UI image first.";
                return;
            }

            ClearGeneratedTextures();
            processedPreview = CreateReadableCopy(sourceTexture);
            processedPreview.name = $"{sourceTexture.name}_processed";
            processedPreview.hideFlags = HideFlags.HideAndDontSave;

            if (removeBackground && !HasTransparentBackground(processedPreview))
                RemoveEdgeConnectedBackground(processedPreview, backgroundTolerance);

            var extracted = ExtractResources(processedPreview);
            extracted.Sort(CompareResourceOrder);
            resources.AddRange(extracted);

            status = resources.Count == 0
                ? "No resource elements found. Lower Alpha Threshold or Min Pixel Area, then Analyze again."
                : $"Found {resources.Count} resource element(s).";
        }

        void DrawPreview()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            if (processedPreview != null)
            {
                Rect rect = GUILayoutUtility.GetRect(160f, 160f, GUILayout.ExpandWidth(false));
                EditorGUI.DrawTextureTransparent(rect, processedPreview, ScaleMode.ScaleToFit);
            }

            if (resources.Count == 0)
                return;

            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < resources.Count; i++)
            {
                ExtractedResource resource = resources[i];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
                    EditorGUI.DrawTextureTransparent(previewRect, resource.Texture, ScaleMode.ScaleToFit);

                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField($"{SanitizeFileName(baseFileName)}_{i + 1}.png", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Bounds: x={resource.Bounds.x}, y={resource.Bounds.y}, w={resource.Bounds.width}, h={resource.Bounds.height}");
                        EditorGUILayout.LabelField($"Pixels: {resource.PixelArea}");
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void SaveResources()
        {
            string cleanBaseName = SanitizeFileName(baseFileName);
            if (string.IsNullOrEmpty(cleanBaseName))
            {
                status = "Base File Name is empty or invalid.";
                return;
            }

            string absoluteFolder = ResolveOutputFolder(outputFolder);
            if (string.IsNullOrEmpty(absoluteFolder))
            {
                status = "Output folder is invalid.";
                return;
            }

            Directory.CreateDirectory(absoluteFolder);
            var importedAssetPaths = new List<string>();

            for (int i = 0; i < resources.Count; i++)
            {
                string fileName = $"{cleanBaseName}_{i + 1}.png";
                string absolutePath = Path.Combine(absoluteFolder, fileName);
                File.WriteAllBytes(absolutePath, resources[i].Texture.EncodeToPNG());

                string assetPath = ToProjectAssetPath(absolutePath);
                if (!string.IsNullOrEmpty(assetPath))
                    importedAssetPaths.Add(assetPath);
            }

            AssetDatabase.Refresh();
            foreach (string assetPath in importedAssetPaths)
                ConfigurePngAsSprite(assetPath);

            status = $"Saved {resources.Count} file(s) to {NormalizePath(absoluteFolder)}.";
        }

        static Texture2D CreateReadableCopy(Texture2D texture)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(texture, renderTexture);
            RenderTexture.active = renderTexture;

            var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            copy.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return copy;
        }

        static bool HasTransparentBackground(Texture2D texture)
        {
            const byte transparentAlpha = 16;
            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;
            int transparentEdgePixels = 0;
            int edgePixels = (width + height) * 2 - 4;

            for (int x = 0; x < width; x++)
            {
                if (pixels[x].a <= transparentAlpha)
                    transparentEdgePixels++;
                if (pixels[(height - 1) * width + x].a <= transparentAlpha)
                    transparentEdgePixels++;
            }

            for (int y = 1; y < height - 1; y++)
            {
                if (pixels[y * width].a <= transparentAlpha)
                    transparentEdgePixels++;
                if (pixels[y * width + width - 1].a <= transparentAlpha)
                    transparentEdgePixels++;
            }

            return transparentEdgePixels >= edgePixels / 3;
        }

        static void RemoveEdgeConnectedBackground(Texture2D texture, int tolerance)
        {
            int width = texture.width;
            int height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            Color32[] backgroundSamples = GetBackgroundSamples(pixels, width, height);
            bool[] visited = new bool[pixels.Length];
            var queue = new Queue<int>();

            for (int x = 0; x < width; x++)
            {
                TryEnqueueBackground(x, 0, width, pixels, visited, queue, backgroundSamples, tolerance);
                TryEnqueueBackground(x, height - 1, width, pixels, visited, queue, backgroundSamples, tolerance);
            }

            for (int y = 1; y < height - 1; y++)
            {
                TryEnqueueBackground(0, y, width, pixels, visited, queue, backgroundSamples, tolerance);
                TryEnqueueBackground(width - 1, y, width, pixels, visited, queue, backgroundSamples, tolerance);
            }

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                Color32 transparent = pixels[index];
                transparent.a = 0;
                pixels[index] = transparent;

                if (x > 0)
                    TryEnqueueBackground(x - 1, y, width, pixels, visited, queue, backgroundSamples, tolerance);
                if (x < width - 1)
                    TryEnqueueBackground(x + 1, y, width, pixels, visited, queue, backgroundSamples, tolerance);
                if (y > 0)
                    TryEnqueueBackground(x, y - 1, width, pixels, visited, queue, backgroundSamples, tolerance);
                if (y < height - 1)
                    TryEnqueueBackground(x, y + 1, width, pixels, visited, queue, backgroundSamples, tolerance);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        static Color32[] GetBackgroundSamples(Color32[] pixels, int width, int height)
        {
            Color32 bottomLeft = pixels[0];
            Color32 bottomRight = pixels[width - 1];
            Color32 topLeft = pixels[(height - 1) * width];
            Color32 topRight = pixels[(height - 1) * width + width - 1];
            var average = new Color32(
                (byte)((bottomLeft.r + bottomRight.r + topLeft.r + topRight.r) / 4),
                (byte)((bottomLeft.g + bottomRight.g + topLeft.g + topRight.g) / 4),
                (byte)((bottomLeft.b + bottomRight.b + topLeft.b + topRight.b) / 4),
                255);

            return new[] { bottomLeft, bottomRight, topLeft, topRight, average };
        }

        static void TryEnqueueBackground(
            int x,
            int y,
            int width,
            Color32[] pixels,
            bool[] visited,
            Queue<int> queue,
            Color32[] backgroundSamples,
            int tolerance)
        {
            int index = y * width + x;
            if (visited[index])
                return;

            visited[index] = true;
            if (IsBackgroundColor(pixels[index], backgroundSamples, tolerance))
                queue.Enqueue(index);
        }

        static bool IsBackgroundColor(Color32 pixel, Color32[] backgroundSamples, int tolerance)
        {
            int toleranceSquared = tolerance * tolerance;
            for (int i = 0; i < backgroundSamples.Length; i++)
            {
                if (ColorDistanceSquared(pixel, backgroundSamples[i]) <= toleranceSquared)
                    return true;
            }

            return false;
        }

        static int ColorDistanceSquared(Color32 a, Color32 b)
        {
            int dr = a.r - b.r;
            int dg = a.g - b.g;
            int db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        List<ExtractedResource> ExtractResources(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            bool[] visited = new bool[pixels.Length];
            var extracted = new List<ExtractedResource>();
            var queue = new Queue<int>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (visited[index] || pixels[index].a <= alphaThreshold)
                        continue;

                    RectInt bounds = FloodFillComponent(x, y, width, height, pixels, visited, queue, out int area);
                    if (area < minPixelArea)
                        continue;

                    bounds = AddPadding(bounds, width, height, padding);
                    extracted.Add(new ExtractedResource
                    {
                        Bounds = bounds,
                        PixelArea = area,
                        Texture = CropTexture(texture, pixels, bounds)
                    });
                }
            }

            return extracted;
        }

        RectInt FloodFillComponent(
            int startX,
            int startY,
            int width,
            int height,
            Color32[] pixels,
            bool[] visited,
            Queue<int> queue,
            out int area)
        {
            int minX = startX;
            int maxX = startX;
            int minY = startY;
            int maxY = startY;
            area = 0;

            queue.Clear();
            EnqueueOpaque(startX, startY, width, pixels, visited, queue);

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int x = index % width;
                int y = index / width;
                area++;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);

                if (x > 0)
                    TryEnqueueOpaque(x - 1, y, width, pixels, visited, queue);
                if (x < width - 1)
                    TryEnqueueOpaque(x + 1, y, width, pixels, visited, queue);
                if (y > 0)
                    TryEnqueueOpaque(x, y - 1, width, pixels, visited, queue);
                if (y < height - 1)
                    TryEnqueueOpaque(x, y + 1, width, pixels, visited, queue);
            }

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        void TryEnqueueOpaque(
            int x,
            int y,
            int width,
            Color32[] pixels,
            bool[] visited,
            Queue<int> queue)
        {
            int index = y * width + x;
            if (visited[index] || pixels[index].a <= alphaThreshold)
                return;

            EnqueueOpaque(x, y, width, pixels, visited, queue);
        }

        static void EnqueueOpaque(
            int x,
            int y,
            int width,
            Color32[] pixels,
            bool[] visited,
            Queue<int> queue)
        {
            int index = y * width + x;
            visited[index] = true;
            queue.Enqueue(index);
        }

        static RectInt AddPadding(RectInt bounds, int width, int height, int amount)
        {
            int xMin = Mathf.Max(0, bounds.xMin - amount);
            int yMin = Mathf.Max(0, bounds.yMin - amount);
            int xMax = Mathf.Min(width, bounds.xMax + amount);
            int yMax = Mathf.Min(height, bounds.yMax + amount);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        static Texture2D CropTexture(Texture2D source, Color32[] sourcePixels, RectInt bounds)
        {
            var texture = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false)
            {
                name = $"{source.name}_{bounds.x}_{bounds.y}",
                hideFlags = HideFlags.HideAndDontSave
            };

            var cropped = new Color32[bounds.width * bounds.height];
            for (int y = 0; y < bounds.height; y++)
            {
                int sourceOffset = (bounds.y + y) * source.width + bounds.x;
                int targetOffset = y * bounds.width;
                Array.Copy(sourcePixels, sourceOffset, cropped, targetOffset, bounds.width);
            }

            texture.SetPixels32(cropped);
            texture.Apply();
            return texture;
        }

        static int CompareResourceOrder(ExtractedResource left, ExtractedResource right)
        {
            int topCompare = right.Bounds.yMax.CompareTo(left.Bounds.yMax);
            return topCompare != 0 ? topCompare : left.Bounds.xMin.CompareTo(right.Bounds.xMin);
        }

        static void ConfigurePngAsSprite(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        void SelectOutputFolder()
        {
            string initialFolder = ResolveOutputFolder(outputFolder);
            if (string.IsNullOrEmpty(initialFolder) || !Directory.Exists(initialFolder))
                initialFolder = Application.dataPath;

            string selected = EditorUtility.OpenFolderPanel("Select output folder", initialFolder, string.Empty);
            if (string.IsNullOrEmpty(selected))
                return;

            outputFolder = ToProjectAssetPath(selected);
            if (string.IsNullOrEmpty(outputFolder))
                outputFolder = NormalizePath(selected);
        }

        static string ResolveOutputFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return string.Empty;

            string normalized = folder.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(normalized))
                return Path.GetFullPath(normalized);

            string projectRoot = GetProjectRoot();
            return Path.GetFullPath(Path.Combine(projectRoot, normalized));
        }

        static string ToProjectAssetPath(string absolutePath)
        {
            string projectRoot = NormalizePath(GetProjectRoot());
            string normalizedPath = NormalizePath(Path.GetFullPath(absolutePath));
            if (!normalizedPath.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return normalizedPath.Substring(projectRoot.Length + 1);
        }

        static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            string cleanName = fileName.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                cleanName = cleanName.Replace(invalidChar.ToString(), string.Empty);

            return cleanName;
        }

        void ClearGeneratedTextures()
        {
            if (processedPreview != null)
                DestroyImmediate(processedPreview);
            processedPreview = null;

            for (int i = 0; i < resources.Count; i++)
            {
                if (resources[i].Texture != null)
                    DestroyImmediate(resources[i].Texture);
            }

            resources.Clear();
        }
    }
}
