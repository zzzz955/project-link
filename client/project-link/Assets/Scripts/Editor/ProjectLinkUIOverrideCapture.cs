using System.Collections.Generic;
using ProjectLink;
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
                CapturePrefabGO(go, prefabName, $"Prefab:{prefabName}", baseline, newEntries);
                PrefabUtility.UnloadPrefabContents(go);
            }

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

        // Called by BuildCurrentSceneUI — captures only the active scene without opening others.
        public static void CaptureCurrentScene(string sceneName)
        {
            var baseline = UIBaselineSnapshot.LoadOrCreate();
            if (baseline.records.Count == 0) return;
            baseline.RebuildIndex();

            var scene = SceneManager.GetActiveScene();
            var target = $"Scene:{sceneName}";
            var newEntries = new List<UIOverrideManifest.Entry>();
            CaptureScene(scene, target, baseline, newEntries);

            var manifest = UIOverrideManifest.LoadOrCreate();
            manifest.entries.RemoveAll(e => e.target == target && e.status == "pending");
            manifest.entries.AddRange(newEntries);
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            manifest.WriteJson();
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

        // prefabName is used as the path root so paths are stable regardless of root GO name.
        static void CapturePrefabGO(GameObject root, string prefabName, string target, UIBaselineSnapshot baseline, List<UIOverrideManifest.Entry> results)
        {
            var visited = new HashSet<string>();
            WalkGO(root, prefabName, target, baseline, results, visited);
            DetectRemovedGOs(target, visited, baseline, results);
        }

        static void WalkGO(GameObject go, string path, string target,
            UIBaselineSnapshot baseline, List<UIOverrideManifest.Entry> results, HashSet<string> visited)
        {
            visited.Add(path);
            bool existsInBaseline = baseline.ContainsPath(target, path);

            var sid = go.GetComponent<GeneratedUIMarker>()?.stableId
                      ?? GeneratedUIMarker.ComputeId(target, path);

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
                            id       = MakeId(target),
                            stableId = sid,
                            target   = target,
                            method   = MethodFor(target),
                            path     = path,
                            op       = "prop",
                            comp     = compType,
                            key      = key,
                            baseVal  = baseVal,
                            currVal  = curr,
                            status   = "pending"
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
                    stableId   = sid,
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
                    id       = MakeId(target),
                    stableId = GeneratedUIMarker.ComputeId(target, removedPath),
                    target   = target,
                    method   = MethodFor(target),
                    path     = removedPath,
                    op       = "remove_go",
                    status   = "pending"
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
        static UIMergeReport _currentReport;

        public static void BeginReport() => _currentReport = new UIMergeReport();

        public static UIMergeReport EndReport()
        {
            var r = _currentReport;
            _currentReport = null;
            return r;
        }

        // Called from BuildScene — apply pending prop overrides to active scene with 3-way merge.
        public static void ApplySceneOverrides(string sceneName)
        {
            var scene    = SceneManager.GetActiveScene();
            var manifest = UIOverrideManifest.LoadOrCreate();
            var policy   = UIBuildSettings.Instance.mergePolicy;
            var target   = $"Scene:{sceneName}";

            foreach (var entry in manifest.entries)
            {
                if (entry.target != target || entry.status != "pending" || entry.op != "prop") continue;

                var t = FindByStableId(scene, entry.stableId) ?? FindByPath(scene, entry.path);
                if (t == null) { Debug.LogWarning($"[UIOverride] Path not found: {entry.path}"); continue; }

                var newToolVal = GetCurrentVal(t.gameObject, entry.comp, entry.key);
                ApplyMerge(t.gameObject, entry, newToolVal, policy);
            }
        }

        // Called from BuildAllSceneUI AFTER the restore check.
        public static void SaveBaselineForScene(string sceneName)
        {
            var scene    = SceneManager.GetActiveScene();
            var target   = $"Scene:{sceneName}";
            var baseline = UIBaselineSnapshot.LoadOrCreate();
            baseline.RebuildIndex();
            baseline.ClearTarget(target);
            foreach (var root in scene.GetRootGameObjects())
                SnapshotGO(root, root.name, target, baseline);
            baseline.Flush();
            EditorUtility.SetDirty(baseline);
        }

        // Called from SavePopupPrefab before SaveAsPrefabAsset.
        public static void ApplyPrefabOverrides(GameObject root, string prefabName)
        {
            var manifest = UIOverrideManifest.LoadOrCreate();
            var policy   = UIBuildSettings.Instance.mergePolicy;
            var target   = $"Prefab:{prefabName}";

            foreach (var entry in manifest.entries)
            {
                if (entry.target != target || entry.status != "pending" || entry.op != "prop") continue;

                var t = FindByStableId(root, entry.stableId) ?? FindByPath(root, entry.path);
                if (t == null) { Debug.LogWarning($"[UIOverride] Path not found in prefab: {entry.path}"); continue; }

                var newToolVal = GetCurrentVal(t.gameObject, entry.comp, entry.key);
                ApplyMerge(t.gameObject, entry, newToolVal, policy);
            }
        }

        // Called from SavePopupPrefab BEFORE ApplyPrefabOverrides.
        public static void SaveBaselineForPrefab(GameObject root, string prefabName)
        {
            var target   = $"Prefab:{prefabName}";
            var baseline = UIBaselineSnapshot.LoadOrCreate();
            baseline.RebuildIndex();
            baseline.ClearTarget(target);
            // Use prefabName as path root so paths are stable regardless of root GO name.
            SnapshotGO(root, prefabName, target, baseline);
            baseline.Flush();
            EditorUtility.SetDirty(baseline);
        }

        // ─── 3-way merge ──────────────────────────────────────────────────

        static void ApplyMerge(GameObject go, UIOverrideManifest.Entry entry, string newToolVal, MergePolicy policy)
        {
            bool toolChanged = newToolVal != null && newToolVal != entry.baseVal;
            bool userChanged = entry.currVal != entry.baseVal;

            if (!userChanged)
            {
                // Tool may have updated → keep tool value (already in scene).
                if (toolChanged)
                    _currentReport?.updated.Add(MakeReportEntry(entry, newToolVal));
                return;
            }

            if (!toolChanged)
            {
                // User changed, tool didn't → restore user value.
                ApplyProp(go, entry.comp, entry.key, entry.currVal);
                _currentReport?.skipped.Add(MakeReportEntry(entry, newToolVal));
                return;
            }

            // Both sides changed → conflict.
            switch (policy)
            {
                case MergePolicy.OverwriteGeneratedOnly:
                    // Tool wins — already in scene, don't apply user val.
                    break;
                case MergePolicy.SkipUserEdited:
                case MergePolicy.ThreeWayMerge:
                    // User wins.
                    ApplyProp(go, entry.comp, entry.key, entry.currVal);
                    break;
                case MergePolicy.ReportOnly:
                    // Don't apply — just report.
                    break;
            }
            _currentReport?.conflicts.Add(MakeReportEntry(entry, newToolVal));
        }

        static UIMergeReport.Entry MakeReportEntry(UIOverrideManifest.Entry e, string toolVal) =>
            new UIMergeReport.Entry
            {
                target  = e.target,
                path    = e.path,
                comp    = e.comp,
                key     = e.key,
                baseVal = e.baseVal,
                userVal = e.currVal,
                toolVal = toolVal ?? e.baseVal
            };

        // ─── Apply prop ───────────────────────────────────────────────────

        static void ApplyProp(GameObject go, string compType, string key, string val)
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c.GetType().Name != compType) continue;
                if (UIPropertySerializer.Get(c, key) == val) return;
                if (!UIPropertySerializer.Set(c, key, val))
                    Debug.LogWarning($"[UIOverride] Set failed: {compType}.{key}={val} on {go.name}");
                return;
            }
            Debug.LogWarning($"[UIOverride] Component not found: {compType} on {go.name}");
        }

        static string GetCurrentVal(GameObject go, string compType, string key)
        {
            foreach (var c in go.GetComponents<Component>())
                if (c.GetType().Name == compType) return UIPropertySerializer.Get(c, key);
            return null;
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
            // Root name is skipped — path root is always prefabName (stable), which may differ
            // from the actual root GO name when naming conventions change between builds.
            var slash = path.IndexOf('/');
            if (slash < 0) return prefabRoot.transform;
            return prefabRoot.transform.Find(path.Substring(slash + 1));
        }

        // ─── StableId resolution ──────────────────────────────────────────

        static Transform FindByStableId(Scene scene, string stableId)
        {
            if (string.IsNullOrEmpty(stableId)) return null;
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = FindByStableId(root, stableId);
                if (found != null) return found;
            }
            return null;
        }

        static Transform FindByStableId(GameObject go, string stableId)
        {
            if (string.IsNullOrEmpty(stableId)) return null;
            var marker = go.GetComponent<GeneratedUIMarker>();
            if (marker != null && marker.stableId == stableId) return go.transform;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                var found = FindByStableId(go.transform.GetChild(i).gameObject, stableId);
                if (found != null) return found;
            }
            return null;
        }
    }
}
