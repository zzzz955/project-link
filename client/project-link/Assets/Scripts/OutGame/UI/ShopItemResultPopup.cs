using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public class ShopItemResultModel
    {
        public ShopItemResultModel(bool success, string errorMessage, int quantityAfter)
        {
            Success = success;
            ErrorMessage = errorMessage;
            QuantityAfter = quantityAfter;
        }

        public bool Success { get; }
        public string ErrorMessage { get; }
        public int QuantityAfter { get; }
    }

    public class ShopItemResultPopup : PopupBase
    {
        [SerializeField] TextMeshProUGUI txtTitle;
        [SerializeField] TextMeshProUGUI txtBody;
        [SerializeField] Button btnConfirm;

        public void Init(ShopItemResultModel model)
        {
            model ??= new ShopItemResultModel(false, null, 0);

            txtTitle   ??= FindTmp("Txt_Title");
            txtBody    ??= FindTmp("Txt_Body");
            btnConfirm ??= FindBtn("Btn_Confirm");

            BindOverlayClose();

            // Override localized title with dynamic success/fail string
            if (txtTitle != null)
            {
                var lt = txtTitle.GetComponent<LocalizedText>();
                if (lt != null) lt.enabled = false;
                txtTitle.text = model.Success
                    ? LocalizationManager.Get("shop.result.success_title")
                    : LocalizationManager.Get("shop.result.fail_title");
            }

            if (txtBody != null)
            {
                txtBody.text = model.Success
                    ? ""
                    : string.IsNullOrEmpty(model.ErrorMessage)
                        ? LocalizationManager.GetError("PURCHASE_FAILED")
                        : model.ErrorMessage;
            }

            if (btnConfirm != null)
                btnConfirm.onClick.AddListener(() => PopupManager.Instance.CloseTop());
        }

        TextMeshProUGUI FindTmp(string n)
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == n) return t;
            return null;
        }

        Button FindBtn(string n)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
                if (b.name == n) return b;
            return null;
        }
    }
}
