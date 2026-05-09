using System.Globalization;
using ProjectLink.Contracts.Lobby;
using ProjectLink.Contracts.Ranking;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class LobbyWireframeController : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI profileNameText;
        [SerializeField] TextMeshProUGUI energyText;
        [SerializeField] TextMeshProUGUI coinText;
        [SerializeField] TextMeshProUGUI stageNumberText;
        [SerializeField] TextMeshProUGUI starsText;
        [SerializeField] TextMeshProUGUI dailyProgressText;
        [SerializeField] TextMeshProUGUI colorCupTimerText;
        [SerializeField] TextMeshProUGUI playDisabledReasonText;
        [SerializeField] TextMeshProUGUI shopBalanceText;
        [SerializeField] TextMeshProUGUI rankingMetricText;
        [SerializeField] TextMeshProUGUI rankingErrorText;
        [SerializeField] RectTransform shopContent;
        [SerializeField] RectTransform rankingContent;
        [SerializeField] Button playButton;
        [SerializeField] Button refillButton;

        IStaticCatalogService _catalog;
        IUiDataService _uiData;

        void Awake()
        {
            ResolveMissingReferences();
            _catalog = UiServiceLocator.Catalog;
            _uiData = UiServiceLocator.UiData;
        }

        void Start()
        {
            RefreshLobby();
            RefreshShop();
            RefreshRanking("global_stages");
        }

        public void RefreshRanking(string category)
        {
            ClearChildren(rankingContent);
            SetText(rankingErrorText, "");

            _uiData.GetRanking(category, result =>
            {
                if (!result.IsSuccess)
                {
                    SetText(rankingErrorText, result.ErrorCode);
                    return;
                }

                ApplyRanking(result.Value);
            });
        }

        void RefreshLobby()
        {
            _uiData.GetLobbyState(result =>
            {
                if (!result.IsSuccess)
                {
                    SetText(playDisabledReasonText, result.ErrorCode);
                    if (playButton != null) playButton.interactable = false;
                    return;
                }

                ApplyLobby(result.Value);
            });
        }

        void RefreshShop()
        {
            ClearChildren(shopContent);
            _uiData.GetShopCatalog(result =>
            {
                if (!result.IsSuccess)
                {
                    AddRow(shopContent, "ShopErrorRow", result.ErrorCode, "", false);
                    return;
                }

                var model = UiViewModelMapper.ToShopScreen(result.Value, _catalog);
                SetText(shopBalanceText, FormatNumber(model.SoftBalance));

                foreach (var product in model.Products)
                {
                    string title = string.IsNullOrEmpty(product.ItemName) ? product.Name : product.ItemName;
                    string price = product.IsIapProduct ? product.PriceIapSku : FormatNumber(product.PriceSoft);
                    AddRow(shopContent, $"Product_{product.ProductId}", title, price, true);
                }
            });
        }

        void ApplyLobby(LobbyStateResponse lobby)
        {
            int currentStageId = lobby.ProgressSummary.NextUnlockedStageId > 0
                ? lobby.ProgressSummary.NextUnlockedStageId
                : Mathf.Max(1, lobby.ProgressSummary.HighestStageId);

            SetText(profileNameText, lobby.Profile.DisplayName);
            SetText(energyText, $"{lobby.Stamina.Current}/{lobby.Stamina.Max}");
            SetText(coinText, FormatNumber(lobby.Currency.SoftAmount));
            SetText(stageNumberText, currentStageId.ToString(CultureInfo.InvariantCulture));
            SetText(starsText, lobby.ProgressSummary.TotalStarsEarned.ToString(CultureInfo.InvariantCulture));
            SetText(dailyProgressText, $"{lobby.DailyChallenge.PlayCountToday}/{lobby.DailyChallenge.PlayCountTarget}");
            SetText(colorCupTimerText, lobby.SeasonEvent?.EndAt ?? "");

            bool canPlay = lobby.Stamina.Current > 0;
            if (playButton != null)
                playButton.interactable = canPlay;
            if (refillButton != null)
                refillButton.gameObject.SetActive(!canPlay);
            SetText(playDisabledReasonText, canPlay ? "" : "Energy 0");
        }

        void ApplyRanking(RankingListResponse ranking)
        {
            SetText(rankingMetricText, ranking.MetricLabel);

            if (ranking.Entries.Count > 0)
            {
                var top = ranking.Entries[0];
                AddRow(rankingContent, "TopRankCard", $"#1 {top.DisplayName}", FormatNumber(top.Value), top.IsMe);
            }

            for (int i = 0; i < ranking.Entries.Count; i++)
            {
                var entry = ranking.Entries[i];
                AddRow(rankingContent, $"Rank_{entry.Rank}", $"#{entry.Rank} {entry.DisplayName}", FormatNumber(entry.Value), entry.IsMe);
            }

            if (ranking.MyRank != null)
                AddRow(rankingContent, "MyRankPin", $"#{ranking.MyRank.Rank} {ranking.MyRank.DisplayName}", FormatNumber(ranking.MyRank.Value), true);
        }

        void ResolveMissingReferences()
        {
            profileNameText ??= FindText("ProfileNameText");
            energyText ??= FindText("EnergyValueText");
            coinText ??= FindText("CoinValueText");
            stageNumberText ??= FindText("CurrentStageNumberText");
            starsText ??= FindText("StageStarsValueText");
            dailyProgressText ??= FindText("DailyProgressText");
            colorCupTimerText ??= FindText("ColorCupTimerText");
            playDisabledReasonText ??= FindText("PlayDisabledReasonText");
            shopBalanceText ??= FindText("ShopBalanceText");
            rankingMetricText ??= FindText("RankingMetricText");
            rankingErrorText ??= FindText("RankingErrorText");
            shopContent ??= FindRect("ShopContent");
            rankingContent ??= FindRect("RankingContent");
            playButton ??= FindButton("PlayButton");
            refillButton ??= FindButton("RefillCTAButton");
        }

        TextMeshProUGUI FindText(string childName)
        {
            foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.name == childName)
                    return label;
            }

            return null;
        }

        RectTransform FindRect(string childName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == childName)
                    return rect;
            }

            return null;
        }

        Button FindButton(string childName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == childName)
                    return button;
            }

            return null;
        }

        static void AddRow(RectTransform parent, string rowName, string left, string right, bool highlighted)
        {
            if (parent == null) return;

            var row = new GameObject(rowName, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            var image = row.GetComponent<Image>();
            image.color = highlighted ? new Color(0.12f, 0.46f, 0.9f, 0.72f) : new Color(0.08f, 0.16f, 0.28f, 0.72f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 18, 18);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            var element = row.GetComponent<LayoutElement>();
            element.preferredHeight = rowName == "TopRankCard" ? 160f : 112f;

            AddRowLabel(row.transform, "LeftText", left, TextAlignmentOptions.MidlineLeft, 1f);
            AddRowLabel(row.transform, "RightText", right, TextAlignmentOptions.MidlineRight, 0.45f);
        }

        static void AddRowLabel(Transform parent, string name, string text, TextAlignmentOptions alignment, float flexibleWidth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 36f;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.96f, 0.98f, 1f, 1f);
            label.alignment = alignment;
            label.enableAutoSizing = true;
            label.fontSizeMin = 18f;
            label.fontSizeMax = 36f;
            go.GetComponent<LayoutElement>().flexibleWidth = flexibleWidth;
        }

        static void ClearChildren(RectTransform parent)
        {
            if (parent == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static string FormatNumber(long value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }
    }
}
