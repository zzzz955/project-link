#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using ProjectLink.Core;
using ProjectLink.Contracts.StreakChallenge;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CatalogLevel = ProjectLink.Data.Generated.StreakChallengeLevel;
using CatalogRewardItem = ProjectLink.Data.Generated.StreakChallengeRewardItem;

namespace ProjectLink.OutGame.UI
{
    public sealed class StreakChallengePopup : PopupBase
    {
        [Header("Buttons")]
        [SerializeField] Button? closeButton;
        [SerializeField] Button? infoButton;
        [SerializeField] Button? activateButton;

        [Header("Static Layout")]
        [SerializeField] GameObject? infoPopup;
        [SerializeField] Image? bannerImage;
        [SerializeField] TextMeshProUGUI? timerText;
        [SerializeField] TextMeshProUGUI? levelText;
        [SerializeField] TextMeshProUGUI? prizeTitleText;
        [SerializeField] Image? prizeIcon;
        [SerializeField] TextMeshProUGUI? prizeAmountText;
        [SerializeField] RectTransform? levelListRoot;

        [Header("Dynamic Sprites")]
        [SerializeField] Sprite? pathLineSprite;
        [SerializeField] Sprite? pathNodeSprite;
        [SerializeField] Sprite? pathNodeDoneSprite;
        [SerializeField] Sprite? platformSprite;
        [SerializeField] Sprite? softCurrencySprite;
        [SerializeField] Sprite? itemRewardSprite;

        bool _initialized;
        StreakChallengeStateResponse? _state;
        TextMeshProUGUI? _actionLabel;
        DateTimeOffset _expiresAt;
        Coroutine? _timerLoop;

        const float PathHeight = 660f;
        const float NodeWidth = 220f;
        const float NodeHeight = 112f;
        const float XRange = 250f;
        const float YMargin = 64f;

        static readonly Color PanelText = new Color(1f, 1f, 1f, 1f);
        static readonly Color MutedText = new Color(0.82f, 0.91f, 1f, 1f);
        static readonly Color Gold = new Color(1f, 0.78f, 0.12f, 1f);
        static readonly Color Purple = new Color(0.48f, 0.12f, 0.88f, 0.96f);
        static readonly Color Cleared = new Color(0.16f, 0.92f, 0.46f, 1f);
        static readonly Color Pending = new Color(0.92f, 0.36f, 1f, 1f);
        static readonly Color Locked = new Color(0.36f, 0.42f, 0.55f, 0.72f);

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;
            ResolveRefs();
            BindOverlayClose();
            if (closeButton != null) closeButton.onClick.AddListener(CloseTop);
            if (infoButton != null) infoButton.onClick.AddListener(ToggleInfoPopup);
            Refresh();
        }

        void OnDisable()
        {
            StopTimer();
        }

        void Refresh()
        {
            UiServiceLocator.UiData.GetStreakChallengeState(result =>
            {
                if (!result.IsSuccess)
                {
                    SetText(levelText, result.ErrorCode);
                    return;
                }

                _state = result.Value;
                Apply(_state);
            });
        }

        void Apply(StreakChallengeStateResponse state)
        {
            StopTimer();
            if (infoPopup != null) infoPopup.SetActive(false);

            int levelIndex = GetRenderLevelIndex(state);
            var levelState = FindLevelState(state, levelIndex);
            int required = GetRequiredCount(state, levelIndex, levelState);
            int current = Mathf.Clamp(levelState?.CurrentCount ?? 0, 0, required);

            SetText(levelText, string.Format(LocalizationManager.Get("streak.level_progress_fmt"),
                levelIndex + 1, current, required));
            SetText(prizeTitleText, LocalizationManager.Get("streak.grand_prize"));
            RenderPrize(state, levelIndex);

            if (levelListRoot != null)
            {
                ClearChildren(levelListRoot);
                DrawPath(levelListRoot, required, current);
            }

            UpdateTimer(state);
            UpdateActionButton(state);
        }

