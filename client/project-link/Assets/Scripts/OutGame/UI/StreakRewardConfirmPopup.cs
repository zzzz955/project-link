using ProjectLink.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public class StreakRewardConfirmModel
    {
        public StreakRewardConfirmModel(int nextStageId, bool nextStageUnlocked)
        {
            NextStageId       = nextStageId;
            NextStageUnlocked = nextStageUnlocked;
        }

        public int  NextStageId       { get; }
        public bool NextStageUnlocked { get; }
    }

    public class StreakRewardConfirmPopup : PopupBase
    {
        [SerializeField] Button btnLobby;
        [SerializeField] Button btnContinue;

        StreakRewardConfirmModel _model;

        public void Init(StreakRewardConfirmModel model)
        {
            _model = model ?? new StreakRewardConfirmModel(GameContext.SelectedStageId + 1, false);

            btnLobby    ??= FindButtonInChildren("Btn_Lobby");
            btnContinue ??= FindButtonInChildren("Btn_Continue");

            BindOverlayClose();

            if (btnLobby != null)
                btnLobby.onClick.AddListener(() =>
                {
                    GameContext.ShouldOpenStreakPopupOnLobby = true;
                    PopupManager.Instance.CloseAll();
                    SceneLoader.Instance.LoadScene("Lobby");
                });

            if (btnContinue != null)
                btnContinue.onClick.AddListener(() =>
                {
                    PopupManager.Instance.CloseAll();
                    if (_model.NextStageUnlocked)
                        RuntimeNavigationButtons.EnterStage(_model.NextStageId);
                    else
                        SceneLoader.Instance.LoadScene("Lobby");
                });

            if (btnLobby == null && btnContinue == null)
                BuildFallback();
        }

        public override void OnBackPressed() { }

        void BuildFallback()
        {
            gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(580f, 480f);
            panelRect.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 1f);

            AddLocalizedLabel(panel.transform, "popup.streak_reward.title", 40, new Vector2(0f, 170f), new Vector2(520f, 64f));
            AddLocalizedLabel(panel.transform, "popup.streak_reward.body",  28, new Vector2(0f,  90f), new Vector2(520f, 80f));
            btnLobby    = AddLocalizedButton(panel.transform, "Btn_Lobby",    "popup.streak_reward.btn_lobby",    new Vector2(0f, -80f));
            btnContinue = AddLocalizedButton(panel.transform, "Btn_Continue", "popup.streak_reward.btn_continue", new Vector2(0f, -180f));

            if (btnLobby != null)
                btnLobby.onClick.AddListener(() =>
                {
                    GameContext.ShouldOpenStreakPopupOnLobby = true;
                    PopupManager.Instance.CloseAll();
                    SceneLoader.Instance.LoadScene("Lobby");
                });

            if (btnContinue != null)
                btnContinue.onClick.AddListener(() =>
                {
                    PopupManager.Instance.CloseAll();
                    if (_model.NextStageUnlocked)
                        RuntimeNavigationButtons.EnterStage(_model.NextStageId);
                    else
                        SceneLoader.Instance.LoadScene("Lobby");
                });
        }

        Button FindButtonInChildren(string childName)
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
                if (btn.name == childName) return btn;
            return null;
        }

        static Button AddLocalizedButton(Transform parent, string name, string stringId, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(440f, 80f);
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);
            var label = AddLabel(go.transform, 30, Vector2.zero, new Vector2(420f, 72f));
            label.gameObject.AddComponent<LocalizedText>().SetStringId(stringId);
            return go.GetComponent<Button>();
        }

        static void AddLocalizedLabel(Transform parent, string stringId, float fontSize, Vector2 pos, Vector2 size)
        {
            var label = AddLabel(parent, fontSize, pos, size);
            label.gameObject.AddComponent<LocalizedText>().SetStringId(stringId);
        }

        static TMPro.TextMeshProUGUI AddLabel(Transform parent, float fontSize, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            var tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
