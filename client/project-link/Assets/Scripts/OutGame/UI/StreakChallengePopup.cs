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

namespace ProjectLink.OutGame.UI
{
    public sealed class StreakChallengePopup : PopupBase
    {
        [SerializeField] Button?             closeButton;
        [SerializeField] Button?             activateButton;    // single dynamic action button
        [SerializeField] TextMeshProUGUI?    statusText;        // repurposed as HH:MM timer label
        [SerializeField] TextMeshProUGUI?    timerText;
        [SerializeField] RectTransform?      levelListRoot;

        bool _initialized;
        StreakChallengeStateResponse? _state;
        TextMeshProUGUI? _actionLabel;   // TMP child of activateButton
        DateTimeOffset   _expiresAt;
        Coroutine?       _timerLoop;
        Coroutine?       _countdownLoop;

        // ── chip colors ───────────────────────────────────────────────────────
        static readonly Color ChipHighlight = new Color(0.95f, 0.72f, 0.1f,  1f);   // current level
        static readonly Color ChipNormal    = new Color(0.22f, 0.52f, 0.88f, 0.9f); // upcoming levels
        static readonly Color ChipDimmed    = new Color(0.28f, 0.28f, 0.32f, 0.5f); // past / inactive

        // ── node / line colors ────────────────────────────────────────────────
        static readonly Color NodeCleared  = new Color(0.18f, 0.82f, 0.35f, 1f);
        static readonly Color NodePending  = new Color(0.25f, 0.25f, 0.3f,  0.85f);
        static readonly Color LineCleared  = new Color(0.2f,  0.8f,  0.38f, 0.85f);
        static readonly Color LinePending  = new Color(0.42f, 0.42f, 0.48f, 0.4f);

        // ── path layout ───────────────────────────────────────────────────────
        const float PathHeight  = 300f;
        const float NodeSize    = 42f;
        const float XRange      = 72f;
        const float YMargin     = 28f;

        // ─────────────────────────────────────────────────────────────────────

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;
            ResolveRefs();
            BindOverlayClose();
            if (closeButton != null) closeButton.onClick.AddListener(CloseTop);
            Refresh();
        }

        void OnDisable()
        {
            StopTimer();
            StopCountdown();
        }

        // ── data ──────────────────────────────────────────────────────────────

        void Refresh()
        {
            UiServiceLocator.UiData.GetStreakChallengeState(result =>
            {
                if (!result.IsSuccess) { SetText(statusText, result.ErrorCode); return; }
                _state = result.Value;
                Apply(_state);
            });
        }

        void Apply(StreakChallengeStateResponse state)
        {
            StopTimer();
            StopCountdown();

            if (levelListRoot != null)
            {
                ClearChildren(levelListRoot);
                BuildChipRow(state);
                BuildPathView(state);
            }

            UpdateHudTimer(state);
            UpdateActionButton(state);
        }

        // ── top: level chip row ───────────────────────────────────────────────

