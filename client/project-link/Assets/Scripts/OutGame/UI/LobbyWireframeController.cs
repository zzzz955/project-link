using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using ProjectLink.Contracts.Progress;
using ProjectLink.Contracts.Ranking;
using ProjectLink.Core;
using ProjectLink.Data;
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
        [SerializeField] TextMeshProUGUI staminaTimerText;
        [SerializeField] TextMeshProUGUI coinText;
        [SerializeField] TextMeshProUGUI stageNumberText;
        [SerializeField] TextMeshProUGUI previousStageNumberText;
        [SerializeField] TextMeshProUGUI nextStageNumberText;
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
        [SerializeField] Button previousStageButton;
        [SerializeField] Button nextStageButton;

        const string DefaultRankingCategory = "global_stages";
        const string ScoreRankingCategory = "global_score";

        IStaticCatalogService _catalog;
        IUiDataService _uiData;
        LobbyViewModel _viewModel;
        int _selectedStageId;
        int _maxSelectableStageId = 1;
        readonly Dictionary<int, int> _stageStars = new();
        readonly HashSet<int> _unlockedStages = new();
        bool _progressLoaded;
        DateTimeOffset? _nextRechargeAt;
        float _nextTimerRefreshAt;
        RectTransform _centerStageNode;
        RectTransform _previousStageNode;
        RectTransform _nextStageNode;
        TextMeshProUGUI _previousStageStarsText;
        TextMeshProUGUI _nextStageStarsText;
        Coroutine _stageAnimation;
        bool _stageSelectionInitialized;
        Button _rankingStagesButton;
        Button _rankingScoreButton;
        TextMeshProUGUI _pinnedRankText;
        TextMeshProUGUI _pinnedScoreText;

        void Awake()
        {
            SceneLoader.Instance?.HoldForReady();
            ResolveMissingReferences();
            _catalog = UiServiceLocator.Catalog;
            _uiData = UiServiceLocator.UiData;
            _maxSelectableStageId = Mathf.Max(1, StageLoader.MaxStageId);
            _viewModel = new LobbyViewModel(_uiData, _catalog);
            _viewModel.Changed += Render;
            BindStageNavigation();
        }

        void Start()
        {
            int pending = 4;
            void OnLoadDone()
            {
                if (--pending == 0)
                    SceneLoader.Instance?.NotifyReady();
            }

            _viewModel.LoadLobby(OnLoadDone);
            _uiData.GetProgress(result => { ApplyProgress(result); OnLoadDone(); });
            _viewModel.LoadShop(OnLoadDone);
            _viewModel.LoadRanking(DefaultRankingCategory, OnLoadDone);
        }

        void OnDestroy()
        {
            if (_viewModel != null)
                _viewModel.Changed -= Render;
        }

        void Update()
        {
            if (_nextRechargeAt == null || Time.unscaledTime < _nextTimerRefreshAt)
                return;

            _nextTimerRefreshAt = Time.unscaledTime + 1f;
            RefreshStaminaTimer();
        }

        public void RefreshRanking(string category)
        {
            ClearChildren(rankingContent);
            SetText(rankingErrorText, "");
            _viewModel.LoadRanking(category);
        }

        void Render()
        {
            if (_viewModel == null) return;

            if (!string.IsNullOrEmpty(_viewModel.ErrorCode))
                SetText(playDisabledReasonText, LocalizationManager.GetError(_viewModel.ErrorCode));

            if (_viewModel.Lobby != null)
                ApplyLobby(_viewModel.Lobby);

            if (_viewModel.Shop != null)
                ApplyShop(_viewModel.Shop);

            if (_viewModel.Ranking != null)
                ApplyRanking(_viewModel.Ranking);
        }

        void ApplyShop(ShopScreenModel model)
        {
            SetText(shopBalanceText, FormatNumber(model.SoftBalance));
            ClearRows(shopContent, "Product_");

            foreach (var product in model.Products)
            {
                string title = string.IsNullOrEmpty(product.ItemName) ? product.Name : product.ItemName;
                string price = product.IsIapProduct ? product.PriceIapSku : FormatNumber(product.PriceSoft);
                AddRow(shopContent, $"Product_{product.ProductId}", title, price, true);
            }
        }

        void ApplyLobby(LobbyScreenModel lobby)
        {
            int currentStageId = Mathf.Max(1, lobby.NextUnlockedStageId);
            if (lobby.HighestStageId > 0)
                currentStageId = Mathf.Max(currentStageId, lobby.HighestStageId + 1);
            currentStageId = Mathf.Clamp(currentStageId, 1, _maxSelectableStageId);

            if (!_stageSelectionInitialized)
            {
                _selectedStageId = currentStageId;
                _stageSelectionInitialized = true;
            }
            _selectedStageId = Mathf.Clamp(_selectedStageId, 1, _maxSelectableStageId);
            GameContext.SelectedStageId = _selectedStageId;
            _nextRechargeAt = ParseDateTime(lobby.NextRechargeAt);

            SetText(profileNameText, string.IsNullOrEmpty(lobby.DisplayName) ? "Guest" : lobby.DisplayName);
            SetText(energyText, $"{lobby.StaminaCurrent}/{lobby.StaminaMax}");
            RefreshStaminaTimer();
            SetText(coinText, FormatNumber(lobby.SoftCurrency));
            RefreshStageCarousel();
            SetText(dailyProgressText, $"{lobby.DailyChallenge.PlayCountToday}/{lobby.DailyChallenge.PlayCountTarget}");
            SetText(colorCupTimerText, lobby.SeasonEvent?.EndAt ?? "");

            if (refillButton != null)
                refillButton.gameObject.SetActive(!lobby.CanPlay);
            RefreshPlayState(lobby);
        }

        void ApplyProgress(ServiceResult<ProgressResponse> result)
        {
            if (!result.IsSuccess || result.Value == null)
                return;

            _progressLoaded = true;
            _stageStars.Clear();
            _unlockedStages.Clear();

            for (int i = 0; i < result.Value.Stages.Count; i++)
            {
                var entry = result.Value.Stages[i];
                _stageStars[entry.StageId] = Mathf.Clamp(entry.Stars, 0, 3);
                if (entry.IsUnlocked)
                    _unlockedStages.Add(entry.StageId);
            }

            RefreshStageCarousel();
        }

        void ApplyRanking(RankingListResponse ranking)
        {
            ClearRows(rankingContent, "TopRankCard", "Rank_", "MyRankPin");
            SetText(rankingMetricText, ranking.MetricLabel);
            RefreshRankingSegmentState(ranking.Category);

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
            {
                AddRow(rankingContent, "MyRankPin", $"#{ranking.MyRank.Rank} {ranking.MyRank.DisplayName}", FormatNumber(ranking.MyRank.Value), true);
                SetText(_pinnedRankText, $"#{ranking.MyRank.Rank}");
                SetText(_pinnedScoreText, FormatNumber(ranking.MyRank.Value));
            }
            else
            {
                SetText(_pinnedRankText, "#--");
                SetText(_pinnedScoreText, "0");
            }
        }

        void ResolveMissingReferences()
        {
            profileNameText ??= FindText("Txt_Nickname");
            energyText ??= FindText("Txt_StaminaCount");
            staminaTimerText ??= FindText("Txt_StaminaTimer");
            coinText ??= FindText("Txt_CurrencyCount");
            stageNumberText ??= FindText("Txt_StageNum");
            starsText ??= FindText("Txt_Stars");
            dailyProgressText ??= FindText("Txt_Frac");
            colorCupTimerText ??= FindText("Txt_Ends");
            playDisabledReasonText ??= FindText("Txt_PlayDisabled");
            shopBalanceText ??= FindTextInParent("Row_Balance", "Txt_Balance") ?? FindText("Txt_Balance");
            rankingMetricText ??= FindText("Txt_Score");
            rankingErrorText ??= FindText("Txt_RankError");
            shopContent ??= FindRectInPanel("Tab_Shop", "Content") ?? FindRect("Content");
            rankingContent ??= FindRectInPanel("Tab_Ranking", "Content") ?? FindRect("Content");
            playButton ??= FindButton("Btn_Play");
            refillButton ??= FindButton("Btn_Refill");
            previousStageButton ??= FindButton("Btn_Prev");
            nextStageButton ??= FindButton("Btn_Next");
            _rankingStagesButton ??= FindButton("Seg_Clear");
            _rankingScoreButton ??= FindButton("Seg_Score");
            _pinnedRankText ??= FindTextInParent("Row_MyRank_Pinned", "Txt_Rank");
            _pinnedScoreText ??= FindTextInParent("Row_MyRank_Pinned", "Txt_Score");
            EnsureSideStageNodes();
        }

        void BindStageNavigation()
        {
            if (previousStageButton != null)
            {
                previousStageButton.onClick.AddListener(SelectPreviousStage);
                AddRepeat(previousStageButton).Repeated.AddListener(SelectPreviousStage);
            }

            if (nextStageButton != null)
            {
                nextStageButton.onClick.AddListener(SelectNextStage);
                AddRepeat(nextStageButton).Repeated.AddListener(SelectNextStage);
            }

            if (_rankingStagesButton != null)
                _rankingStagesButton.onClick.AddListener(() => RefreshRanking(DefaultRankingCategory));
            if (_rankingScoreButton != null)
                _rankingScoreButton.onClick.AddListener(() => RefreshRanking(ScoreRankingCategory));
        }

        void SelectPreviousStage()
        {
            if (_selectedStageId <= 1) return;
            _stageSelectionInitialized = true;
            _selectedStageId--;
            RefreshStageCarousel();
        }

        void SelectNextStage()
        {
            if (_selectedStageId >= _maxSelectableStageId) return;
            _stageSelectionInitialized = true;
            _selectedStageId++;
            RefreshStageCarousel();
        }

        void RefreshStageCarousel()
        {
            _selectedStageId = Mathf.Clamp(_selectedStageId <= 0 ? 1 : _selectedStageId, 1, _maxSelectableStageId);
            GameContext.SelectedStageId = _selectedStageId;
            SetText(stageNumberText, _selectedStageId.ToString(CultureInfo.InvariantCulture));

            bool hasPrev = _selectedStageId > 1;
            bool hasNext = _selectedStageId < _maxSelectableStageId;

            SetText(previousStageNumberText, hasPrev ? (_selectedStageId - 1).ToString(CultureInfo.InvariantCulture) : "");
            SetText(nextStageNumberText,     hasNext ? (_selectedStageId + 1).ToString(CultureInfo.InvariantCulture) : "");
            SetText(starsText, FormatStageStars(_selectedStageId));
            SetText(_previousStageStarsText, hasPrev ? FormatStageStars(_selectedStageId - 1) : "");
            SetText(_nextStageStarsText, hasNext ? FormatStageStars(_selectedStageId + 1) : "");

            if (previousStageButton != null)
                previousStageButton.interactable = hasPrev;
            if (nextStageButton != null)
                nextStageButton.interactable = hasNext;

            if (_previousStageNode != null)
                _previousStageNode.gameObject.SetActive(hasPrev);
            if (_nextStageNode != null)
                _nextStageNode.gameObject.SetActive(hasNext);

            RefreshPlayState(_viewModel?.Lobby);
            StartStageSwitchAnimation();
        }

        void RefreshRankingSegmentState(string category)
        {
            bool scoreSelected = string.Equals(category, "GLOBAL_SCORE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(_viewModel?.RankingCategory, ScoreRankingCategory, StringComparison.OrdinalIgnoreCase);
            SetSegmentSelected(_rankingStagesButton, !scoreSelected);
            SetSegmentSelected(_rankingScoreButton, scoreSelected);
        }

        void RefreshPlayState(LobbyScreenModel lobby)
        {
            if (lobby == null)
                return;

            bool unlocked = IsStageUnlocked(_selectedStageId, lobby);
            bool canPlay = lobby.CanPlay && unlocked;
            if (playButton != null)
                playButton.interactable = canPlay;

            if (!lobby.CanPlay)
                SetText(playDisabledReasonText, LocalizationManager.Get("status.energy_empty"));
            else
                SetText(playDisabledReasonText, "");
        }

        bool IsStageUnlocked(int stageId, LobbyScreenModel lobby)
        {
            if (_progressLoaded)
                return _unlockedStages.Contains(stageId);

            return stageId == 1 || stageId <= Mathf.Max(lobby.NextUnlockedStageId, lobby.HighestStageId);
        }

        string FormatStageStars(int stageId)
        {
            return _stageStars.TryGetValue(stageId, out var stars)
                ? stars.ToString(CultureInfo.InvariantCulture)
                : "0";
        }

        void RefreshStaminaTimer()
        {
            if (staminaTimerText == null)
                return;

            if (_nextRechargeAt == null)
            {
                staminaTimerText.text = "";
                return;
            }

            var remaining = _nextRechargeAt.Value - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                staminaTimerText.text = "00:00";
                return;
            }

            int totalSeconds = Mathf.CeilToInt((float)remaining.TotalSeconds);
            staminaTimerText.text = $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
        }

        void EnsureSideStageNodes()
        {
            if (stageNumberText == null || (previousStageNumberText != null && nextStageNumberText != null))
                return;

            var center = stageNumberText.transform.parent as RectTransform;
            var track = center != null ? center.parent as RectTransform : null;
            if (center == null || track == null)
                return;

            // Reduce spacing and allow track to overflow its container
            var trackLayout = track.GetComponent<HorizontalLayoutGroup>();
            if (trackLayout != null)
            {
                trackLayout.spacing = 16f;
                trackLayout.childAlignment = TextAnchor.MiddleCenter;
                trackLayout.childForceExpandWidth = false;
            }
            var trackFitter = track.GetComponent<ContentSizeFitter>();
            if (trackFitter == null)
                trackFitter = track.gameObject.AddComponent<ContentSizeFitter>();
            trackFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var prev = FindOrCloneNode(track, center, "StageNode_Prev", 0);
            var next = FindOrCloneNode(track, center, "StageNode_Next", 2);
            center.SetSiblingIndex(1);
            _centerStageNode = center;
            _previousStageNode = prev;
            _nextStageNode = next;
            previousStageNumberText ??= prev?.transform.Find("Txt_StageNum")?.GetComponent<TextMeshProUGUI>();
            nextStageNumberText ??= next?.transform.Find("Txt_StageNum")?.GetComponent<TextMeshProUGUI>();
            starsText = center.transform.Find("Txt_Stars")?.GetComponent<TextMeshProUGUI>() ?? starsText;
            _previousStageStarsText = prev?.transform.Find("Txt_Stars")?.GetComponent<TextMeshProUGUI>();
            _nextStageStarsText = next?.transform.Find("Txt_Stars")?.GetComponent<TextMeshProUGUI>();
        }

        void StartStageSwitchAnimation()
        {
            if (!isActiveAndEnabled || _centerStageNode == null)
                return;

            if (_stageAnimation != null)
                StopCoroutine(_stageAnimation);
            _stageAnimation = StartCoroutine(StageSwitchAnimation());
        }

        IEnumerator StageSwitchAnimation()
        {
            const float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                if (_centerStageNode != null)
                    _centerStageNode.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, eased);
                if (_previousStageNode != null)
                    _previousStageNode.localScale = Vector3.one * Mathf.Lerp(0.62f, 0.72f, eased);
                if (_nextStageNode != null)
                    _nextStageNode.localScale = Vector3.one * Mathf.Lerp(0.62f, 0.72f, eased);
                yield return null;
            }

            if (_centerStageNode != null)
                _centerStageNode.localScale = Vector3.one;
            if (_previousStageNode != null)
                _previousStageNode.localScale = Vector3.one * 0.72f;
            if (_nextStageNode != null)
                _nextStageNode.localScale = Vector3.one * 0.72f;
            _stageAnimation = null;
        }

        static RectTransform FindOrCloneNode(RectTransform track, RectTransform center, string name, int siblingIndex)
        {
            var existing = track.Find(name) as RectTransform;
            if (existing == null)
            {
                var clone = Instantiate(center.gameObject, track, false);
                clone.name = name;
                existing = clone.GetComponent<RectTransform>();
                foreach (var button in clone.GetComponentsInChildren<Button>(true))
                    button.interactable = false;
            }

            existing.SetSiblingIndex(siblingIndex);
            existing.localScale = Vector3.one * 0.72f;
            var image = existing.GetComponent<Image>();
            if (image != null)
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0.55f);
            return existing;
        }

        static RepeatButton AddRepeat(Button button)
        {
            var repeat = button.GetComponent<RepeatButton>();
            return repeat != null ? repeat : button.gameObject.AddComponent<RepeatButton>();
        }

        static DateTimeOffset? ParseDateTime(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed.ToUniversalTime()
                : null;
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

        RectTransform FindRectInPanel(string panelName, string childName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name != panelName || rect.GetComponent<Button>() != null)
                    continue;

                foreach (var child in rect.GetComponentsInChildren<RectTransform>(true))
                {
                    if (child.name == childName)
                        return child;
                }
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

        TextMeshProUGUI FindTextInParent(string parentName, string childName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name != parentName)
                    continue;

                var child = rect.Find(childName);
                return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
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

        static void ClearRows(RectTransform parent, params string[] prefixes)
        {
            if (parent == null || prefixes == null) return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                for (int p = 0; p < prefixes.Length; p++)
                {
                    if (!child.name.StartsWith(prefixes[p], StringComparison.Ordinal))
                        continue;

                    Destroy(child.gameObject);
                    break;
                }
            }
        }

        static void SetSegmentSelected(Button button, bool selected)
        {
            if (button == null) return;

            var image = button.targetGraphic as Image;
            if (image != null)
                image.color = selected ? new Color(0.12f, 0.46f, 0.9f, 0.42f) : new Color(0, 0, 0, 0);
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
