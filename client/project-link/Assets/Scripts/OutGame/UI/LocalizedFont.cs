using ProjectLink.Core;
using TMPro;
using UnityEngine;

namespace ProjectLink.OutGame.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class LocalizedFont : MonoBehaviour
    {
        TextMeshProUGUI _label;

        void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            Apply();
        }

        void OnEnable()
        {
            LocalizationManager.LanguageChanged += Apply;
            Apply();
        }

        void OnDisable()
        {
            LocalizationManager.LanguageChanged -= Apply;
        }

        void Apply()
        {
            if (_label == null) _label = GetComponent<TextMeshProUGUI>();
            var registry = FontRegistry.Instance;
            if (registry == null || LocalizationManager.Instance == null || _label == null) return;
            bool isBold = (_label.fontStyle & FontStyles.Bold) != 0;
            var lang = LocalizationManager.Instance.CurrentLanguage;
            if (!registry.TryGetFonts(lang, out var regular, out var bold)) return;
            var target = isBold && bold != null ? bold : regular;
            if (target == null || _label.font == target) return;
            _label.font = target;
            _label.ForceMeshUpdate(false, true);
        }
    }
}