        void BuildChipRow(StreakChallengeStateResponse state)
        {
            var row = new GameObject("ChipRow", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(levelListRoot, false);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 58f;
            le.flexibleWidth   = 1f;
            var hg = row.GetComponent<HorizontalLayoutGroup>();
            hg.spacing              = 8f;
            hg.padding              = new RectOffset(6, 6, 0, 0);
            hg.childAlignment       = TextAnchor.MiddleCenter;
            hg.childForceExpandWidth  = true;
            hg.childForceExpandHeight = true;

            for (int i = 0; i < 3; i++)
                MakeChip(row.transform, i, ChipColorForIndex(i, state));
        }

        static Color ChipColorForIndex(int i, StreakChallengeStateResponse state)
        {
            if (state.EventStatus == "INACTIVE" || state.EventStatus == "COMPLETED")
                return ChipDimmed;
            int cur = state.CurrentLevel;
            if (i < cur)  return ChipDimmed;
            if (i == cur) return ChipHighlight;
            return ChipNormal;
        }

        static void MakeChip(Transform parent, int index, Color color)
        {
            var go = new GameObject($"Chip_L{index}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;

            var lbl = new GameObject("Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
            lbl.transform.SetParent(go.transform, false);
            var rt  = lbl.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = lbl.GetComponent<TextMeshProUGUI>();
            tmp.text      = $"LV.{index + 1}";
            tmp.fontSize  = 18f;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ── middle: streak path view ──────────────────────────────────────────

        void BuildPathView(StreakChallengeStateResponse state)
        {
            // Determine which level's progress to show
            int levelIdx;
            int current;
            int required;

            if (state.EventStatus == "INACTIVE")
            {
                levelIdx = 0;
                current  = 0;
                required = state.Levels.Count > 0 ? state.Levels[0].RequiredCount : 3;
            }
            else if (state.EventStatus == "COMPLETED")
            {
                int last  = state.Levels.Count - 1;
                var lv    = last >= 0 ? state.Levels[last] : null;
                levelIdx  = last >= 0 ? last : 2;
                required  = lv?.RequiredCount ?? 7;
                current   = required; // all done
            }
            else
            {
                levelIdx = state.CurrentLevel;
                var lv   = state.Levels.Count > levelIdx ? state.Levels[levelIdx] : null;
                required = lv?.RequiredCount ?? DefaultRequired(levelIdx);
                current  = lv?.CurrentCount  ?? 0;
            }

            // Container with fixed height, children positioned freely
            var container = new GameObject("PathContainer",
                typeof(RectTransform), typeof(LayoutElement));
            container.transform.SetParent(levelListRoot, false);
            var le = container.GetComponent<LayoutElement>();
            le.preferredHeight = PathHeight;
            le.flexibleWidth   = 1f;
            var ct = container.GetComponent<RectTransform>();

            DrawPath(ct, levelIdx, current, required);
        }

        static void DrawPath(RectTransform parent, int levelIdx, int current, int required)
        {
            var rng  = new System.Random(levelIdx);
            float yBot = -(PathHeight * 0.5f) + YMargin;
            float yTop =  (PathHeight * 0.5f) - YMargin;

            var positions = new Vector2[required];
            for (int i = 0; i < required; i++)
            {
                float t = required > 1 ? (float)i / (required - 1) : 0f;
                float y = Mathf.Lerp(yBot, yTop, t);
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * XRange;
                positions[i] = new Vector2(x, y);
            }

            // Lines first (below nodes in hierarchy)
            for (int i = 0; i < required - 1; i++)
                DrawLine(parent, positions[i], positions[i + 1], i + 1 < current);

            // Nodes on top
            for (int i = 0; i < required; i++)
                DrawNode(parent, positions[i], i, i < current);
        }

        static void DrawLine(RectTransform parent, Vector2 from, Vector2 to, bool cleared)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt   = go.GetComponent<RectTransform>();
            var diff = to - from;
            rt.anchoredPosition = (from + to) * 0.5f;
            rt.sizeDelta        = new Vector2(4f, diff.magnitude);
            rt.localRotation    = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(diff.x, diff.y) * Mathf.Rad2Deg);
            go.GetComponent<Image>().color = cleared ? LineCleared : LinePending;
        }

        static void DrawNode(RectTransform parent, Vector2 pos, int index, bool cleared)
        {
            var go = new GameObject($"Node_{index}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = new Vector2(NodeSize, NodeSize);
            go.GetComponent<Image>().color = cleared ? NodeCleared : NodePending;

            var lbl = new GameObject("Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
            lbl.transform.SetParent(go.transform, false);
            var lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            var tmp = lbl.GetComponent<TextMeshProUGUI>();
            tmp.text      = cleared ? "✓" : $"{index + 1}";
            tmp.fontSize  = cleared ? 20f : 15f;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ── HUD timer (HH:MM, per-minute) ─────────────────────────────────────

        void UpdateHudTimer(StreakChallengeStateResponse state)
        {
            if (state.EventStatus == "ACTIVE" && !string.IsNullOrEmpty(state.ExpiresAtIso))
            {
                try
                {
                    _expiresAt = DateTimeOffset.Parse(state.ExpiresAtIso);
                    RenderHudTimer();
                    _timerLoop = StartCoroutine(HudTimerLoop());
                    return;
                }
                catch { /* fallthrough */ }
            }
            SetText(timerText, "");
        }

        void StopTimer()
        {
            if (_timerLoop != null) { StopCoroutine(_timerLoop); _timerLoop = null; }
        }

        IEnumerator HudTimerLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(60f);
                RenderHudTimer();
            }
        }

        void RenderHudTimer()
        {
            var remaining = _expiresAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) { SetText(timerText, "00:00"); StopTimer(); return; }
            int h = (int)remaining.TotalHours;
            int m = remaining.Minutes;
            SetText(timerText, $"{h:D2}:{m:D2}");
        }

        // ── action button ─────────────────────────────────────────────────────

        void UpdateActionButton(StreakChallengeStateResponse state)
        {
            StopCountdown();
            if (activateButton == null) return;

            activateButton.onClick.RemoveAllListeners();

            if (state.AvailableActions.Contains("ACTIVATE"))
            {
                Show(true);
                SetLabel(LocalizationManager.Get("streak.activate"));
                activateButton.onClick.AddListener(OnActivateAndStart);
            }
            else if (state.AvailableActions.Contains("START_LEVEL"))
            {
                int lvl = GetLevelIndexByStatus(state, "READY");
                Show(true);
                SetLabel(string.Format(LocalizationManager.Get("streak.start_level"), lvl + 1));
                activateButton.onClick.AddListener(() => OnStartLevel(lvl));
            }
            else if (state.AvailableActions.Contains("CLAIM_REWARD"))
            {
                int lvl = GetLevelIndexByReward(state, "PENDING");
                Show(true);
                SetLabel(LocalizationManager.Get("streak.claim"));
                activateButton.onClick.AddListener(() => OnClaimReward(lvl));
            }
            else if (state.AvailableActions.Contains("CONTINUE_LEVEL"))
            {
                Show(false);
                SetLabel(LocalizationManager.Get("streak.claim"));
            }
            else if (state.AvailableActions.Contains("VIEW_COMPLETED"))
            {
                Show(false);
                StartCountdown(state);
            }
            else
            {
                activateButton.gameObject.SetActive(false);
            }
        }

        void Show(bool interactive)
        {
            activateButton!.gameObject.SetActive(true);
            activateButton.interactable = interactive;
        }

        void SetLabel(string text)
        {
            if (_actionLabel != null) _actionLabel.text = text;
        }

        static int GetLevelIndexByStatus(StreakChallengeStateResponse state, string status)
        {
            foreach (var l in state.Levels) if (l.LevelStatus == status) return l.LevelIndex;
            return state.CurrentLevel;
        }

        static int GetLevelIndexByReward(StreakChallengeStateResponse state, string rewardState)
        {
            foreach (var l in state.Levels) if (l.RewardState == rewardState) return l.LevelIndex;
            return state.CurrentLevel;
        }

        // ── per-second countdown (for COMPLETED button) ───────────────────────

        void StartCountdown(StreakChallengeStateResponse state)
        {
            if (string.IsNullOrEmpty(state.ExpiresAtIso)) { SetLabel("--:--:--"); return; }
            try
            {
                _expiresAt = DateTimeOffset.Parse(state.ExpiresAtIso);
                RenderCountdown();
                _countdownLoop = StartCoroutine(CountdownLoop());
            }
            catch { SetLabel("--:--:--"); }
        }

        void StopCountdown()
        {
            if (_countdownLoop != null) { StopCoroutine(_countdownLoop); _countdownLoop = null; }
        }

        IEnumerator CountdownLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f);
                RenderCountdown();
            }
        }

        void RenderCountdown()
        {
            var remaining = _expiresAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) { SetLabel("00:00:00"); StopCountdown(); return; }
            int h = (int)remaining.TotalHours;
            int m = remaining.Minutes;
            int s = remaining.Seconds;
            SetLabel($"{h:D2}:{m:D2}:{s:D2}");
        }

        // ── action handlers ───────────────────────────────────────────────────

        void OnActivateAndStart()
        {
            if (activateButton != null) activateButton.interactable = false;
            UiServiceLocator.UiData.ActivateStreakChallenge(result =>
            {
                if (!result.IsSuccess)
                {
                    if (activateButton != null) activateButton.interactable = true;
                    return;
                }
                // Auto-start Level 0 immediately after activation
                UiServiceLocator.UiData.StartStreakLevel(0, r2 =>
                {
                    _state = r2.IsSuccess ? r2.Value : result.Value;
                    Apply(_state);
                });
            });
        }

        void OnStartLevel(int levelIndex)
        {
            if (activateButton != null) activateButton.interactable = false;
            UiServiceLocator.UiData.StartStreakLevel(levelIndex, result =>
            {
                if (!result.IsSuccess) { if (activateButton != null) activateButton.interactable = true; return; }
                _state = result.Value;
                Apply(_state);
            });
        }

        void OnClaimReward(int levelIndex)
        {
            if (activateButton != null) activateButton.interactable = false;
            var correlationId = Guid.NewGuid().ToString();
            UiServiceLocator.UiData.ClaimStreakReward(levelIndex, correlationId, result =>
            {
                if (!result.IsSuccess) { if (activateButton != null) activateButton.interactable = true; return; }
                _state = result.Value.EventState;
                Apply(_state);
            });
        }

        // ── helpers ───────────────────────────────────────────────────────────

        static int DefaultRequired(int i)
        {
            switch (i) { case 0: return 3; case 1: return 5; default: return 7; }
        }

        void ResolveRefs()
        {
            closeButton    ??= FindButton("CloseButton");
            activateButton ??= FindButton("ActivateButton");
            statusText     ??= FindText("StatusText");
            timerText      ??= FindText("TimerText");
            levelListRoot  ??= FindRect("LevelList");

            if (activateButton != null)
                _actionLabel = activateButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        Button? FindButton(string n)
        {
            foreach (var b in GetComponentsInChildren<Button>(true)) if (b.name == n) return b;
            return null;
        }

        TextMeshProUGUI? FindText(string n)
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true)) if (t.name == n) return t;
            return null;
        }

        RectTransform? FindRect(string n)
        {
            foreach (var r in GetComponentsInChildren<RectTransform>(true)) if (r.name == n) return r;
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

        static void CloseTop()
        {
            if (PopupManager.Instance != null) PopupManager.Instance.CloseTop();
        }
    }
}
