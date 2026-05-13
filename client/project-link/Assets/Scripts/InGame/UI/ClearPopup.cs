using ProjectLink.Contracts.Progress;
using ProjectLink.Core;
using ProjectLink.Data;
using ProjectLink.OutGame.UI;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.InGame.UI
{
    public sealed class StageClearPopupModel
    {
        public StageClearPopupModel(int stageId, int stars, int softReward, long softBalanceAfter, int movesUsed, int moveLimit, long clearElapsedMs, int score, bool isBestRecord, int nextStageId, bool nextStageUnlocked)
        {
            StageId = stageId;
            Stars = stars;
            SoftReward = softReward;
            SoftBalanceAfter = softBalanceAfter;
            MovesUsed = movesUsed;
            MoveLimit = moveLimit;
            ClearElapsedMs = clearElapsedMs;
            Score = score;
            IsBestRecord = isBestRecord;
            NextStageId = nextStageId;
            NextStageUnlocked = nextStageUnlocked;
        }

        public int StageId { get; }
        public int Stars { get; }
        public int SoftReward { get; }
        public long SoftBalanceAfter { get; }
        public int MovesUsed { get; }
        public int MoveLimit { get; }
        public long ClearElapsedMs { get; }
        public int Score { get; }
        public bool IsBestRecord { get; }
        public int NextStageId { get; }
        public bool NextStageUnlocked { get; }
    }

    public class ClearPopup : PopupBase
    {
        [SerializeField] Button nextButton;
        [SerializeField] Button retryButton;
        [SerializeField] Button lobbyButton;
        [SerializeField] TextMeshProUGUI stageText;
        [SerializeField] TextMeshProUGUI rewardText;
        [SerializeField] TextMeshProUGUI movesText;
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] RectTransform starRow;

        bool _initialized;
        StageClearPopupModel _model;
        bool _progressLoaded;
        bool _nextStageUnlocked;
        bool _nextStageCleared;

        public void Init(int stageId, int stars)
        {
            Init(new StageClearPopupModel(stageId, stars, 0, 0, 0, 0, 0, 0, false, stageId + 1, true));
        }

        public void Init(StageClearPopupModel model)
        {
            if (_initialized) return;
            _initialized = true;
            _model = model ?? new StageClearPopupModel(GameContext.SelectedStageId, 0, 0, 0, 0, 0, 0, 0, false, GameContext.SelectedStageId + 1, false);

            ResolveMissingReferences();
            if (nextButton == null || retryButton == null || lobbyButton == null)
                BuildFallback();

            Apply();
            BindButtons();
            RefreshNextStageProgress();
        }

        public override void OnBackPressed() { }

        void Apply()
        {
            SetText(stageText, $"Stage {_model.StageId}");
            SetText(rewardText, $"+{_model.SoftReward} / {FormatTime(_model.ClearElapsedMs)}");
            SetText(movesText, _model.MoveLimit > 0 ? $"{_model.MovesUsed}/{_model.MoveLimit}" : _model.MovesUsed.ToString());
            SetText(scoreText, _model.IsBestRecord ? $"{_model.Score} BEST" : _model.Score.ToString());

            ClearChildren(starRow);
            for (int i = 0; i < Mathf.Clamp(_model.Stars, 0, 3); i++)
                AddStar(i);

            RefreshNextButton();
        }

        void BindButtons()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(LoadNext);
            if (retryButton != null)
                retryButton.onClick.AddListener(Retry);
            if (lobbyButton != null)
                lobbyButton.onClick.AddListener(LoadLobby);
        }

        void LoadNext()
        {
            if (!_progressLoaded)
            {
                if (nextButton != null)
                    nextButton.interactable = false;
                UiServiceLocator.UiData.GetProgress(result =>
                {
                    ApplyProgress(result);
                    if (_nextStageCleared)
                        OpenClearedNextConfirm();
                    else
                        LoadNextConfirmed();
                    RefreshNextButton();
                });
                return;
            }

            if (_nextStageCleared)
            {
                OpenClearedNextConfirm();
                return;
            }

            LoadNextConfirmed();
        }

        void LoadNextConfirmed()
        {
            PopupManager.Instance.CloseAll();
            if (GameContext.IsDailyChallengeStage)
                GameContext.AdvanceDailyChallengeStage(_model.NextStageId);
            RuntimeNavigationButtons.EnterStage(_model.NextStageId);
        }

        void OpenClearedNextConfirm()
        {
            PopupManager.Request(PopupId.ClearNextStageConfirm,
                new ClearNextStageConfirmModel(_model.NextStageId, LoadNextConfirmed, LoadLobby));
        }

        void Retry()
        {
            PopupManager.Instance.CloseAll();
            RuntimeNavigationButtons.EnterStage(_model.StageId);
        }

        static void LoadLobby()
        {
            PopupManager.Instance.CloseAll();
            GameContext.ClearDailyChallengeRun();
            SceneLoader.Instance.LoadScene("Lobby");
        }

        void RefreshNextStageProgress()
        {
            _nextStageUnlocked = _model.NextStageUnlocked;
            _nextStageCleared = false;
            RefreshNextButton();
            UiServiceLocator.UiData.GetProgress(ApplyProgress);
        }

        void ApplyProgress(ServiceResult<ProgressResponse> result)
        {
            if (!result.IsSuccess || result.Value == null)
            {
                _progressLoaded = false;
                _nextStageUnlocked = _model.NextStageUnlocked;
                _nextStageCleared = false;
                RefreshNextButton();
                return;
            }

            _progressLoaded = true;
            _nextStageUnlocked = false;
            _nextStageCleared = false;

            for (int i = 0; i < result.Value.Stages.Count; i++)
            {
                var entry = result.Value.Stages[i];
                if (entry.StageId != _model.NextStageId)
                    continue;

                _nextStageUnlocked = entry.IsUnlocked;
                _nextStageCleared = entry.Stars > 0 || !string.IsNullOrEmpty(entry.ClearedAt);
                break;
            }

            RefreshNextButton();
        }

        void RefreshNextButton()
        {
            if (nextButton != null)
                nextButton.interactable = _model.NextStageId > 0
                    && _model.NextStageId <= StageLoader.MaxStageId
                    && (_nextStageUnlocked || _model.NextStageUnlocked);
        }

        void ResolveMissingReferences()
        {
            nextButton ??= FindButton("NextButton");
            retryButton ??= FindButton("RetryButton");
            lobbyButton ??= FindButton("LobbyButton");
            stageText ??= FindText("StageText");
            rewardText ??= FindText("RewardText");
            movesText ??= FindText("MovesText");
            scoreText ??= FindText("ScoreText");
            starRow ??= FindRect("StarRow");
        }

        void BuildFallback()
        {
            gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 760f);
            panelRect.anchoredPosition = Vector2.zero;
            panelGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 1f);

            AddLocalizedLabel(panelGo.transform, "game_stage_clear", 64, new Vector2(0f, 270f), new Vector2(560f, 90f));
            stageText = AddLabel(panelGo.transform, "StageText", 38, new Vector2(0f, 190f), new Vector2(500f, 64f));
            starRow = AddRow(panelGo.transform, "StarRow", new Vector2(0f, 95f), new Vector2(420f, 90f));
            rewardText = AddLabel(panelGo.transform, "RewardText", 34, new Vector2(0f, 10f), new Vector2(520f, 60f));
            movesText = AddLabel(panelGo.transform, "MovesText", 30, new Vector2(0f, -55f), new Vector2(520f, 52f));
            scoreText = AddLabel(panelGo.transform, "ScoreText", 30, new Vector2(0f, -115f), new Vector2(520f, 52f));
            nextButton = AddLocalizedButton(panelGo.transform, "NextButton", "game_next", new Vector2(0f, -215f));
            retryButton = AddLocalizedButton(panelGo.transform, "RetryButton", "game_retry", new Vector2(0f, -325f));
            lobbyButton = AddLocalizedButton(panelGo.transform, "LobbyButton", "game_lobby", new Vector2(0f, -435f));
        }

        void AddStar(int index)
        {
            if (starRow == null) return;

            var go = new GameObject($"Star_{index}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(starRow, false);
            go.GetComponent<Image>().color = new Color(1f, 0.85f, 0f, 1f);
            var element = go.GetComponent<LayoutElement>();
            element.preferredWidth = 82f;
            element.preferredHeight = 82f;
        }

        static Button AddLocalizedButton(Transform parent, string name, string stringId, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 86f);
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);
            AddLocalizedLabel(go.transform, stringId, 38, Vector2.zero, new Vector2(400f, 80f));
            return go.GetComponent<Button>();
        }

        static TextMeshProUGUI AddLabel(Transform parent, string name, float fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void AddLocalizedLabel(Transform parent, string stringId, float fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var label = AddLabel(parent, "Label", fontSize, anchoredPos, size);
            label.gameObject.AddComponent<LocalizedText>().SetStringId(stringId);
        }

        static RectTransform AddRow(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            return rect;
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

        static string FormatTime(long elapsedMs)
        {
            if (elapsedMs <= 0) return "00:00";
            var seconds = Mathf.FloorToInt(elapsedMs / 1000f);
            return $"{seconds / 60:D2}:{seconds % 60:D2}";
        }
    }
}
