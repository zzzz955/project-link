using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectLink.EditorTools
{
    public enum MergePolicy
    {
        OverwriteGeneratedOnly,
        SkipUserEdited,
        ThreeWayMerge,
        ReportOnly
    }

    public class UIBuildSettings : ScriptableObject
    {
        public const string AssetPath = "Assets/Editor/UIBuildSettings.asset";

        public MergePolicy mergePolicy = MergePolicy.ThreeWayMerge;

        static UIBuildSettings _instance;

        public static UIBuildSettings Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = AssetDatabase.LoadAssetAtPath<UIBuildSettings>(AssetPath);
                if (_instance != null) return _instance;
                _instance = CreateInstance<UIBuildSettings>();
                if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                    AssetDatabase.CreateFolder("Assets", "Editor");
                AssetDatabase.CreateAsset(_instance, AssetPath);
                AssetDatabase.SaveAssets();
                return _instance;
            }
        }
    }

    public class UIMergeReport
    {
        public struct Entry
        {
            public string target;
            public string path;
            public string comp;
            public string key;
            public string baseVal;
            public string userVal;
            public string toolVal;
        }

        public List<Entry> updated   = new();
        public List<Entry> skipped   = new();
        public List<Entry> conflicts = new();

        public bool HasAny => updated.Count + skipped.Count + conflicts.Count > 0;
    }
}
