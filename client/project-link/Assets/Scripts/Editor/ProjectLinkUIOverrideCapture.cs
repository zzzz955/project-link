using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectLink.EditorTools
{
    // ── Editor menu tools ─────────────────────────────────────────────────────

    public static class ProjectLinkUIOverrideCapture
    {
        static readonly string[] ScenePaths =
        {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/Title.unity",
            "Assets/Scenes/Lobby.unity",
            "Assets/Scenes/Game.unity",
        };
        const string PrefabRoot = "Assets/Resources/Prefabs/UI";

        [MenuItem("Tools/Project Link/UI Overrides/Capture All Overrides")]
        public static void CaptureAllOverrides()
        {
            var baseline = UIBaselineSnapshot.LoadOrCreate();
            if (baseline.records.Count == 0)
            {
                Debug.LogWarning("[UIOverride] No baseline. Run 'Build All Scene UI' first.");
                return;
            }
            baseline.RebuildIndex();

            var prevScene = SceneManager.GetActiveScene().path;

            // Collect into a temporary list — scene/prefab operations can destroy Unity objects
            // mid-loop (Unity may invalidate ScriptableObject refs during OpenScene), so we
            // defer touching the manifest until all risky operations are done.
            var newEntries = new List<UIOverrideManifest.Entry>();

            foreach (var path in ScenePaths)
            {
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                CaptureScene(scene, $"Scene:{sceneName}", baseline, newEntries);
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            foreach (var guid in guids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                var go = PrefabUtility.LoadPrefabContents(prefabPath);
                CapturePrefabGO(go, $"Prefab:{prefabName}", baseline, newEntries);
                PrefabUtility.UnloadPrefabContents(go);
            }

            // Load manifest fresh after all OpenScene/prefab operations
            var manifest = UIOverrideManifest.LoadOrCreate();
            manifest.entries.RemoveAll(e => e.status == "pending");
            manifest.entries.AddRange(newEntries);

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            manifest.WriteJson();

            if (!string.IsNullOrEmpty(prevScene))
                EditorSceneManager.OpenScene(prevScene, OpenSceneMode.Single);

            Debug.Log($"[UIOverride] {manifest.entries.Count} override entries → {UIOverrideManifest.JsonPath}");
        }

        [MenuItem("Tools/Project Link/UI Overrides/Clear Promoted Entries")]
        public static void ClearPromoted()
        {
            var manifest = UIOverrideManifest.LoadOrCreate();
            int before = manifest.entries.Count;
            manifest.entries.RemoveAll(e => e.status == "promoted");
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            manifest.WriteJson();
            Debug.Log($"[UIOverride] Removed {before - manifest.entries.Count} promoted entries.");
        }

        // ─── Capture helpers ──────────────────────────────────────────────

        static void CaptureScene(Scene scene, string target, UIBaselineSnapshot baseline, List<UIOverrideManifest.Entry> results)
        {
            var visited = new HashSet<string>();
            foreach (var root in scene.GetRootGameObjects())
                WalkGO(root, root.name, target, baseline, results, visited);
            DetectRemovedGOs(target, visited, baseline, results);
        }

        static void CapturePrefabGO(GameObject root, string target, UIBaselineSnapshot baseline, List<UIOverrideManifest.Entry> results)
        {
            var visited = new HashSet<string>();
            WalkGO(root, root.name, target, baseline, results, visited);
            DetectRemovedGOs(target, visited, baseline, results);
        }

        static void WalkGO(GameObject go, string path, string target,
            UIBaselineSnapshot baseline, List<UIOverrideManifest.Entry> results, HashSet<string> visited)
        {
            visited.Add(path);
            bool existsInBaseline = baseline.ContainsPath(target, path);

            if (existsInBaseline)
            {
                foreach (var (comp, compType, keys) in UIPropertySerializer.TrackedComponents(go))
                {
                    foreach (var key in keys)
                    {
                        var curr = UIPropertySerializer.Get(comp, key);
                        if (curr == null) continue;
                        if (!baseline.TryGet(target, path, compType, key, out var baseVal)) continue;
                        if (curr == baseVal) continue;

                        results.Add(new UIOverrideManifest.Entry
                        {
                            id      = MakeId(target),
                            target  = target,
                            method  = MethodFor(target),
                            path    = path,
                            op      = "prop",
                            comp    = compType,
                            key     = key,
                            baseVal = baseVal,
                            currVal = curr,
                            status  = "pending"
                        });
                    }
                }
            }
            else
            {
                var props = new List<UIOverrideManifest.PropSnapshot>();
                foreach (var (comp, compType, keys) in UIPropertySerializer.TrackedComponents(go))
                    foreach (var key in keys)
                    {
                        var val = UIPropertySerializer.Get(comp, key);
                        if (val != null) props.Add(new UIOverrideManifest.PropSnapshot { comp = compType, key = key, val = val });
                    }

                results.Add(new UIOverrideManifest.Entry
                {
                    id         = MakeId(target),
                    target     = target,
                    method     = MethodFor(target),
                    path       = path,
                    op         = "new_go",
                    siblingIdx = go.transform.GetSiblingIndex(),
                    goProps    = props,
                    status     = "pending"
                });
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);
                WalkGO(child.gameObject, path + "/" + child.name, target, baseline, results, visited);
            }
        }

        static void DetectRemovedGOs(string target, HashSet<string> visited,
            UIBaselineSnapshot baseline, List<UIOverrideManifest.Entry> results)
        {
            var baselinePaths = baseline.GetPathsForTarget(target);
            baselinePaths.ExceptWith(visited);
            foreach (var removedPath in baselinePaths)
            {
                results.Add(new UIOverrideManifest.Entry
                {
                    id     = MakeId(target),
                    target = target,
                    method = MethodFor(target),
                    path   = removedPath,
                    op     = "remove_go",
                    status = "pending"
                });
            }
        }

        static string MethodFor(string target)
        {
            var name = target.Contains(":") ? target.Substring(target.IndexOf(':') + 1) : target;
            return $"Build{name}";
        }

        static string MakeId(string target)
        {
            var name = target.Contains(":") ? target.Substring(target.IndexOf(':') + 1) : target;
            var prefix = name.Length >= 3 ? name.Substring(0, 3).ToUpper() : name.ToUpper();
            return $"{prefix}-{System.Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }
    }

    // ── Runtime apply helpers — called by ProjectLinkUIBuilder ────────────────

    public static class ProjectLinkUIOverrideApply
    {
        // Called from BuildScene — apply pending prop overrides to active scene (NO snapshot here)
        public static void ApplySceneOverrides(string sceneName)
        {
            var scene = SceneManager.GetActiveScene();
            var manifest = UIOverrideManifest.LoadOrCreate();
            var target = $"Scene:{sceneName}";
            foreach (var entry in manifest.entries)
            {
                if (entry.target != target || entry.status != "pending" || entry.op != "prop") continue;
                var t = FindByPath(scene, entry.path);
                if (t == null) { Debug.LogWarning($"[UIOverride] Path not found: {entry.path}"); continue; }
                ApplyProp(t.gameObject, entry.comp, entry.key, entry.currVal);
            }
        }

        // Called from BuildAllSceneUI AFTER the restore check — baseline is saved from whatever
        // is actually on disk, so CaptureAllOverrides will always diff against the correct state
        public static void SaveBaselineForScene(string sceneName)
        {
            var scene = SceneManager.GetActiveScene();
            var target = $"Scene:{sceneName}";
            var baseline = UIBaselineSnapshot.LoadOrCreate();
            baseline.RebuildIndex();
            baseline.ClearTarget(target);
            foreach (var root in scene.GetRootGameObjects())
                SnapshotGO(root, root.name, target, baseline);
            baseline.Flush();
            EditorUtility.SetDirty(baseline);
        }

        // Called from SavePopupPrefab before SaveAsPrefabAsset — apply pending prop overrides
        public static void ApplyPrefabOverrides(GameObject root, string prefabName)
        {
            var manifest = UIOverrideManifest.LoadOrCreate();
            var target = $"Prefab:{prefabName}";
            foreach (var entry in manifest.entries)
            {
                if (entry.target != target || entry.status != "pending" || entry.op != "prop") continue;
                var t = FindByPath(root, entry.path);
                if (t == null) { Debug.LogWarning($"[UIOverride] Path not found in prefab: {entry.path}"); continue; }
                ApplyProp(t.gameObject, entry.comp, entry.key, entry.currVal);
            }
        }

        // Called from SavePopupPrefab AFTER RestoreIfUnchanged — loads from disk so baseline
        // always matches the file that was actually committed (original or new)
        public static void SaveBaselineForPrefab(string prefabPath, string prefabName)
        {
            var target = $"Prefab:{prefabName}";
            var baseline = UIBaselineSnapshot.LoadOrCreate();
            baseline.RebuildIndex();
            baseline.ClearTarget(target);
            if (System.IO.File.Exists(prefabPath))
            {
                var go = PrefabUtility.LoadPrefabContents(prefabPath);
                SnapshotGO(go, go.name, target, baseline);
                PrefabUtility.UnloadPrefabContents(go);
            }
            baseline.Flush();
            EditorUtility.SetDirty(baseline);
        }

        // ─── Apply helper ─────────────────────────────────────────────────

        static void ApplyProp(GameObject go, string compType, string key, string val)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c.GetType().Name != compType) continue;
                // Skip if value already matches — prevents marking scene/font assets dirty
                if (UIPropertySerializer.Get(c, key) == val) return;
                if (!UIPropertySerializer.Set(c, key, val))
                    Debug.LogWarning($"[UIOverride] Set failed: {compType}.{key}={val} on {go.name}");
                return;
            }
            Debug.LogWarning($"[UIOverride] Component not found: {compType} on {go.name}");
        }

        // ─── Snapshot ─────────────────────────────────────────────────────

        static void SnapshotGO(GameObject go, string path, string target, UIBaselineSnapshot baseline)
        {
            foreach (var (comp, compType, keys) in UIPropertySerializer.TrackedComponents(go))
                foreach (var key in keys)
                {
                    var val = UIPropertySerializer.Get(comp, key);
                    if (val != null) baseline.Upsert(target, path, compType, key, val);
                }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                var child = go.transform.GetChild(i);
                SnapshotGO(child.gameObject, path + "/" + child.name, target, baseline);
            }
        }

        // ─── Path resolution ──────────────────────────────────────────────

        static Transform FindByPath(Scene scene, string path)
        {
            var slash = path.IndexOf('/');
            var rootName = slash < 0 ? path : path.Substring(0, slash);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != rootName) continue;
                if (slash < 0) return root.transform;
                return root.transform.Find(path.Substring(slash + 1));
            }
            return null;
        }

        static Transform FindByPath(GameObject prefabRoot, string path)
        {
            var slash = path.IndexOf('/');
            var rootName = slash < 0 ? path : path.Substring(0, slash);
            if (prefabRoot.name != rootName) return null;
            if (slash < 0) return prefabRoot.transform;
            return prefabRoot.transform.Find(path.Substring(slash + 1));
        }
    }
}
