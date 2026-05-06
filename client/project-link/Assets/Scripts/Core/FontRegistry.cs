using System;
using TMPro;
using UnityEngine;

namespace ProjectLink.Core
{
    [CreateAssetMenu(menuName = "Project Link/Font Registry")]
    public sealed class FontRegistry : ScriptableObject
    {
        public const string ResourcePath = "FontRegistry";

        [Serializable]
        public struct LanguageFonts
        {
            public LanguageCode language;
            public TMP_FontAsset regular;
            public TMP_FontAsset bold;
        }

        [SerializeField] LanguageFonts[] entries;

        static FontRegistry _instance;

        public static FontRegistry Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<FontRegistry>(ResourcePath);
                return _instance;
            }
        }

        public bool TryGetFonts(LanguageCode language, out TMP_FontAsset regular, out TMP_FontAsset bold)
        {
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    if (entry.language == language)
                    {
                        regular = entry.regular;
                        bold = entry.bold;
                        return regular != null || bold != null;
                    }
                }
            }
            regular = bold = null;
            return false;
        }
    }
}
