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

        void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
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

            if (_label != null && !string.IsNullOrEmpty(stringId))
                _label.text = LocalizationManager.Get(stringId);
        }
    }
}
