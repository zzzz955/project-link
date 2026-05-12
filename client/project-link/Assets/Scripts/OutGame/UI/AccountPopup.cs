using ProjectLink.Core;
using ProjectLink.Contracts.Account;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class AccountPopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] TextMeshProUGUI displayNameText;
        [SerializeField] TextMeshProUGUI accountStatusText;

        bool _initialized;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindOverlayClose();
            BindClose(closeButton);
            UiServiceLocator.UiData.GetAccountMe(Apply);
        }

        void Apply(ServiceResult<AccountMeResponse> result)
        {
            if (!result.IsSuccess)
            {
                SetText(accountStatusText, result.ErrorCode);
                return;
            }

            SetText(displayNameText, result.Value.DisplayName);
            SetText(accountStatusText, result.Value.IsGuest ? "Guest" : string.Join(", ", result.Value.LinkedProviders));
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("CloseButton");
            displayNameText ??= FindText("DisplayNameText");
            accountStatusText ??= FindText("AccountStatusText");
        }

        Button FindButton(string name)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == name) return button;
            return null;
        }

        TextMeshProUGUI FindText(string name)
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == name) return text;
            return null;
        }

        static void BindClose(Button button)
        {
            if (button != null)
                button.onClick.AddListener(CloseTop);
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }
    }
}