        int GetRenderLevelIndex(StreakChallengeStateResponse state)
        {
            int pending = GetLevelIndexByReward(state, "PENDING");
            if (pending >= 0) return pending;

            if (state.EventStatus == "COMPLETED" && state.Levels.Count > 0)
                return state.Levels[state.Levels.Count - 1].LevelIndex;

            if (state.CurrentLevel > 0 || state.EventStatus != "INACTIVE")
                return state.CurrentLevel;

            return 0;
        }

        void RenderPrize(StreakChallengeStateResponse state, int levelIndex)
        {
            var level = FindCatalogLevel(state, levelIndex);
            IReadOnlyList<CatalogRewardItem> rewards = level != null
                ? UiServiceLocator.Catalog.GetStreakChallengeRewardItems(level.rewardGroupId)
                : Array.Empty<CatalogRewardItem>();

            if (rewards.Count == 0)
            {
                SetText(prizeAmountText, "");
                if (prizeIcon != null) prizeIcon.enabled = false;
                return;
            }

            var reward = rewards[0];
            if (prizeIcon != null)
            {
                prizeIcon.enabled = true;
                prizeIcon.sprite = reward.itemType == "SOFT_CURRENCY" ? softCurrencySprite : itemRewardSprite;
                prizeIcon.color = prizeIcon.sprite == null ? Gold : Color.white;
                prizeIcon.preserveAspect = true;
            }

            string label = reward.itemType == "SOFT_CURRENCY"
                ? string.Format(LocalizationManager.Get("streak.reward_soft_currency_fmt"), reward.amount)
                : FormatItemReward(reward);
            SetText(prizeAmountText, label);
        }

        string FormatItemReward(CatalogRewardItem reward)
        {
            var item = reward.itemId > 0 ? UiServiceLocator.Catalog.FindItem(reward.itemId) : null;
            string name = item != null ? item.name : LocalizationManager.Get("streak.reward_item");
            return string.Format(LocalizationManager.Get("streak.reward_item_fmt"), reward.amount, name);
        }

        CatalogLevel? FindCatalogLevel(StreakChallengeStateResponse state, int levelIndex)
        {
            var levels = UiServiceLocator.Catalog.GetStreakChallengeLevels(state.EventId, state.EventVersion);
            if (levels.Count == 0)
                levels = UiServiceLocator.Catalog.GetStreakChallengeLevels();

            foreach (var level in levels)
                if (level.levelIndex == levelIndex)
                    return level;

            return null;
        }

        int GetRequiredCount(StreakChallengeStateResponse state, int levelIndex, StreakChallengeLevelState? levelState)
        {
            if (levelState != null && levelState.RequiredCount > 0)
                return levelState.RequiredCount;

            var level = FindCatalogLevel(state, levelIndex);
            return level?.requiredClearCount ?? 1;
        }

        static StreakChallengeLevelState? FindLevelState(StreakChallengeStateResponse state, int levelIndex)
        {
            foreach (var level in state.Levels)
                if (level.LevelIndex == levelIndex)
                    return level;

            return null;
        }

        void DrawPath(RectTransform parent, int required, int current)
        {
            required = Mathf.Max(1, required);
            float yBot = -PathHeight * 0.5f + YMargin;
            float yTop = PathHeight * 0.5f - YMargin;
            var positions = new Vector2[required];

            for (int i = 0; i < required; i++)
            {
                float t = required > 1 ? (float)i / (required - 1) : 1f;
                float y = Mathf.Lerp(yBot, yTop, t);
                float x = ((i & 1) == 0 ? -1f : 1f) * Mathf.Lerp(XRange, 42f, t);
                positions[i] = new Vector2(x, y);
            }

            for (int i = 0; i < required - 1; i++)
                DrawLine(parent, positions[i], positions[i + 1], i + 1 <= current);

            for (int i = 0; i < required; i++)
                DrawPlatform(parent, positions[i], i + 1, i < current, i == current);
        }

