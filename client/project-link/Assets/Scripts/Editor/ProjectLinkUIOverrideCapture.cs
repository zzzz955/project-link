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

            var manifest = UIOverrideManifest.LoadOrCreate();
            baseline.RebuildIndex();
            manifest.entries.RemoveAll(e => e.status == "pending");

            var prevScene = SceneManager.GetActiveScene().path;

            foreach (var path in ScenePaths)
            {
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                CaptureScene(scene, $"Scene:{sceneName}", baseline, manifest);
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            foreach (var guid in guids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath);
                var go = PrefabUtility.LoadPrefabContents(prefabPath);
                CapturePrefabGO(go, $"Prefab:{prefabName}", baseline, manifest);
                PrefabUtility.UnloadPrefabContents(go);
            }

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

        static void CaptureScene(Scene scene, string target, UIBaselineSnapshot baseline, UIOverrideManifest manifest)
        {
            var visited = new HashSet<string>();
            foreach (var root in scene.GetRootGameObjects())
                WalkGO(root, root.name, target, baseline, manifest, visited);
            DetectRemovedGOs(target, visited, baseline, manifest);
        }

        static void CapturePrefabGO(GameObject root, string target, UIBaselineSnapshot baseline, UIOverrideManifest manifest)
        {
            var visited = new HashSet<string>();
            WalkGO(root, root.name, target, baseline, manifest, visited);
            DetectRemovedGOs(target, visited, baseline, manifest);
        }

        static void WalkGO(GameObject go, string path, string target,
            UIBaselineSnapshot baseline, UIOverrideManifest manifest, HashSet<string> visited)
        {
            visited.Add(path);
            bool existsInBaseline = baseline.ContainsPath(target, path);

            if (existsInBaseline)
            {
                // Diff tracked properties
                foreach (var (comp, compType, keys) in UIPropertySerializer.TrackedComponents(go))
                {
                    foreach (var key in keys)
                    {
                        var curr = UIPropertySerializer.Get(comp, key);
                        if (curr == null) continue;
                        if (!baseline.TryGet(target, path, compType, key, out var baseVal)) continue;
                        if (curr == baseVal) continue;

                        manifest.entries.Add(new UIOverrideManifest.Entry
                        {
                            id      = manifest.NextId(target),
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
                // Not in baseline → manually added GO
                var props = new List<UIOverrideManifest.PropSnapshot>();
                foreach (var (comp, compType, keys) in UIPropertySerializer.TrackedComponents(go))
                    foreach (var key in keys)
                    {
                        var val = UIPropertySerializer.Get(comp, key);
                        if (val != null) props.Add(new UIOverrideManifest.PropSnapshot { comp = compType, key = key, val = val });
                    }

                manifest.entries.Add(new UIOverrideManifest.Entry
                {
                    id         = manifest.NextId(target),
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
                WalkGO(child.gameObject, path + "/" + child.name, target, baseline, manifest, visited);
            }
        }

        static void DetectRemovedGOs(string target, HashSet<string> visited,
            UIBaselineSnapshot baseline, UIOverrideManifest manifest)
        {
            var baselinePaths = baseline.GetPathsForTarget(target);
            baselinePaths.ExceptWith(visited);
            foreach (var removedPath in baselinePaths)
            {
                manifest.entries.Add(new UIOverrideManifest.Entry
                {
                    id     = manifest.NextId(target),
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
    }

    // ── Runtime apply helpers — called by ProjectLinkUIBuilder ────────────────

    public static class ProjectLinkUIOverrideApply
    {
        const string PrefabRoot = "Assets/Resources/Prefabs/UI";

        // Called from BuildScene — snapshot clean build state, then apply prop overrides
        public static void SnapshotAndApplyScene(string sceneName)
        {
            var scene = SceneManager.GetActiveScene();

            var baseline = UIBaselineSnapshot.LoadOrCreate();
            baseline.RebuildIndex();
            baseline.ClearTarget($"Scene:{sceneName}");
            foreach (var root in scene.GetRootGameObjects())
                SnapshotGO(root, root.name, $"Scene:{sceneName}", baseline);
            baseline.Flush();
            EditorUtility.SetDirty(baseline);

            ApplyToScene(scene, $"Scene:{sceneName}");
        }

        // Called from SavePopupPrefab — snapshot clean build state, then apply prop overrides
        public static void SnapshotAndApplyPrefab(GameObject root, string prefabName)
        {
            var target = $"Prefab:{prefabName}";

            var baseline = UIBaselineSnapshot.LoadOrCreate();
            baseline.RebuildIndex();
            baseline.ClearTarget(target);
            SnapshotGO(root, root.name, target, baseline);
            baseline.Flush();
            EditorUtility.SetDirty(baseline);

            ApplyToGO(root, root.name, target);
        }

        // ─── Apply ────────────────────────────────────────────────────────

        static void ApplyToScene(Scene scene, string target)
        {
            var manifest = UIOverrideManifest.LoadOrCreate();
            foreach (var entry in manifest.entries)
            {
                if (entry.target != target || entry.status != "pending" || entry.op != "prop") continue;
                var t = FindByPath(scene, entry.path);
                if (t == null) { Debug.LogWarning($"[UIOverride] Path not found: {entry.path}"); continue; }
                ApplyProp(t.gameObject, entry.comp, entry.key, entry.currVal);
            }
        }

        static void ApplyToGO(GameObject root, string rootName, string target)
        {
            var manifest = UIOverrideManifest.LoadOrCreate();
            foreach (var entry in manifest.entries)
            {
                if (entry.target != target || entry.status != "pending" || entry.op != "prop") continue;
                var t = FindByPath(root, entry.path);
                if (t == null) { Debug.LogWarning($"[UIOverride] Path not found in prefab: {entry.path}"); continue; }
                ApplyProp(t.gameObject, entry.comp, entry.key, entry.currVal);
            }
        }

        static void ApplyProp(GameObject go, string compType, string key, string val)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c.GetType().Name != compType) continue;
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
