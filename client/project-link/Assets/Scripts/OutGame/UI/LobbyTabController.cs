using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
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
