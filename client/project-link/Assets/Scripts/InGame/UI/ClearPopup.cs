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
        public StageClearPopupModel(int stageId, int stars, int softReward, long softBalanceAfter, int movesUsed, int moveLimit, long clearElapsedMs, int score, bool isBestRecord, int nextStageId, bool nextStageUnlocked, int? rankPercentile)
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
            RankPercentile = rankPercentile;
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
        public int? RankPercentile { get; }
        public string StreakDirective { get; set; } = "NONE";
        public bool IsSuccess => Stars > 0;
    }

    public class ClearPopup : PopupBase
    {
        [SerializeField] Button retryButton;
        [SerializeField] Button lobbyButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button doubleButton;
        [SerializeField] Button addMovesButton;
        [SerializeField] Button quitButton;
        [SerializeField] RectTransform successRoot;
        [SerializeField] RectTransform failureRoot;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI resultText;
        [SerializeField] TextMeshProUGUI stageText;
        [SerializeField] TextMeshProUGUI rewardText;
        [SerializeField] TextMeshProUGUI movesText;
        [SerializeField] TextMeshProUGUI bestText;
        [SerializeField] TextMeshProUGUI scoreText;
        [SerializeField] TextMeshProUGUI rankPercentileText;
        [SerializeField] RectTransform starRow;
        [SerializeField] Sprite starOnSprite;
        [SerializeField] Sprite starOffSprite;

        bool _initialized;
        StageClearPopupModel _model;

        public void Init(int stageId, int stars)
        {
            Init(new StageClearPopupModel(stageId, stars, 0, 0, 0, 0, 0, 0, false, stageId + 1, true, null));
        }

        public void Init(StageClearPopupModel model)
        {
            if (_initialized) return;
            _initialized = true;
            _model = model ?? new StageClearPopupModel(GameContext.SelectedStageId, 0, 0, 0, 0, 0, 0, 0, false, GameContext.SelectedStageId + 1, false, null);

            ResolveMissingReferences();
            if (retryButton == null || lobbyButton == null)
                BuildFallback();

            Apply();
            BindButtons();
        }

        public override void OnBackPressed() { }

        void Apply()
        {
            var levelFmt = LocalizationManager.Get("popup.clear.level_fmt");
            SetText(titleText, string.Format(levelFmt, _model.StageId));
            SetText(stageText, string.Format(levelFmt, _model.StageId));
            SetText(resultText, _model.IsSuccess
                ? LocalizationManager.Get(_model.Stars >= 3 ? "popup.clear.perfect" : "popup.clear.title")
                : LocalizationManager.Get("popup.clear.try_again"));

            var scoreLbl = LocalizationManager.Get("popup.clear.label_score");
            var scoreSuffix = _model.IsBestRecord ? " BEST" : "";
            SetText(scoreText, $"{scoreLbl}: {_model.Score}{scoreSuffix}");

            SetText(movesText, _model.MovesUsed.ToString());
            SetText(bestText, _model.MoveLimit > 0 ? _model.MoveLimit.ToString() : "-");
            SetAllText("MovesText", _model.MovesUsed.ToString());
            SetAllText("BestText", _model.MoveLimit > 0 ? _model.MoveLimit.ToString() : "-");

            SetText(rewardText, $"+{_model.SoftReward}");

            ApplyRankPercentile();
            ApplyStars(_model.Stars);
            RefreshButtonVisibility();
        }

        void ApplyRankPercentile()
        {
            if (rankPercentileText == null) return;
            if (_model.RankPercentile.HasValue)
            {
                var fmt = LocalizationManager.Get("popup.clear.rank_top_fmt");
                rankPercentileText.text = string.Format(fmt, _model.RankPercentile.Value);
                rankPercentileText.gameObject.SetActive(true);
            }
            else
            {
                rankPercentileText.gameObject.SetActive(false);
            }
        }

        void ApplyStars(int earned)
        {
            if (starRow == null) return;
            if (starRow.Find("Img_Star_0") != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var slot = starRow.Find($"Img_Star_{i}");
                    if (slot == null) continue;
                    var img = slot.GetComponent<Image>();
                    if (img == null) continue;
                    bool filled = i < Mathf.Clamp(earned, 0, 3);
                    var sprite = filled ? starOnSprite : starOffSprite;
                    if (sprite != null) { img.sprite = sprite; img.color = Color.white; img.preserveAspect = true; }
                    else img.color = filled ? new Color(1f, 0.82f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.2f);
                }
                return;
            }
            ClearChildren(starRow);
            for (int i = 0; i < 3; i++)
                AddStar(starRow, i < Mathf.Clamp(earned, 0, 3));
        }

        void RefreshButtonVisibility()
        {
            if (successRoot != null)
                successRoot.gameObject.SetActive(_model.IsSuccess);
            if (failureRoot != null)
                failureRoot.gameObject.SetActive(!_model.IsSuccess);
            if (retryButton != null)
                retryButton.gameObject.SetActive(!_model.IsSuccess);
            if (lobbyButton != null)
                lobbyButton.gameObject.SetActive(true);
            if (nextButton != null)
                nextButton.gameObject.SetActive(_model.IsSuccess);
            if (doubleButton != null)
                doubleButton.gameObject.SetActive(_model.IsSuccess);
            if (addMovesButton != null)
                addMovesButton.gameObject.SetActive(!_model.IsSuccess);
            if (quitButton != null)
                quitButton.gameObject.SetActive(!_model.IsSuccess);
        }

        void BindButtons()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(Retry);
            if (lobbyButton != null)
                lobbyButton.onClick.AddListener(LoadLobby);
            if (nextButton != null)
                nextButton.onClick.AddListener(Next);
            if (quitButton != null)
                quitButton.onClick.AddListener(LoadLobby);
            if (doubleButton != null)
                doubleButton.interactable = false;
            if (addMovesButton != null)
                addMovesButton.interactable = false;
        }

        void Retry()
        {
            PopupManager.Instance.CloseAll();
            RuntimeNavigationButtons.EnterStage(_model.StageId);
        }

        static void LoadLobby()
        {
            PopupManager.Instance.CloseAll();
            SceneLoader.Instance.LoadScene("Lobby");
        }

        void Next()
        {
            if (_model.StreakDirective == "OPEN_REWARD_POPUP")
            {
                PopupManager.Request(PopupId.StreakRewardConfirm,
                    new ProjectLink.OutGame.UI.StreakRewardConfirmModel(_model.NextStageId, _model.NextStageUnlocked));
                return;
            }
            PopupManager.Instance.CloseAll();
            if (_model.NextStageUnlocked)
                RuntimeNavigationButtons.EnterStage(_model.NextStageId);
            else
                SceneLoader.Instance.LoadScene("Lobby");
        }

        void ResolveMissingReferences()
        {
            retryButton ??= FindButton("RetryButton") ?? FindButton("Btn_Retry");
            lobbyButton ??= FindButton("LobbyButton") ?? FindButton("Btn_Lobby");
            nextButton ??= FindButton("Btn_Next");
            doubleButton ??= FindButton("Btn_Double");
            addMovesButton ??= FindButton("Btn_AddMoves");
            quitButton ??= FindButton("Btn_Quit");
            successRoot ??= FindRect("SuccessContent");
            failureRoot ??= FindRect("FailureContent");
            titleText ??= FindText("Txt_Title");
            resultText ??= FindText("ResultText");
            stageText ??= FindText("StageText");
            rewardText ??= FindText("RewardText");
            movesText ??= FindText("MovesText");
            bestText ??= FindText("BestText");
            scoreText ??= FindText("ScoreText");
            rankPercentileText ??= FindText("RankPercentileText");
            starRow ??= FindRect("StarRow") ?? FindRect("Group_Stars");
        }

        void BuildFallback()
        {
            gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 780f);
            panelRect.anchoredPosition = Vector2.zero;
            panelGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 1f);

            AddLocalizedLabel(panelGo.transform, _model.IsSuccess ? "popup.clear.title" : "popup.clear.failed", 48, new Vector2(0f, 290f), new Vector2(560f, 72f));
            stageText = AddLabel(panelGo.transform, "StageText", 34, new Vector2(0f, 230f), new Vector2(520f, 56f));
            rankPercentileText = AddLabel(panelGo.transform, "RankPercentileText", 28, new Vector2(0f, 175f), new Vector2(420f, 44f));
            rankPercentileText.color = new Color(1f, 0.85f, 0.2f, 1f);
            starRow = AddRow(panelGo.transform, "StarRow", new Vector2(0f, 110f), new Vector2(420f, 90f));
            scoreText = AddLabel(panelGo.transform, "ScoreText", 28, new Vector2(0f, 40f), new Vector2(520f, 48f));
            movesText = AddLabel(panelGo.transform, "MovesText", 26, new Vector2(0f, -15f), new Vector2(520f, 44f));
            rewardText = AddLabel(panelGo.transform, "RewardText", 26, new Vector2(0f, -65f), new Vector2(520f, 44f));
            retryButton = AddLocalizedButton(panelGo.transform, "RetryButton", "popup.clear.retry", new Vector2(0f, -185f));
            lobbyButton = AddLocalizedButton(panelGo.transform, "LobbyButton", "common.lobby", new Vector2(0f, -295f));
        }

        void AddStar(RectTransform parent, bool filled)
        {
            if (parent == null) return;
            var go = new GameObject("Star", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            var sprite = filled ? starOnSprite : starOffSprite;
            if (sprite != null) { img.sprite = sprite; img.color = Color.white; img.preserveAspect = true; }
            else img.color = filled ? new Color(1f, 0.82f, 0.1f, 1f) : new Color(1f, 1f, 1f, 0.2f);
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
            AddLocalizedLabel(go.transform, stringId, 34, Vector2.zero, new Vector2(400f, 80f));
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

        void SetAllText(string objectName, string value)
        {
            foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.name == objectName)
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
