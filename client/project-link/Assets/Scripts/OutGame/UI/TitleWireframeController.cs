using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class TitleWireframeController : MonoBehaviour
    {
        [SerializeField] Button startButton;
        [SerializeField] Button languageButton;
        [SerializeField] Button accountButton;
        [SerializeField] TextMeshProUGUI versionText;
        [SerializeField] TextMeshProUGUI accountButtonText;

        void Start()
        {
            UiServiceLocator.UiData.GetBootstrapConfig(result =>
            {
                if (result.IsSuccess)
                    SetText(versionText, result.Value.ClientVersion);
            });

            UiServiceLocator.UiData.GetAccountMe(result =>
            {
                if (result.IsSuccess)
                    SetText(accountButtonText, string.IsNullOrEmpty(result.Value.DisplayName) ? "Guest" : result.Value.DisplayName);
            });
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (startButton != null) startButton.interactable = interactable;
            if (languageButton != null) languageButton.interactable = interactable;
            if (accountButton != null) accountButton.interactable = interactable;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }
    }
}
