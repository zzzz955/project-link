using ProjectLink.Core;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public class ShopItemConfirmModel
    {
        public ShopItemConfirmModel(int itemId, string itemName, int cost, long currentBalance,
            System.Action<long> onPurchaseSuccess = null, string descriptionKey = null)
        {
            ItemId = itemId;
            ItemName = itemName;
            Cost = cost;
            CurrentBalance = currentBalance;
            OnPurchaseSuccess = onPurchaseSuccess;
            DescriptionKey = descriptionKey ?? "";
        }

        public int ItemId { get; }
        public string ItemName { get; }
        public int Cost { get; }
        public long CurrentBalance { get; }
        public System.Action<long> OnPurchaseSuccess { get; }
        public string DescriptionKey { get; }
    }

    public class ShopItemConfirmPopup : PopupBase
    {
        [SerializeField] TextMeshProUGUI txtDescription;
        [SerializeField] TextMeshProUGUI txtItemName;
        [SerializeField] TextMeshProUGUI txtBalance;
        [SerializeField] TextMeshProUGUI txtCost;
        [SerializeField] TextMeshProUGUI txtAfter;
        [SerializeField] Button btnBuy;
        [SerializeField] Button btnCancel;

        ShopItemConfirmModel _model;

        public void Init(ShopItemConfirmModel model)
        {
            _model = model ?? new ShopItemConfirmModel(0, "", 0, 0);

            txtDescription ??= FindTmp("Txt_Description");
            txtItemName ??= FindTmp("Txt_ItemName");
            txtBalance  ??= FindTmp("Txt_Balance");
            txtCost     ??= FindTmp("Txt_Cost");
            txtAfter    ??= FindTmp("Txt_After");
            btnBuy      ??= FindBtn("Btn_Buy");
            btnCancel   ??= FindBtn("Btn_Cancel");

            BindOverlayClose();

            if (txtDescription != null)
                txtDescription.text = string.IsNullOrEmpty(_model.DescriptionKey)
                    ? "" : LocalizationManager.Get(_model.DescriptionKey);
            if (txtItemName != null) txtItemName.text = _model.ItemName;
            if (txtBalance  != null) txtBalance.text  = FormatNumber(_model.CurrentBalance);
            if (txtCost     != null) txtCost.text     = FormatNumber(_model.Cost);
            if (txtAfter    != null) txtAfter.text    = FormatNumber(_model.CurrentBalance - _model.Cost);

            if (btnCancel != null)
                btnCancel.onClick.AddListener(() => PopupManager.Instance.CloseTop());

            if (btnBuy != null)
                btnBuy.onClick.AddListener(OnBuy);
        }

        void OnBuy()
        {
            btnBuy.interactable = false;
            UiServiceLocator.UiData.PurchaseItem(_model.ItemId, 1, result =>
            {
                PopupManager.Instance.CloseTop();
                if (result.IsSuccess)
                {
                    _model.OnPurchaseSuccess?.Invoke(result.Value.SoftBalanceAfter);
                    PopupManager.Request(PopupId.ShopItemResult,
                        new ShopItemResultModel(true, null, result.Value.QuantityAfter));
                }
                else
                {
                    PopupManager.Request(PopupId.ShopItemResult,
                        new ShopItemResultModel(false, LocalizationManager.GetError(result.ErrorCode), 0));
                }
            });
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

        static string FormatNumber(long v) => v.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
