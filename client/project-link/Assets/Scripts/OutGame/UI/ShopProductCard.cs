using System;
using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public class ShopProductCard : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI txtTitle;
        [SerializeField] Image imgItemIcon;
        [SerializeField] TextMeshProUGUI txtPrice;
        [SerializeField] Button btnCard;

        int _itemId;
        string _itemName;
        int _cost;
        Func<long> _getBalance;
        Action<long> _onPurchaseSuccess;

        public void Init(int itemId, string itemName, int cost, Func<long> getBalance, Sprite icon,
            Action<long> onPurchaseSuccess = null)
        {
            _itemId = itemId;
            _itemName = itemName;
            _cost = cost;
            _getBalance = getBalance;
            _onPurchaseSuccess = onPurchaseSuccess;

            txtTitle    ??= FindTmp("Txt_Title");
            imgItemIcon ??= FindImg("Img_ItemIcon");
            txtPrice    ??= FindTmp("Txt_Price");
            btnCard     ??= GetComponent<Button>() ?? GetComponentInChildren<Button>();

            if (txtTitle != null) txtTitle.text = itemName;
            if (txtPrice != null) txtPrice.text = cost.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            if (imgItemIcon != null && icon != null)
            {
                imgItemIcon.sprite = icon;
                imgItemIcon.color = Color.white;
                imgItemIcon.preserveAspect = true;
            }

            if (btnCard != null)
                btnCard.onClick.AddListener(OnTap);
        }

        void OnTap()
        {
            PopupManager.Request(PopupId.ShopItemConfirm,
                new ShopItemConfirmModel(_itemId, _itemName, _cost, _getBalance?.Invoke() ?? 0, _onPurchaseSuccess));
        }

        TextMeshProUGUI FindTmp(string n)
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == n) return t;
            return null;
        }

        Image FindImg(string n)
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
                if (img.name == n) return img;
            return null;
        }
    }
}
