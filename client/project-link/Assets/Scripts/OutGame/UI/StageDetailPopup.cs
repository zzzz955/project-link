using System.Globalization;
using ProjectLink.Core;
using ProjectLink.Contracts.Ranking;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class StageDetailPopup : PopupBase
    {
        [SerializeField] Button btnClose;
        [SerializeField] Button btnPlay;
        [SerializeField] RectTransform starRow;
        [SerializeField] RectTransform rankContent;
        [SerializeField] Sprite starOnSprite;
        [SerializeField] Sprite starOffSprite;

        int _stageId;
        bool _initialized;

        public void Init(int stageId = 0)
        {
            if (_initialized) return;
            _initialized = true;
            _stageId = Mathf.Max(1, stageId > 0 ? stageId : GameContext.SelectedStageId);
            btnClose ??= FindButton("Btn_Close");
            btnPlay  ??= FindButton("Btn_Play");
            starRow  ??= FindRect("Group_Stars");
            rankContent ??= FindRect("RankContent");
            EnsureRankingContent();
            BindOverlayClose();
            SetDynamicTitle();

            if (btnClose != null)
                btnClose.onClick.AddListener(() => PopupManager.Instance.CloseTop());
            if (btnPlay != null)
                btnPlay.onClick.AddListener(OnPlay);

            UiServiceLocator.UiData.GetProgress(ApplyProgress);
            UiServiceLocator.UiData.GetRanking($"stage:{_stageId}", ApplyRanking);
        }

        void SetDynamicTitle()
        {
            var titleTmp = FindText("Txt_Title");
            if (titleTmp == null) return;
            var lt = titleTmp.GetComponent<LocalizedText>();
            if (lt != null) lt.enabled = false;
            titleTmp.text = string.Format(LocalizationManager.Get("popup.stage.title_n_fmt"), _stageId);
        }

        void OnPlay()
        {
            PopupManager.Instance.CloseTop();
            RuntimeNavigationButtons.EnterStage(_stageId);
        }

        void ApplyProgress(ServiceResult<ProjectLink.Contracts.Progress.ProgressResponse> result)
        {
            int stars = 0;
            if (result.IsSuccess && result.Value != null)
            {
                for (int i = 0; i < result.Value.Stages.Count; i++)
                {
                    var entry = result.Value.Stages[i];
                    if (entry.StageId != _stageId) continue;
                    stars = Mathf.Clamp(entry.Stars, 0, 3);
                    break;
                }
            }

            RenderStars(stars, starRow);
        }

        void ApplyRanking(ServiceResult<RankingListResponse> result)
        {
            ClearChildren(rankContent);

            if (!result.IsSuccess || result.Value == null)
                return;

            var ranking = result.Value;
            int count = Mathf.Min(10, ranking.Entries.Count);
            for (int i = 0; i < count; i++)
            {
                var entry = ranking.Entries[i];
                AddRankRow($"#{entry.Rank}",
                    string.IsNullOrEmpty(entry.DisplayName)
                        ? LocalizationManager.Get("popup.account.guest")
                        : entry.DisplayName,
                    FormatScore(entry.Value), entry.IsMe);
            }
        }

        void EnsureRankingContent()
        {
            RectTransform parent = transform as RectTransform;

            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == "Content") { parent = rect; break; }
            }

            if (rankContent == null)
            {
                var scroll = new GameObject("RankScroll",
                    typeof(RectTransform), typeof(Image), typeof(Mask), typeof(ScrollRect), typeof(LayoutElement));
                scroll.transform.SetParent(parent, false);
                scroll.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
                scroll.GetComponent<Mask>().showMaskGraphic = false;
                scroll.GetComponent<LayoutElement>().preferredHeight = 360f;

                var content = new GameObject("RankContent",
                    typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                content.transform.SetParent(scroll.transform, false);
                rankContent = content.GetComponent<RectTransform>();
                rankContent.anchorMin = new Vector2(0f, 1f);
                rankContent.anchorMax = new Vector2(1f, 1f);
                rankContent.pivot = new Vector2(0.5f, 1f);
                rankContent.offsetMin = Vector2.zero;
                rankContent.offsetMax = Vector2.zero;

                var layout = content.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(18, 18, 12, 12);
                layout.spacing = 8f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;

                var fitter = content.GetComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var scrollRect = scroll.GetComponent<ScrollRect>();
                scrollRect.viewport = scroll.GetComponent<RectTransform>();
                scrollRect.content = rankContent;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.horizontal = false;
            }
        }

        void RenderStars(int earnedStars, RectTransform parent)
        {
            if (parent == null) return;
            if (parent.Find("Img_Star_0") != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var slot = parent.Find($"Img_Star_{i}");
                    if (slot == null) continue;
                    var img = slot.GetComponent<Image>();
                    if (img == null) continue;
                    bool filled = i < earnedStars;
                    var sprite = filled ? starOnSprite : starOffSprite;
                    if (sprite != null) { img.sprite = sprite; img.color = Color.white; img.preserveAspect = true; }
                    else img.color = filled ? new Color(1f, 0.82f, 0.15f, 1f) : new Color(1f, 1f, 1f, 0.18f);
                }
                return;
            }
            ClearChildren(parent);
            for (int i = 0; i < 3; i++)
                AddStarSlot(parent, i < earnedStars);
        }

        void AddStarSlot(RectTransform parent, bool filled)
        {
            var go = new GameObject("Star", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            var sprite = filled ? starOnSprite : starOffSprite;
            if (sprite != null) { img.sprite = sprite; img.color = Color.white; img.preserveAspect = true; }
            else img.color = filled ? new Color(1f, 0.82f, 0.15f, 1f) : new Color(1f, 1f, 1f, 0.18f);
            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 64f;
            layout.preferredHeight = 64f;
        }

        void AddRankRow(string rank, string name, string value, bool isMe)
        {
            if (rankContent == null) return;
            var row = new GameObject($"Rank_{rank}",
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(rankContent, false);
            row.GetComponent<Image>().color = isMe
                ? new Color(0.12f, 0.45f, 0.9f, 0.75f)
                : new Color(1f, 1f, 1f, 0.08f);
            row.GetComponent<LayoutElement>().preferredHeight = 54f;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 6, 6);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            AddLabel(row.transform, rank, 0.28f, TextAlignmentOptions.MidlineLeft);
            AddLabel(row.transform, name, 1f, TextAlignmentOptions.MidlineLeft);
            AddLabel(row.transform, value, 0.45f, TextAlignmentOptions.MidlineRight);
        }

        static void AddLabel(Transform parent, string text, float flexibleWidth, TextAlignmentOptions alignment)
        {
            var label = new GameObject("Text",
                typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            label.transform.SetParent(parent, false);
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22f;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14f;
            tmp.fontSizeMax = 22f;
            tmp.color = Color.white;
            tmp.alignment = alignment;
            label.GetComponent<LayoutElement>().flexibleWidth = flexibleWidth;
        }

        static string FormatScore(long value) =>
            value >= 1_000_000 ? $"{value / 1_000_000.0:F1}M"
            : value >= 1_000 ? $"{value / 1_000.0:F1}K"
            : value.ToString(CultureInfo.InvariantCulture);

        static void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        Button FindButton(string childName)
        {
            foreach (var b in GetComponentsInChildren<Button>(true))
                if (b.name == childName) return b;
            return null;
        }

        TextMeshProUGUI FindText(string childName)
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == childName) return t;
            return null;
        }

        RectTransform FindRect(string childName)
        {
            foreach (var r in GetComponentsInChildren<RectTransform>(true))
                if (r.name == childName) return r;
            return null;
        }
    }
}
