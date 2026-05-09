using ProjectLink.Core;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class BuyItemPopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button closeIconButton;
        [SerializeField] Button buyButton;
        [SerializeField] TextMeshProUGUI featuredNameText;
        [SerializeField] TextMeshProUGUI featuredDescriptionText;
        [SerializeField] TextMeshProUGUI priceText;
        [SerializeField] TextMeshProUGUI softBalanceText;
        [SerializeField] RectTransform itemGrid;

        bool _initialized;
        int _selectedProductId;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindClose(closeButton);
            BindClose(closeIconButton);
            if (buyButton != null)
                buyButton.onClick.AddListener(PurchaseSelected);
            RefreshCatalog();
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("CloseButton");
            closeIconButton ??= FindButton("CloseIconButton");
            buyButton ??= FindButton("BuyButton");
            featuredNameText ??= FindText("FeaturedNameText");
            featuredDescriptionText ??= FindText("FeaturedDescriptionText");
            priceText ??= FindText("PriceText");
            softBalanceText ??= FindText("SoftBalanceText");
            itemGrid ??= FindRect("ItemGrid");
        }

        void RefreshCatalog()
        {
            ClearChildren(itemGrid);
            UiServiceLocator.UiData.GetShopCatalog(result =>
            {
                if (!result.IsSuccess)
                {
                    SetText(featuredNameText, result.ErrorCode);
                    return;
                }

                var model = UiViewModelMapper.ToShopScreen(result.Value, UiServiceLocator.Catalog);
                SetText(softBalanceText, model.SoftBalance.ToString());

                if (model.Products.Count == 0)
                    return;

                var featured = model.Products[0];
                _selectedProductId = featured.ProductId;
                SetText(featuredNameText, string.IsNullOrEmpty(featured.ItemName) ? featured.Name : featured.ItemName);
                SetText(featuredDescriptionText, featured.ItemDescription);
                SetText(priceText, featured.IsIapProduct ? featured.PriceIapSku : featured.PriceSoft.ToString());

                for (int i = 0; i < model.Products.Count && i < 3; i++)
                    AddItemCard(model.Products[i]);
            });
        }

        void AddItemCard(ShopProductModel product)
        {
            if (itemGrid == null) return;

            var go = new GameObject($"ItemCard_{product.ProductId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(itemGrid, false);
            go.GetComponent<Image>().color = new Color(0.1f, 0.22f, 0.42f, 0.85f);
            go.GetComponent<Button>().onClick.AddListener(() => SelectProduct(product));

            var label = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 12f);
            rect.offsetMax = new Vector2(-12f, -12f);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = string.IsNullOrEmpty(product.ItemName) ? product.Name : product.ItemName;
            tmp.fontSize = 28f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
        }

        void SelectProduct(ShopProductModel product)
        {
            _selectedProductId = product.ProductId;
            SetText(featuredNameText, string.IsNullOrEmpty(product.ItemName) ? product.Name : product.ItemName);
            SetText(featuredDescriptionText, product.ItemDescription);
            SetText(priceText, product.IsIapProduct ? product.PriceIapSku : product.PriceSoft.ToString());
        }

        void PurchaseSelected()
        {
            if (_selectedProductId <= 0) return;
            if (buyButton != null) buyButton.interactable = false;

            UiServiceLocator.UiData.PurchaseShopProduct(_selectedProductId, 1, "", result =>
            {
                if (buyButton != null) buyButton.interactable = true;
                if (!result.IsSuccess)
                {
                    SetText(featuredDescriptionText, result.ErrorCode);
                    return;
                }

                SetText(softBalanceText, result.Value.SoftBalanceAfter.ToString());
                RefreshCatalog();
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

        RectTransform FindRect(string rectName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == rectName)
                    return rect;
            }

            return null;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }
    }
}
