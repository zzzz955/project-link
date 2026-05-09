using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class BootstrapWireframeController : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI loadingLabelText;
        [SerializeField] TextMeshProUGUI versionText;
        [SerializeField] Image progressFillImage;

        void Start()
        {
            SetText(loadingLabelText, "Loading");
            if (progressFillImage != null)
                progressFillImage.fillAmount = 0.35f;

            UiServiceLocator.UiData.GetBootstrapConfig(result =>
            {
                if (!result.IsSuccess)
                {
                    SetText(loadingLabelText, result.ErrorCode);
                    return;
                }

                var value = result.Value;
                SetText(loadingLabelText, value.Maintenance ? "Maintenance" : "Ready");
                SetText(versionText, value.ClientVersion);
                if (progressFillImage != null)
                    progressFillImage.fillAmount = 1f;
            });
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }
    }
}
