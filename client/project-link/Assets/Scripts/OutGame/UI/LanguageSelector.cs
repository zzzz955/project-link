using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class LanguageSelector : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown dropdown;

        bool _isRefreshing;

        void Awake()
        {
            if (dropdown == null)
                dropdown = GetComponent<TMP_Dropdown>();

            if (dropdown != null)
                EnsureDropdownSetup();
        }

        void EnsureDropdownSetup()
        {
            if (dropdown.captionText == null)
            {
                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(dropdown.transform, false);
                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(10, 4);
                labelRect.offsetMax = new Vector2(-28, -4);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.fontSize = 26;
                label.color = Color.white;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.raycastTarget = false;
                dropdown.captionText = label;
            }

            if (dropdown.template != null) return;

            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(dropdown.transform, false);
            var templateRect = templateGo.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0, 200);
            templateGo.AddComponent<CanvasRenderer>();
            templateGo.AddComponent<Image>().color = new Color(0.08f, 0.13f, 0.24f, 0.98f);
            var scrollRect = templateGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            templateGo.SetActive(false);

            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(templateGo.transform, false);
            var viewportRect = viewportGo.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0, 1);
            viewportGo.AddComponent<CanvasRenderer>();
            viewportGo.AddComponent<Image>().color = new Color(0, 0, 0, 255);
            viewportGo.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = viewportRect;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRect;

            var itemGo = new GameObject("Item");
            itemGo.transform.SetParent(contentGo.transform, false);
            var itemRect = itemGo.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, 1);
            itemRect.anchorMax = new Vector2(1, 1);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(0, 56);
            itemGo.AddComponent<CanvasRenderer>();
            var itemLayoutElement = itemGo.AddComponent<LayoutElement>();
            itemLayoutElement.minHeight = 56f;
            itemLayoutElement.preferredHeight = 56f;
            var itemImage = itemGo.AddComponent<Image>();
            itemImage.color = new Color(0, 0, 0, 0);
            var itemToggle = itemGo.AddComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;
            itemToggle.isOn = false;

            var checkmarkGo = new GameObject("Item Checkmark");
            checkmarkGo.transform.SetParent(itemGo.transform, false);
            var checkmarkRect = checkmarkGo.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(20, 20);
            checkmarkRect.anchoredPosition = new Vector2(10, 0);
            checkmarkGo.AddComponent<CanvasRenderer>();
            var checkmarkImage = checkmarkGo.AddComponent<Image>();
            checkmarkImage.color = new Color(1, 1, 1, 0.7f);
            itemToggle.graphic = checkmarkImage;

            var itemLabelGo = new GameObject("Item Label");
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            var itemLabelRect = itemLabelGo.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(24, 1);
            itemLabelRect.offsetMax = new Vector2(-10, -2);
            var itemLabel = itemLabelGo.AddComponent<TextMeshProUGUI>();
            itemLabel.fontSize = 26;
            itemLabel.color = Color.white;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.raycastTarget = false;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
        }

        void OnEnable()
        {
            if (dropdown == null)
                return;

            dropdown.onValueChanged.AddListener(OnValueChanged);
            LocalizationManager.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnValueChanged);

            LocalizationManager.LanguageChanged -= Refresh;
        }

        public void Refresh()
        {
            if (dropdown == null || LocalizationManager.Instance == null)
                return;

            _isRefreshing = true;
            dropdown.ClearOptions();
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_us")));
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_kr")));
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_cn")));
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_tw")));
            dropdown.options.Add(new TMP_Dropdown.OptionData(LocalizationManager.Get("country_th")));
            dropdown.value = ToIndex(LocalizationManager.Instance.CurrentLanguage);
            dropdown.RefreshShownValue();
            _isRefreshing = false;
        }

        void OnValueChanged(int index)
        {
            if (_isRefreshing)
                return;

            LocalizationManager.SetLanguage(ToLanguage(index));
        }

        static int ToIndex(LanguageCode language)
        {
            return language switch
            {
                LanguageCode.KO => 1,
                LanguageCode.ZH_CN => 2,
                LanguageCode.ZH_TW => 3,
                LanguageCode.TH => 4,
                _ => 0,
            };
        }

        static LanguageCode ToLanguage(int index)
        {
            return index switch
            {
                1 => LanguageCode.KO,
                2 => LanguageCode.ZH_CN,
                3 => LanguageCode.ZH_TW,
                4 => LanguageCode.TH,
                _ => LanguageCode.EN,
            };
        }
    }
}
