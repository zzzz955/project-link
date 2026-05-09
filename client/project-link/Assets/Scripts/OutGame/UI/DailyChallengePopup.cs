using ProjectLink.Core;
using ProjectLink.Contracts.Daily;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class DailyChallengePopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button playButton;
        [SerializeField] TextMeshProUGUI countdownText;
        [SerializeField] TextMeshProUGUI progressText;
        [SerializeField] TextMeshProUGUI rewardText;
        [SerializeField] RectTransform streakRow;

        bool _initialized;
        int _stageIdToPlay = 1;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindClose(closeButton);
            if (playButton != null)
                playButton.onClick.AddListener(PlayChallenge);

            UiServiceLocator.UiData.GetDailyChallenge(Apply);
        }

        void Apply(ServiceResult<DailyChallengeResponse> result)
        {
            if (!result.IsSuccess)
            {
                SetText(progressText, result.ErrorCode);
                if (playButton != null) playButton.interactable = false;
                return;
            }

            var value = result.Value;
            if (value.TodayStageIds.Count > 0)
                _stageIdToPlay = Mathf.Max(1, value.TodayStageIds[0]);
            SetText(countdownText, value.ResetAt);
            SetText(progressText, $"{value.PlayCountToday}/{value.PlayCountTarget}  Stage {_stageIdToPlay}");
            if (playButton != null) playButton.interactable = !value.CompletedToday && value.TodayStageIds.Count > 0;

            ClearChildren(streakRow);
            for (int i = 0; i < value.Tiles.Count; i++)
            {
                var tile = value.Tiles[i];
                AddTile(tile.Day.ToString(), tile.IsDone, tile.IsToday, tile.IsLocked);
            }

            if (value.TodayRewards.Count > 0)
            {
                var reward = value.TodayRewards[0];
                SetText(rewardText, $"{reward.RewardType} x{reward.Amount}");
            }
        }

        void PlayChallenge()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
            GameContext.SelectedStageId = _stageIdToPlay;
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene("Game");
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("CloseButton");
            playButton ??= FindButton("PlayButton");
            countdownText ??= FindText("CountdownTimerText");
            progressText ??= FindText("DailyProgressText");
            rewardText ??= FindText("RewardPreviewText");
            streakRow ??= FindRect("WeekStreakRow");
        }

        void AddTile(string text, bool done, bool today, bool locked)
        {
            if (streakRow == null) return;

            var go = new GameObject($"DayTile_{text}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(streakRow, false);
            var image = go.GetComponent<Image>();
            image.color = locked ? new Color(1f, 1f, 1f, 0.15f) : today ? new Color(1f, 0.78f, 0.1f, 0.9f) : done ? new Color(0.2f, 0.85f, 0.35f, 0.9f) : new Color(0.1f, 0.25f, 0.45f, 0.9f);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 86f;
            element.preferredHeight = 86f;

            var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.text = done ? "Done" : text;
            tmp.fontSize = 34f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        Button FindButton(string name)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == name) return button;
            return null;
        }

        TextMeshProUGUI FindText(string name)
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == name) return text;
            return null;
        }

        RectTransform FindRect(string name)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
                if (rect.name == name) return rect;
            return null;
        }

        static void BindClose(Button button)
        {
            if (button != null)
                button.onClick.AddListener(CloseTop);
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
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
    }
}
