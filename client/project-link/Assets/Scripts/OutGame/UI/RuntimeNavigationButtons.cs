using ProjectLink.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class RuntimeNavigationButtons : MonoBehaviour
    {
        const string TitleSceneName = "Title";
        const string LobbySceneName = "Lobby";
        const string GameSceneName = "Game";

        [SerializeField]
        [Min(1)]
        int defaultStageId = 1;

        public void LoadTitle()
        {
            LoadScene(TitleSceneName);
        }

        public void OpenReturnTitlePopup()
        {
            if (PopupManager.Instance == null)
            {
                LoadTitle();
                return;
            }

            PopupManager.Request(PopupId.ReturnTitle);
        }

        public void OpenExitGamePopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.ExitGame, this);
        }

        public void OpenSettingsPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.Settings);
        }

        public void OpenBuyItemPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.BuyItem);
        }

        public void OpenEnergyPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.Energy);
        }

        public void LoadLobby()
        {
            LoadScene(LobbySceneName);
        }

        public void LoadGame()
        {
            LoadGameWithStage(defaultStageId);
        }

        public void LoadGameWithStage(int stageId)
        {
            GameContext.SelectedStageId = Mathf.Max(1, stageId);
            LoadScene(GameSceneName);
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void LoadScene(string sceneName)
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }

    public sealed class LobbyTabController : MonoBehaviour
    {
        [SerializeField] Button shopTabButton;
        [SerializeField] Button homeTabButton;
        [SerializeField] Button rankingTabButton;
        [SerializeField] GameObject shopPanel;
        [SerializeField] GameObject homePanel;
        [SerializeField] GameObject rankingPanel;
        [SerializeField] LobbyTab defaultTab = LobbyTab.Home;

        enum LobbyTab
        {
            Shop,
            Home,
            Ranking
        }

        public void Configure(Button shopButton, Button homeButton, Button rankingButton, GameObject shop, GameObject home, GameObject ranking)
        {
            shopTabButton = shopButton;
            homeTabButton = homeButton;
            rankingTabButton = rankingButton;
            shopPanel = shop;
            homePanel = home;
            rankingPanel = ranking;
        }

        void Awake()
        {
            ResolveMissingReferences();
            Bind();
            Show(defaultTab);
        }

        void ResolveMissingReferences()
        {
            shopTabButton ??= FindButton("ShopTabButton");
            homeTabButton ??= FindButton("HomeTabButton");
            rankingTabButton ??= FindButton("RankingTabButton");
            shopPanel ??= FindChild("ShopPanel");
            homePanel ??= FindChild("HomePanel");
            rankingPanel ??= FindChild("RankingPanel");
        }

        void Bind()
        {
            if (shopTabButton != null)
                shopTabButton.onClick.AddListener(() => Show(LobbyTab.Shop));
            if (homeTabButton != null)
                homeTabButton.onClick.AddListener(() => Show(LobbyTab.Home));
            if (rankingTabButton != null)
                rankingTabButton.onClick.AddListener(() => Show(LobbyTab.Ranking));
        }

        void Show(LobbyTab tab)
        {
            SetActive(shopPanel, tab == LobbyTab.Shop);
            SetActive(homePanel, tab == LobbyTab.Home);
            SetActive(rankingPanel, tab == LobbyTab.Ranking);
            SetInteractable(shopTabButton, tab != LobbyTab.Shop);
            SetInteractable(homeTabButton, tab != LobbyTab.Home);
            SetInteractable(rankingTabButton, tab != LobbyTab.Ranking);
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

        GameObject FindChild(string childName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == childName)
                    return rect.gameObject;
            }

            return null;
        }

        static void SetActive(GameObject target, bool active)
        {
            if (target != null)
                target.SetActive(active);
        }

        static void SetInteractable(Button button, bool interactable)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }
}
