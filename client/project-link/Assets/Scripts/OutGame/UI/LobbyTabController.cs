using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            shopTabButton ??= FindButton("ShopTabButton") ?? FindButton("Tab_Shop");
            homeTabButton ??= FindButton("HomeTabButton") ?? FindButton("Tab_Home");
            rankingTabButton ??= FindButton("RankingTabButton") ?? FindButton("Tab_Ranking");
            shopPanel ??= FindPanel("Tab_Shop") ?? FindChild("ShopPanel");
            homePanel ??= FindPanel("Tab_Home") ?? FindChild("HomePanel");
            rankingPanel ??= FindPanel("Tab_Ranking") ?? FindChild("RankingPanel");
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
            SetTabVisual(shopTabButton, tab == LobbyTab.Shop);
            SetTabVisual(homeTabButton, tab == LobbyTab.Home);
            SetTabVisual(rankingTabButton, tab == LobbyTab.Ranking);
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

        GameObject FindPanel(string childName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name != "Group_TabBodies")
                    continue;

                var child = rect.Find(childName);
                return child != null ? child.gameObject : null;
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

        static void SetTabVisual(Button button, bool selected)
        {
            if (button == null) return;

            var buttonImage = button.targetGraphic as Image;
            if (buttonImage != null)
                buttonImage.color = selected ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0f);

            var icon = FindChildComponent<Image>(button.transform, "Icon");
            if (icon != null)
                icon.color = selected ? Color.white : new Color(0.63f, 0.68f, 0.76f, 1f);

            var label = FindChildComponent<TextMeshProUGUI>(button.transform, "Txt");
            if (label != null)
                label.color = selected ? Color.white : new Color(0.63f, 0.68f, 0.76f, 1f);

            var indicator = button.transform.Find("Indicator");
            if (indicator != null)
                indicator.gameObject.SetActive(selected);
        }

        static T FindChildComponent<T>(Transform parent, string childName) where T : Component
        {
            var child = parent.Find(childName);
            return child != null ? child.GetComponent<T>() : null;
        }
    }
}
