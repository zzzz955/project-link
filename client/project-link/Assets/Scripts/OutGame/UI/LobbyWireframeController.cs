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
        [SerializeField] TextMeshProUGUI colorCupTimerText;
        [SerializeField] StreakChallengeBadge streakBadge;
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
        bool _staminaFull;
        bool _firstLobbyApply = true;
        Coroutine _energyAnim;
        Coroutine _coinAnim;
        [SerializeField] Sprite starOnSprite;
        [SerializeField] Sprite starOffSprite;
        [SerializeField] Sprite[] difficultySprites;
        [SerializeField] Sprite lockSprite;
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
        TextMeshProUGUI _pinnedYouText;
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

        void OnEnable()  { LocalizationManager.LanguageChanged += OnLanguageChanged; }
        void OnDisable() { LocalizationManager.LanguageChanged -= OnLanguageChanged; }

        void OnDestroy()
        {
            if (_viewModel != null)
                _viewModel.Changed -= Render;
            LocalizationManager.LanguageChanged -= OnLanguageChanged;
        }

        void OnLanguageChanged()
        {
            RefreshStaminaTimer();
            RefreshPlayState(_viewModel?.Lobby);
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
            ConfigureShopGrid();
            ClearRows(shopContent, "Product_", "Section_");

            for (int i = 0; i < model.Products.Count; i++)
            {
                var p = model.Products[i];
                var title = p.Category == "COIN" && p.GrantQuantity > 0
                    ? FormatNumber(p.GrantQuantity)
                    : p.ItemName ?? p.Name;
                var price = p.PriceSoft > 0 ? FormatNumber(p.PriceSoft) : p.PriceIapSku;
                AddProductCard(shopContent, $"Product_{p.ProductId}", title, price, i == 1);
            }
        }

        void ConfigureShopGrid()
        {
            if (shopContent == null) return;

            var balance = shopContent.Find("Row_Balance");
            if (balance != null) balance.gameObject.SetActive(false);
            var header = shopContent.Find("Header_Stamina");
            if (header != null) header.gameObject.SetActive(false);

            var vertical = shopContent.GetComponent<VerticalLayoutGroup>();
            if (vertical != null) DestroyImmediate(vertical);
            var fitter = shopContent.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = shopContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var grid = shopContent.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = shopContent.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(280f, 500f);
            grid.spacing = new Vector2(24f, 24f);
            grid.padding = new RectOffset(16, 16, 16, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
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

            _staminaFull = lobby.StaminaCurrent >= lobby.StaminaMax;
            if (_firstLobbyApply)
            {
                _firstLobbyApply = false;
                if (_energyAnim != null) StopCoroutine(_energyAnim);
                _energyAnim = StartCoroutine(AnimateCount(energyText, lobby.StaminaCurrent, 0.8f,
                    v => v.ToString(CultureInfo.InvariantCulture)));
                if (_coinAnim != null) StopCoroutine(_coinAnim);
                _coinAnim = StartCoroutine(AnimateCount(coinText, lobby.SoftCurrency, 0.8f, FormatNumber));
            }
            else
            {
                SetText(energyText, lobby.StaminaCurrent.ToString(CultureInfo.InvariantCulture));
                SetText(coinText, FormatNumber(lobby.SoftCurrency));
            }
            RefreshStaminaTimer();
            RefreshStageCarousel();
            var sc = lobby.StreakChallenge;
            streakBadge?.Apply(sc?.EventStatus, sc?.HasPendingReward ?? false, sc?.RemainingTimeIso ?? "");

            if (GameContext.ShouldOpenStreakPopupOnLobby)
            {
                GameContext.ShouldOpenStreakPopupOnLobby = false;
                PopupManager.Request(PopupId.StreakChallenge);
            }
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
            ClearRows(rankingContent, "Rank_");
            SetText(rankingMetricText, ranking.MetricLabel);
            RefreshRankingSegmentState(ranking.Category);

            for (int i = 0; i < ranking.Entries.Count; i++)
            {
                var entry = ranking.Entries[i];
                AddRow(rankingContent, $"Rank_{entry.Rank}", $"#{entry.Rank} {entry.DisplayName}", FormatNumber(entry.Value), entry.IsMe);
            }

            if (ranking.MyRank != null)
            {
                SetText(_pinnedRankText, $"#{ranking.MyRank.Rank}");
                SetText(_pinnedScoreText, FormatNumber(ranking.MyRank.Value));
                var name = ranking.MyRank.DisplayName;
                SetText(_pinnedYouText, string.IsNullOrEmpty(name)
                    ? LocalizationManager.Get("popup.account.guest")
                    : name);
            }
            else
            {
                SetText(_pinnedRankText, "#--");
                SetText(_pinnedScoreText, "0");
                SetText(_pinnedYouText, LocalizationManager.Get("popup.account.guest"));
            }
        }

        void ResolveMissingReferences()
        {
            energyText ??= FindText("Txt_StaminaCount");
            staminaTimerText ??= FindText("Txt_StaminaTimer");
            coinText ??= FindText("Txt_CurrencyCount");
            stageNumberText ??= FindText("Txt_StageNum");
            starsText ??= FindText("Txt_Stars");
            colorCupTimerText ??= FindText("Txt_Ends");
            streakBadge ??= GetComponentInChildren<StreakChallengeBadge>(true);
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
            _pinnedYouText ??= FindTextInParent("Row_MyRank_Pinned", "Txt_You");
            _pinnedScoreText ??= FindTextInParent("Row_MyRank_Pinned", "Txt_Score");
            EnsureSideStageNodes();
        }

        void BindAvatarButton()
        {
            var avatarBtn = FindButton("Slot_Avatar");
            if (avatarBtn != null)
                avatarBtn.onClick.AddListener(() => PopupManager.Request(PopupId.Account));
        }

        void BindStageNavigation()
        {
            BindAvatarButton();
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
            // starsText kept for fallback; primary rendering via Img_Star_* images
            SetText(starsText, FormatStageStars(_selectedStageId));
            SetText(_previousStageStarsText, hasPrev ? FormatStageStars(_selectedStageId - 1) : "");
            SetText(_nextStageStarsText, hasNext ? FormatStageStars(_selectedStageId + 1) : "");

            if (_centerStageNode != null)
            {
                RenderStarImages(_centerStageNode, _selectedStageId);
                RenderDifficultySprite(_centerStageNode, _selectedStageId);
                RenderLockIcon(_centerStageNode, _selectedStageId);
            }
            if (_previousStageNode != null && hasPrev)
            {
                RenderStarImages(_previousStageNode, _selectedStageId - 1);
                RenderDifficultySprite(_previousStageNode, _selectedStageId - 1);
                RenderLockIcon(_previousStageNode, _selectedStageId - 1);
            }
            if (_nextStageNode != null && hasNext)
            {
                RenderStarImages(_nextStageNode, _selectedStageId + 1);
                RenderDifficultySprite(_nextStageNode, _selectedStageId + 1);
                RenderLockIcon(_nextStageNode, _selectedStageId + 1);
            }

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

        void RenderStarImages(RectTransform node, int stageId)
        {
            var starsRow = node.Find("Group_Stars") as RectTransform;
            if (starsRow == null) return;
            int earned = _stageStars.TryGetValue(stageId, out var s) ? s : 0;
            for (int i = 0; i < 3; i++)
            {
                var slot = starsRow.Find($"Img_Star_{i}");
                if (slot == null) continue;
                var img = slot.GetComponent<UnityEngine.UI.Image>();
                if (img == null) continue;
                bool filled = i < earned;
                var sprite = filled ? starOnSprite : starOffSprite;
                if (sprite != null) { img.sprite = sprite; img.color = Color.white; img.preserveAspect = true; }
                else img.color = filled ? new Color(1f, 0.82f, 0.15f, 1f) : new Color(1f, 1f, 1f, 0.25f);
            }
        }

        void RenderDifficultySprite(RectTransform node, int stageId)
        {
            if (difficultySprites == null || difficultySprites.Length == 0) return;
            var img = node.GetComponent<UnityEngine.UI.Image>();
            if (img == null) return;
            int difficulty = Mathf.Clamp(StageLoader.GetDifficulty(stageId), 1, difficultySprites.Length);
            var sprite = difficultySprites[difficulty - 1];
            if (sprite != null) { img.sprite = sprite; img.color = Color.white; img.preserveAspect = false; }
        }

        void RenderLockIcon(RectTransform node, int stageId)
        {
            var lockIconTf = node.Find("LockIcon");
            if (lockIconTf == null) return;
            bool locked = _progressLoaded && !_unlockedStages.Contains(stageId);
            lockIconTf.gameObject.SetActive(locked);
            if (locked && lockSprite != null)
            {
                var img = lockIconTf.GetComponent<UnityEngine.UI.Image>();
                if (img != null) { img.sprite = lockSprite; img.color = Color.white; }
            }
        }

        void RefreshStaminaTimer()
        {
            if (staminaTimerText == null)
                return;

            if (_staminaFull || _nextRechargeAt == null)
            {
                staminaTimerText.text = _staminaFull ? LocalizationManager.Get("status.stamina_full") : "";
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

        static void AddSectionHeader(RectTransform parent, string name, string label)
        {
            if (parent == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 28f;
            txt.fontStyle = FontStyles.Bold;
            txt.color = new Color(0.63f, 0.68f, 0.76f, 1f);
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            txt.raycastTarget = false;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 56f;
            go.AddComponent<LocalizedFont>();
        }

        static void AddProductCard(RectTransform parent, string name, string title, string price, bool best)
        {
            if (parent == null) return;
            var card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            card.transform.SetParent(parent, false);
            card.GetComponent<Image>().color = new Color(0.06f, 0.16f, 0.28f, 0.9f);
            var element = card.GetComponent<LayoutElement>();
            element.preferredWidth = 280f;
            element.preferredHeight = 500f;

            var rect = card.GetComponent<RectTransform>();
            // Title — top-left
            var titleGo = new GameObject("Txt_Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(rect, false);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 88f);
            titleRect.anchoredPosition = new Vector2(0f, -36f);
            var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
            titleTmp.text = title;
            titleTmp.fontSize = 46f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.82f, 0.08f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;
            titleGo.AddComponent<LocalizedFont>();

            // Price — bottom-right
            var iconGo = new GameObject("Icon_Wire", typeof(RectTransform), typeof(TextMeshProUGUI));
            iconGo.transform.SetParent(rect, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(190f, 190f);
            iconRect.anchoredPosition = new Vector2(0f, 16f);
            var iconTmp = iconGo.GetComponent<TextMeshProUGUI>();
            iconTmp.text = "*";
            iconTmp.fontSize = 150f;
            iconTmp.fontStyle = FontStyles.Bold;
            iconTmp.color = new Color(1f, 0.75f, 0.02f, 1f);
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.raycastTarget = false;

            var priceGo = new GameObject("Txt_Price", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceGo.transform.SetParent(rect, false);
            var priceRect = priceGo.GetComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(0f, 0f);
            priceRect.anchorMax = new Vector2(1f, 0f);
            priceRect.pivot = new Vector2(0.5f, 0f);
            priceRect.sizeDelta = new Vector2(-36f, 96f);
            priceRect.anchoredPosition = new Vector2(0f, 26f);
            var priceTmp = priceGo.GetComponent<TextMeshProUGUI>();
            priceTmp.text = price;
            priceTmp.fontSize = 36f;
            priceTmp.fontStyle = FontStyles.Bold;
            priceTmp.color = Color.white;
            priceTmp.alignment = TextAlignmentOptions.Center;
            priceTmp.raycastTarget = false;
            priceGo.AddComponent<LocalizedFont>();
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
            go.AddComponent<LocalizedFont>();
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

        IEnumerator AnimateCount(TextMeshProUGUI label, long target, float duration, Func<long, string> formatter)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                if (label != null) label.text = formatter((long)(t * target));
                yield return null;
            }
            if (label != null) label.text = formatter(target);
        }
    }
}
