using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectLink.EditorTools
{
    public class UIBaselineSnapshot : ScriptableObject
    {
        public const string AssetPath = "Assets/Editor/UIBaselineSnapshot.asset";

        [System.Serializable]
        public class Record
        {
            public string target;
            public string path;
            public string comp;
            public string key;
            public string val;
        }

        public List<Record> records = new();

        Dictionary<string, string> _index;

        public static UIBaselineSnapshot LoadOrCreate()
        {
            EnsureEditorFolder();
            var a = AssetDatabase.LoadAssetAtPath<UIBaselineSnapshot>(AssetPath);
            if (a != null) return a;
            a = CreateInstance<UIBaselineSnapshot>();
            AssetDatabase.CreateAsset(a, AssetPath);
            AssetDatabase.SaveAssets();
            return a;
        }

        public void RebuildIndex()
        {
            _index = new Dictionary<string, string>(records.Count);
            foreach (var r in records)
                _index[MakeKey(r.target, r.path, r.comp, r.key)] = r.val;
        }

        public bool TryGet(string target, string path, string comp, string key, out string val)
        {
            if (_index == null) RebuildIndex();
            return _index.TryGetValue(MakeKey(target, path, comp, key), out val);
        }

        public void Upsert(string target, string path, string comp, string key, string val)
        {
            if (_index == null) RebuildIndex();
            _index[MakeKey(target, path, comp, key)] = val;
        }

        public void ClearTarget(string target)
        {
            if (_index == null) RebuildIndex();
            var toRemove = new List<string>();
            var prefix = target + "\x01";
            foreach (var k in _index.Keys)
                if (k.StartsWith(prefix)) toRemove.Add(k);
            foreach (var k in toRemove) _index.Remove(k);
        }

        public bool ContainsPath(string target, string path)
        {
            if (_index == null) RebuildIndex();
            var prefix = target + "\x01" + path + "\x01";
            foreach (var k in _index.Keys)
                if (k.StartsWith(prefix)) return true;
            return false;
        }

        public HashSet<string> GetPathsForTarget(string target)
        {
            if (_index == null) RebuildIndex();
            var result = new HashSet<string>();
            var prefix = target + "\x01";
            foreach (var k in _index.Keys)
            {
                if (!k.StartsWith(prefix)) continue;
                var second = k.IndexOf('\x01', prefix.Length);
                if (second > 0) result.Add(k.Substring(prefix.Length, second - prefix.Length));
            }
            return result;
        }

        public void Flush()
        {
            if (_index == null) return;
            records.Clear();
            var keys = new System.Collections.Generic.List<string>(_index.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            foreach (var k in keys)
            {
                var p = k.Split('\x01');
                if (p.Length != 4) continue;
                records.Add(new Record { target = p[0], path = p[1], comp = p[2], key = p[3], val = _index[k] });
            }
        }

        static string MakeKey(string target, string path, string comp, string key)
            => target + "\x01" + path + "\x01" + comp + "\x01" + key;

        static void EnsureEditorFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");
        }
    }
}