        void DrawLine(RectTransform parent, Vector2 from, Vector2 to, bool cleared)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            var diff = to - from;
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.sizeDelta = new Vector2(16f, diff.magnitude);
            rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.x, diff.y) * Mathf.Rad2Deg);

            var image = go.GetComponent<Image>();
            image.sprite = pathLineSprite;
            image.color = cleared ? Cleared : new Color(1f, 1f, 1f, 0.28f);
            image.type = pathLineSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
        }

        void DrawPlatform(RectTransform parent, Vector2 pos, int number, bool cleared, bool current)
        {
            var go = new GameObject($"Step_{number}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(NodeWidth, NodeHeight);

            var image = go.GetComponent<Image>();
            image.sprite = cleared ? pathNodeDoneSprite ?? platformSprite : pathNodeSprite ?? platformSprite;
            image.color = image.sprite != null ? Color.white : (cleared ? Cleared : current ? Pending : Locked);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var label = AddText(go.transform, "Txt_Index", 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(label.rectTransform);
            label.text = cleared ? LocalizationManager.Get("streak.step_done") : number.ToString();
            label.color = Color.white;
        }

        void UpdateTimer(StreakChallengeStateResponse state)
        {
            if (state.EventStatus == "ACTIVE" && !string.IsNullOrEmpty(state.ExpiresAtIso))
            {
                if (DateTimeOffset.TryParse(state.ExpiresAtIso, out _expiresAt))
                {
                    RenderTimer();
                    _timerLoop = StartCoroutine(TimerLoop());
                    return;
                }
            }

            SetText(timerText, string.Format(LocalizationManager.Get("streak.remaining_fmt"), 0, 0));
        }

        IEnumerator TimerLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(60f);
                RenderTimer();
            }
        }

        void RenderTimer()
        {
            var remaining = _expiresAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                SetText(timerText, string.Format(LocalizationManager.Get("streak.remaining_fmt"), 0, 0));
                StopTimer();
                return;
            }

            int totalMinutes = Mathf.Max(0, (int)Math.Floor(remaining.TotalMinutes));
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            SetText(timerText, string.Format(LocalizationManager.Get("streak.remaining_fmt"), hours, minutes));
        }

        void StopTimer()
        {
            if (_timerLoop == null) return;
            StopCoroutine(_timerLoop);
            _timerLoop = null;
        }

        void UpdateActionButton(StreakChallengeStateResponse state)
        {
            if (activateButton == null) return;
            activateButton.onClick.RemoveAllListeners();
            activateButton.gameObject.SetActive(true);

            if (state.AvailableActions.Contains("ACTIVATE"))
            {
                SetAction(LocalizationManager.Get("streak.activate"), true);
                activateButton.onClick.AddListener(OnActivateAndStart);
                return;
            }

            if (state.AvailableActions.Contains("CLAIM_REWARD"))
            {
                int level = GetLevelIndexByReward(state, "PENDING");
                SetAction(LocalizationManager.Get("streak.claim"), true);
                activateButton.onClick.AddListener(() => OnClaimReward(level));
                return;
            }

            if (state.AvailableActions.Contains("START_LEVEL"))
            {
                int level = GetLevelIndexByStatus(state, "READY");
                SetAction(string.Format(LocalizationManager.Get("streak.start_level"), level + 1), true);
                activateButton.onClick.AddListener(() => OnStartLevel(level));
                return;
            }

            SetAction(LocalizationManager.Get("streak.claim"), false);
        }

        void SetAction(string label, bool interactive)
        {
            if (_actionLabel != null) _actionLabel.text = label;
            if (activateButton != null) activateButton.interactable = interactive;
        }

        static int GetLevelIndexByStatus(StreakChallengeStateResponse state, string status)
        {
            foreach (var level in state.Levels)
                if (level.LevelStatus == status)
                    return level.LevelIndex;

            return state.CurrentLevel;
        }

        static int GetLevelIndexByReward(StreakChallengeStateResponse state, string rewardState)
        {
            foreach (var level in state.Levels)
                if (level.RewardState == rewardState)
                    return level.LevelIndex;

            return -1;
        }

        void OnActivateAndStart()
        {
            SetBusy();
            UiServiceLocator.UiData.ActivateStreakChallenge(result =>
            {
                if (!result.IsSuccess)
                {
                    SetAction(LocalizationManager.Get("streak.activate"), true);
                    return;
                }

                UiServiceLocator.UiData.StartStreakLevel(0, startResult =>
                {
                    _state = startResult.IsSuccess ? startResult.Value : result.Value;
                    Apply(_state);
                });
            });
        }

        void OnStartLevel(int levelIndex)
        {
            SetBusy();
            UiServiceLocator.UiData.StartStreakLevel(levelIndex, result =>
            {
                if (!result.IsSuccess)
                {
                    Apply(_state ?? new StreakChallengeStateResponse());
                    return;
                }

                _state = result.Value;
                Apply(_state);
            });
        }

        void OnClaimReward(int levelIndex)
        {
            if (levelIndex < 0) return;
            SetBusy();
            UiServiceLocator.UiData.ClaimStreakReward(levelIndex, Guid.NewGuid().ToString(), result =>
            {
                if (!result.IsSuccess)
                {
                    Apply(_state ?? new StreakChallengeStateResponse());
                    return;
                }

                _state = result.Value.EventState;
                Apply(_state);
            });
        }

        void SetBusy()
        {
            if (activateButton != null) activateButton.interactable = false;
        }

        void ToggleInfoPopup()
        {
            if (infoPopup == null) return;
            infoPopup.SetActive(!infoPopup.activeSelf);
        }

        void ResolveRefs()
        {
            closeButton ??= FindButton("Btn_Close") ?? FindButton("CloseButton");
            infoButton ??= FindButton("Btn_Info");
            activateButton ??= FindButton("Btn_Claim") ?? FindButton("ActivateButton") ?? FindButton("Btn_Activate");
            timerText ??= FindText("Txt_Timer") ?? FindText("TimerText");
            levelText ??= FindText("Txt_Level") ?? FindText("StatusText");
            prizeTitleText ??= FindText("Txt_PrizeTitle");
            prizeAmountText ??= FindText("Txt_PrizeAmount");
            prizeIcon ??= FindImage("Img_PrizeIcon");
            bannerImage ??= FindImage("Img_Banner");
            levelListRoot ??= FindRect("LevelPath") ?? FindRect("LevelList");
            infoPopup ??= FindGameObject("InfoPopup");

            if (activateButton != null)
                _actionLabel = activateButton.GetComponentInChildren<TextMeshProUGUI>(true);

            if (levelListRoot == null || prizeAmountText == null || timerText == null)
                BuildRuntimeLayout();
        }

        void BuildRuntimeLayout()
        {
            var panel = FindRect("Panel") ?? transform as RectTransform;
            var content = FindRect("Content") ?? panel;
            var footer = FindRect("Footer");
            if (content == null) return;

            ClearChildren(content);

            var header = FindRect("Header");
            if (header != null && infoButton == null)
                infoButton = BuildIconButton(header, "Btn_Info", new Vector2(20f, 0f), false, "i");

            bannerImage = AddImage(content, "Img_Banner", new Vector2(760f, 230f), new Color(0.13f, 0.53f, 1f, 0.4f));
            timerText = AddText(content, "Txt_Timer", 32f, TextAlignmentOptions.Center, FontStyles.Bold);
            levelText = AddText(content, "Txt_Level", 40f, TextAlignmentOptions.Center, FontStyles.Bold);

            var prize = AddImage(content, "Panel_Prize", new Vector2(720f, 150f), Purple);
            prizeTitleText = AddText(prize.transform, "Txt_PrizeTitle", 26f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(prizeTitleText.rectTransform, new Vector2(0f, 38f), new Vector2(650f, 42f));
            prizeIcon = AddImage(prize.rectTransform, "Img_PrizeIcon", new Vector2(64f, 64f), Gold);
            prizeIcon.rectTransform.anchoredPosition = new Vector2(-120f, -28f);
            prizeAmountText = AddText(prize.transform, "Txt_PrizeAmount", 42f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(prizeAmountText.rectTransform, new Vector2(70f, -28f), new Vector2(360f, 72f));

            var path = new GameObject("LevelPath", typeof(RectTransform), typeof(LayoutElement));
            path.transform.SetParent(content, false);
            levelListRoot = path.GetComponent<RectTransform>();
            levelListRoot.sizeDelta = new Vector2(0f, PathHeight);
            path.GetComponent<LayoutElement>().preferredHeight = PathHeight;

            if (footer != null)
            {
                ClearChildren(footer);
                activateButton = BuildFooterButton(footer);
                _actionLabel = activateButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (panel != null && infoPopup == null)
                infoPopup = BuildInfoPopup(panel).gameObject;
        }

        Button BuildIconButton(RectTransform header, string name, Vector2 position, bool right, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(header, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = right ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            rt.pivot = right ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(80f, 80f);
            rt.anchoredPosition = position;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.16f, 0.62f, 1f, 0.92f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var text = AddText(go.transform, "Txt_Label", 44f, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(text.rectTransform);
            text.text = label;
            return btn;
        }

        Button BuildFooterButton(RectTransform parent)
        {
            var go = new GameObject("Btn_Claim", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 120f);
            go.GetComponent<Image>().color = new Color(1f, 0.42f, 0.18f, 1f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 120f;
            le.flexibleWidth = 1f;

            var text = AddText(go.transform, "Txt_Label", 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(text.rectTransform);
            return go.GetComponent<Button>();
        }

        RectTransform BuildInfoPopup(RectTransform panel)
        {
            var root = new GameObject("InfoPopup", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(panel, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(760f, 420f);
            rt.anchoredPosition = new Vector2(0f, 120f);
            root.GetComponent<Image>().color = new Color(0.05f, 0.1f, 0.22f, 0.98f);

            var title = AddText(root.transform, "Txt_InfoTitle", 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            SetRect(title.rectTransform, new Vector2(0f, 134f), new Vector2(640f, 64f));
            title.text = LocalizationManager.Get("streak.info_title");

            var body = AddText(root.transform, "Txt_InfoBody", 26f, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            SetRect(body.rectTransform, new Vector2(0f, -24f), new Vector2(620f, 220f));
            body.textWrappingMode = TextWrappingModes.Normal;
            body.text = LocalizationManager.Get("streak.info_body");

            root.SetActive(false);
            return rt;
        }

        Image AddImage(RectTransform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = size.x;
            le.preferredHeight = size.y;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        TextMeshProUGUI AddText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = PanelText;
            tmp.raycastTarget = false;
            tmp.gameObject.AddComponent<LocalizedFont>();
            return tmp;
        }

        static void SetRect(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        Button? FindButton(string n)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == n)
                    return button;
            return null;
        }

        TextMeshProUGUI? FindText(string n)
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == n)
                    return text;
            return null;
        }

        Image? FindImage(string n)
        {
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (image.name == n)
                    return image;
            return null;
        }

        RectTransform? FindRect(string n)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
                if (rect.name == n)
                    return rect;
            return null;
        }

        GameObject? FindGameObject(string n)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
                if (rect.name == n)
                    return rect.gameObject;
            return null;
        }

        static void SetText(TextMeshProUGUI? label, string value)
        {
            if (label != null) label.text = value ?? "";
        }

        static void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        static void Stretch(RectTransform rect, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(l, b);
            rect.offsetMax = new Vector2(-r, -t);
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.CloseTop();
        }
    }
}
