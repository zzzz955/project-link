using ProjectLink.Core;
using TMPro;
using UnityEngine;

namespace ProjectLink.OutGame.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] string stringId;

        TextMeshProUGUI _label;
        TMP_FontAsset _defaultFont;

        void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            _defaultFont = _label != null ? _label.font : null;
            Refresh();
        }

        void OnEnable()
        {
            LocalizationManager.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            LocalizationManager.LanguageChanged -= Refresh;
        }

        public void SetStringId(string value)
        {
            stringId = value;
            Refresh();
        }

        public void Refresh()
        {
            if (_label == null)
                _label = GetComponent<TextMeshProUGUI>();

            if (_label == null || string.IsNullOrEmpty(stringId))
                return;

            _label.text = LocalizationManager.Get(stringId);
            ApplyFont();
        }

        void ApplyFont()
        {
            var registry = FontRegistry.Instance;
            if (registry == null || LocalizationManager.Instance == null)
                return;

            bool isBold = (_label.fontStyle & FontStyles.Bold) != 0;
            var lang = LocalizationManager.Instance.CurrentLanguage;

            TMP_FontAsset target;
            if (registry.TryGetFonts(lang, out var regular, out var bold))
                target = isBold && bold != null ? bold : regular != null ? regular : _defaultFont;
            else
                target = _defaultFont;

            if (_label.font != target)
            {
                _label.font = target;
                _label.ForceMeshUpdate(false, true);
            }
        }
    }
}
