using System.Collections.Generic;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public class ShopInventoryStrip : MonoBehaviour
    {
        const float DimAlpha = 0.4f;

        public void Refresh(IUiDataService uiData, IStaticCatalogService catalog, Sprite[] itemIcons)
        {
            uiData.GetInventory(result =>
            {
                var quantities = new Dictionary<int, int>();
                if (result.IsSuccess && result.Value?.Items != null)
                    foreach (var slot in result.Value.Items)
                        quantities[slot.ItemId] = slot.Quantity;

                ApplyStrip(catalog, quantities, itemIcons);
            });
        }

        void ApplyStrip(IStaticCatalogService catalog, Dictionary<int, int> quantities, Sprite[] itemIcons)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            var allItems = catalog.GetAllItems();
            for (int i = 0; i < allItems.Count; i++)
            {
                var item = allItems[i];
                quantities.TryGetValue(item.id, out int qty);

                var cell = new GameObject($"InventoryCell_{item.id}", typeof(RectTransform), typeof(LayoutElement));
                cell.transform.SetParent(transform, false);
                var le = cell.GetComponent<LayoutElement>();
                le.preferredWidth = 120f;
                le.preferredHeight = 120f;

                var bg = cell.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.16f, 0.28f, 0.6f);

                var vlg = cell.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 4f;
                vlg.padding = new RectOffset(8, 8, 8, 8);
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;

                // Icon
                var iconGo = new GameObject("Img_Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconGo.transform.SetParent(cell.transform, false);
                iconGo.GetComponent<LayoutElement>().preferredHeight = 64f;
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.preserveAspect = true;
                Sprite sprite = (itemIcons != null && i < itemIcons.Length) ? itemIcons[i] : null;
                if (sprite != null) { iconImg.sprite = sprite; iconImg.color = Color.white; }
                else iconImg.color = new Color(1f, 0.75f, 0.02f, 0.6f);

                // Count label
                var countGo = new GameObject("Txt_Count", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                countGo.transform.SetParent(cell.transform, false);
                countGo.GetComponent<LayoutElement>().preferredHeight = 28f;
                var countTmp = countGo.GetComponent<TextMeshProUGUI>();
                countTmp.text = qty.ToString();
                countTmp.fontSize = 22f;
                countTmp.fontStyle = TMPro.FontStyles.Bold;
                countTmp.alignment = TMPro.TextAlignmentOptions.Midline;
                countTmp.color = Color.white;
                countTmp.raycastTarget = false;
                countGo.AddComponent<LocalizedFont>();

                // Dim if empty
                if (qty <= 0)
                {
                    var cg = cell.AddComponent<CanvasGroup>();
                    cg.alpha = DimAlpha;
                }
            }
        }
    }
}
