using ProjectLink.Core;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class SettingPopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button closeIconButton;
        [SerializeField] Button saveButton;
        [SerializeField] TextMeshProUGUI accountStatusText;

        bool _initialized;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindClose(closeButton);
            BindClose(closeIconButton);
            BindClose(saveButton);
            RefreshAccount();
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("CloseButton");
            closeIconButton ??= FindButton("CloseIconButton");
            saveButton ??= FindButton("SaveButton");
            accountStatusText ??= FindText("AccountStatusText");
        }

        void RefreshAccount()
        {
            UiServiceLocator.UiData.GetAccountMe(result =>
            {
                if (!result.IsSuccess)
                {
                    SetText(accountStatusText, result.ErrorCode);
                    return;
                }

                SetText(accountStatusText, result.Value.IsGuest ? "Guest" : result.Value.DisplayName);
            });
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

        TextMeshProUGUI FindText(string labelName)
        {
            foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.name == labelName)
                    return label;
            }

            return null;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }
    }
}
