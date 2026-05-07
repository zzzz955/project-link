using ProjectLink.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class EnergyPopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button closeIconButton;
        [SerializeField] Button watchAdButton;
        [SerializeField] Button refillButton;

        bool _initialized;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindClose(closeButton);
            BindClose(closeIconButton);
            BindClose(watchAdButton);
            BindClose(refillButton);
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("CloseButton");
            closeIconButton ??= FindButton("CloseIconButton");
            watchAdButton ??= FindButton("WatchAdButton");
            refillButton ??= FindButton("RefillButton");
        }

        void BindClose(Button button)
        {
            if (button != null)
                button.onClick.AddListener(CloseTop);
        }

        Button FindButton(string buttonName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                    return button;
            }

            return null;
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }
    }
}
