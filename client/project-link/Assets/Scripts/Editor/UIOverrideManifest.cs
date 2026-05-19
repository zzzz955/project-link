using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectLink.EditorTools
{
    public class UIOverrideManifest : ScriptableObject
    {
        public const string AssetPath = "Assets/Editor/UIOverrideManifest.asset";
        public const string JsonPath  = "Assets/Editor/UIOverrideManifest.json";

        [Serializable]
        public class PropSnapshot
        {
            public string comp;
            public string key;
            public string val;
        }

        [Serializable]
        public class Entry
        {
            public string id;
            public string target;     // "Scene:Lobby" | "Prefab:SettingPopup"
            public string method;     // builder method name — grep target for AI
            public string path;       // full hierarchy path, "/" separator
            public string op;         // "prop" | "new_go" | "remove_go"
            public string comp;       // component type   (op=prop)
            public string key;        // property name    (op=prop)
            public string baseVal;    // builder baseline (op=prop)
            public string currVal;    // current value    (op=prop)
            public int    siblingIdx; // sibling order    (op=new_go)
            public List<PropSnapshot> goProps = new(); // component snapshot (op=new_go)
            public string status;     // "pending" | "promoted"
        }

        [Serializable]
        class JsonWrapper
        {
            public string captured;
            public List<Entry> entries;
        }

        public List<Entry> entries = new();

        public static UIOverrideManifest LoadOrCreate()
        {
            EnsureEditorFolder();
            var a = AssetDatabase.LoadAssetAtPath<UIOverrideManifest>(AssetPath);
            if (a != null) return a;
            a = CreateInstance<UIOverrideManifest>();
            AssetDatabase.CreateAsset(a, AssetPath);
            AssetDatabase.SaveAssets();
            return a;
        }

        public string NextId(string target)
        {
            var name = target.Contains(":") ? target.Substring(target.IndexOf(':') + 1) : target;
            var prefix = name.Length >= 3 ? name.Substring(0, 3).ToUpper() : name.ToUpper();
            return $"{prefix}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        public void WriteJson()
        {
            EnsureEditorFolder();
            var w = new JsonWrapper
            {
                captured = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                entries  = entries
            };
            var absPath = Path.Combine(Application.dataPath, "Editor", "UIOverrideManifest.json");
            File.WriteAllText(absPath, JsonUtility.ToJson(w, prettyPrint: true));
            AssetDatabase.ImportAsset(JsonPath);
        }

        static void EnsureEditorFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");
        }
    }
}
