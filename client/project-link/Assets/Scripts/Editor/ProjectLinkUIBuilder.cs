using System.Collections.Generic;
using ProjectLink.Core;
using ProjectLink.InGame.UI;
using ProjectLink.OutGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectLink.EditorTools
{
    public static class ProjectLinkUIBuilder
    {
        const float Width = 1080f;
        const float Height = 1920f;
        const string ResourceRoot = "Assets/Resources/UI";
        const string PopupPrefabRoot = "Assets/Resources/Prefabs/UI";
        static readonly Color BackgroundSlot = new(0.04f, 0.08f, 0.14f, 0.18f);
        static readonly Color ContentSlot = new(0.12f, 0.35f, 0.7f, 0.24f);
        static readonly Color ButtonSlot = new(0.1f, 0.55f, 0.95f, 0.42f);
        static readonly Color AccentButtonSlot = new(0.12f, 0.85f, 0.35f, 0.48f);
        static readonly Color ScrollSlot = new(0.1f, 0.16f, 0.25f, 0.32f);

        readonly struct RectSpec
        {
            public RectSpec(string name, Vector2 position, Vector2 size)
            {
                Name = name;
                Position = position;
                Size = size;
            }

            public string Name { get; }
            public Vector2 Position { get; }
            public Vector2 Size { get; }
        }

        [MenuItem("Tools/Project Link/UI Build/Build Current Scene UI")]
        public static void BuildCurrentSceneUI()
        {
            BuildScene(SceneManager.GetActiveScene().name);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Project Link/UI Build/Build All Scene UI")]
        public static void BuildAllSceneUI()
        {
            BuildPopupPrefabs();

            if (!Application.isBatchMode)
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            string[] scenePaths =
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Title.unity",
                "Assets/Scenes/Lobby.unity",
                "Assets/Scenes/Game.unity"
            };

            foreach (string path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                BuildScene(scene.name);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        public static void BuildAllSceneUIBatch()
        {
            BuildAllSceneUI();
        }

        [MenuItem("Tools/Project Link/UI Build/Build Popup Prefabs")]
        public static void BuildPopupPrefabs()
        {
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder(PopupPrefabRoot);

            CreateWireframePopupPrefab<ReturnTitlePopup>("ReturnTitlePopup", ConfirmPopupSlots(), ConfirmPopupButtons());
            CreateWireframePopupPrefab<ExitGamePopup>("ExitGamePopup", ConfirmPopupSlots(), ConfirmPopupButtons());

            CreateWireframePopupPrefab<SettingPopup>("SettingPopup", new[]
            {
                new RectSpec("PanelImageSlot", Vector2.zero, new Vector2(850f, 1500f)),
                new RectSpec("TitleImageSlot", new Vector2(0f, 580f), new Vector2(620f, 130f)),
                new RectSpec("BgmRowSlot", new Vector2(0f, 350f), new Vector2(760f, 130f)),
                new RectSpec("SfxRowSlot", new Vector2(0f, 195f), new Vector2(760f, 130f)),
                new RectSpec("HapticsRowSlot", new Vector2(0f, 40f), new Vector2(760f, 130f)),
                new RectSpec("NotificationRowSlot", new Vector2(0f, -115f), new Vector2(760f, 130f)),
                new RectSpec("LanguageRowSlot", new Vector2(0f, -270f), new Vector2(760f, 130f)),
                new RectSpec("AccountRowSlot", new Vector2(0f, -455f), new Vector2(760f, 155f))
            }, new[]
            {
                new RectSpec("CloseIconButton", new Vector2(382f, 672f), new Vector2(112f, 112f)),
                new RectSpec("BgmToggleButton", new Vector2(255f, 350f), new Vector2(170f, 90f)),
                new RectSpec("SfxToggleButton", new Vector2(255f, 195f), new Vector2(170f, 90f)),
                new RectSpec("HapticsToggleButton", new Vector2(255f, 40f), new Vector2(170f, 90f)),
                new RectSpec("NotificationsToggleButton", new Vector2(255f, -115f), new Vector2(170f, 90f)),
                new RectSpec("LanguageDropdownButton", new Vector2(160f, -270f), new Vector2(270f, 95f)),
                new RectSpec("AccountLinkButton", new Vector2(230f, -455f), new Vector2(250f, 110f)),
                new RectSpec("CloseButton", new Vector2(-180f, -595f), new Vector2(310f, 120f)),
                new RectSpec("SaveButton", new Vector2(180f, -595f), new Vector2(310f, 120f))
            });

            CreateWireframePopupPrefab<BuyItemPopup>("BuyItemPopup", new[]
            {
                new RectSpec("PanelImageSlot", Vector2.zero, new Vector2(900f, 1500f)),
                new RectSpec("TitleImageSlot", new Vector2(0f, 610f), new Vector2(520f, 120f)),
                new RectSpec("FeaturedItemCardSlot", new Vector2(0f, 285f), new Vector2(660f, 430f)),
                new RectSpec("UndoItemSlot", new Vector2(-265f, -125f), new Vector2(245f, 340f)),
                new RectSpec("PaintItemSlot", new Vector2(0f, -125f), new Vector2(245f, 340f)),
                new RectSpec("ShuffleItemSlot", new Vector2(265f, -125f), new Vector2(245f, 340f)),
                new RectSpec("PricePanelSlot", new Vector2(0f, -535f), new Vector2(760f, 310f))
            }, new[]
            {
                new RectSpec("CloseIconButton", new Vector2(338f, 675f), new Vector2(112f, 112f)),
                new RectSpec("UndoItemButton", new Vector2(-265f, -125f), new Vector2(245f, 340f)),
                new RectSpec("PaintItemButton", new Vector2(0f, -125f), new Vector2(245f, 340f)),
                new RectSpec("ShuffleItemButton", new Vector2(265f, -125f), new Vector2(245f, 340f)),
                new RectSpec("BuyButton", new Vector2(0f, -590f), new Vector2(560f, 130f))
            });

            CreateWireframePopupPrefab<EnergyPopup>("EnergyPopup", new[]
            {
                new RectSpec("PanelImageSlot", Vector2.zero, new Vector2(820f, 1450f)),
                new RectSpec("TitleImageSlot", new Vector2(0f, 560f), new Vector2(560f, 210f)),
                new RectSpec("EnergyIconSlot", new Vector2(0f, 260f), new Vector2(410f, 340f)),
                new RectSpec("EnergyAmountSlot", new Vector2(0f, 40f), new Vector2(360f, 95f)),
                new RectSpec("MessageSlot", new Vector2(0f, -115f), new Vector2(600f, 120f))
            }, new[]
            {
                new RectSpec("CloseIconButton", new Vector2(285f, 660f), new Vector2(112f, 112f)),
                new RectSpec("WatchAdButton", new Vector2(0f, -360f), new Vector2(700f, 130f)),
                new RectSpec("RefillButton", new Vector2(0f, -570f), new Vector2(700f, 140f))
            });

            CreateWireframePopupPrefab<DailyChallengePopup>("DailyChallengePopup", new[]
            {
                new RectSpec("PanelImageSlot", Vector2.zero, new Vector2(850f, 1320f)),
                new RectSpec("InfoCardSlot", new Vector2(0f, 280f), new Vector2(700f, 330f)),
                new RectSpec("RewardPreviewSlot", new Vector2(0f, -230f), new Vector2(690f, 180f))
            }, new[]
            {
                new RectSpec("CloseButton", new Vector2(330f, 575f), new Vector2(112f, 112f)),
                new RectSpec("PlayButton", new Vector2(0f, -515f), new Vector2(680f, 130f))
            });

            CreateWireframePopupPrefab<AccountPopup>("AccountPopup", new[]
            {
                new RectSpec("PanelImageSlot", Vector2.zero, new Vector2(850f, 1380f)),
                new RectSpec("UserAvatarEmptySlot", new Vector2(0f, 330f), new Vector2(230f, 230f)),
                new RectSpec("SignInListSlot", new Vector2(0f, -135f), new Vector2(680f, 520f))
            }, new[]
            {
                new RectSpec("CloseButton", new Vector2(330f, 605f), new Vector2(112f, 112f)),
                new RectSpec("GoogleButton", new Vector2(0f, 60f), new Vector2(650f, 100f)),
                new RectSpec("AppleButton", new Vector2(0f, -60f), new Vector2(650f, 100f)),
                new RectSpec("FacebookButton", new Vector2(0f, -180f), new Vector2(650f, 100f)),
                new RectSpec("EmailButton", new Vector2(0f, -300f), new Vector2(650f, 100f))
            });

            CreateWireframePopupPrefab<RewardPopup>("RewardPopup", new[]
            {
                new RectSpec("PanelImageSlot", Vector2.zero, new Vector2(820f, 1260f)),
                new RectSpec("RewardVisualSlot", new Vector2(0f, 280f), new Vector2(360f, 300f)),
                new RectSpec("UpsellCardSlot", new Vector2(0f, -90f), new Vector2(680f, 210f))
            }, new[]
            {
                new RectSpec("CloseButton", new Vector2(315f, 550f), new Vector2(112f, 112f)),
                new RectSpec("WatchAdButton", new Vector2(0f, -330f), new Vector2(680f, 130f)),
                new RectSpec("ClaimButton", new Vector2(0f, -510f), new Vector2(680f, 130f))
            });

            CreateWireframePopupPrefab<ClearPopup>("ClearPopup", new[]
            {
                new RectSpec("PanelImageSlot", Vector2.zero, new Vector2(860f, 1080f)),
                new RectSpec("StarRow", new Vector2(0f, 210f), new Vector2(520f, 110f))
            }, new[]
            {
                new RectSpec("NextButton", new Vector2(0f, -255f), new Vector2(620f, 92f)),
                new RectSpec("RetryButton", new Vector2(0f, -370f), new Vector2(620f, 92f)),
                new RectSpec("LobbyButton", new Vector2(0f, -485f), new Vector2(620f, 92f))
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static RectSpec[] ConfirmPopupSlots()
        {
            return new[]
            {
                new RectSpec("PanelImageSlot", Vector2.zero, new Vector2(780f, 620f)),
                new RectSpec("TitleImageSlot", new Vector2(0f, 160f), new Vector2(600f, 100f)),
                new RectSpec("MessageSlot", new Vector2(0f, 25f), new Vector2(620f, 150f))
            };
        }

        static RectSpec[] ConfirmPopupButtons()
        {
            return new[]
            {
                new RectSpec("CloseIconButton", new Vector2(330f, 250f), new Vector2(96f, 96f)),
                new RectSpec("CancelButton", new Vector2(-180f, -185f), new Vector2(300f, 115f)),
                new RectSpec("ConfirmButton", new Vector2(180f, -185f), new Vector2(300f, 115f))
            };
        }

        [MenuItem("Tools/Project Link/UI Build/Configure UI Texture Imports")]
        public static void ConfigureUiTextureImports()
        {
            ConfigureSpriteSheet($"{ResourceRoot}/AssetResource1.png", 4, 6, "AssetResource1");
            ConfigureSpriteSheet($"{ResourceRoot}/AssetResource2.png", 3, 5, "AssetResource2");
            ConfigureSpriteSheet($"{ResourceRoot}/AssetResource3.png", 3, 3, "AssetResource3");
            AssetDatabase.SaveAssets();
        }

        static void BuildScene(string sceneName)
        {
            EnsureEventSystem();

            var root = CreateCanvasRoot(sceneName);
            var safe = CreateSafeArea(root.transform);
            var router = root.AddComponent<RuntimeNavigationButtons>();
            ConfigureEscapeHandler(root, sceneName, router);

            switch (sceneName)
            {
                case "Bootstrap":
                    BuildBootstrap(safe);
                    break;
                case "Title":
                    BuildTitle(safe, router);
                    break;
                case "Lobby":
                    BuildLobby(safe, router);
                    break;
                case "Game":
                    BuildGameShell(safe, router);
                    break;
                default:
                    Debug.LogWarning($"No static UI builder registered for scene '{sceneName}'.");
                    break;
            }
        }

        static GameObject CreateCanvasRoot(string sceneName)
        {
            string rootName = $"StaticUIRoot_{sceneName}";
            var existing = GameObject.Find(rootName);
            if (existing != null)
                Object.DestroyImmediate(existing);

            var root = new GameObject(rootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Stretch(root.GetComponent<RectTransform>());

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sceneName == "Game" ? 1 : 5;
            canvas.sortingLayerName = "UI";

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(Width, Height);
            scaler.matchWidthOrHeight = 0.5f;

            return root;
        }

        static RectTransform CreateSafeArea(Transform parent)
        {
            var go = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        static void BuildBootstrap(RectTransform safe)
        {
            AddImageSlot(safe, "BackgroundImageSlot", Vector2.zero, new Vector2(Width, Height));
            AddImageSlot(safe, "LogoImageSlot", new Vector2(0f, 360f), new Vector2(500f, 430f));
            AddLocalizedLabel(safe, "TitleText", "ui_color", new Vector2(-95f, 50f), new Vector2(360f, 90f), 76f, TextAlignmentOptions.Right, FontStyles.Bold);
            AddLocalizedLabel(safe, "TitleText_Paths", "ui_paths", new Vector2(165f, 50f), new Vector2(360f, 90f), 76f, TextAlignmentOptions.Left, FontStyles.Bold);
            AddLabel(safe, "LoadingLabelText", "", new Vector2(0f, -430f), new Vector2(620f, 85f), 34f);
            AddImageSlot(safe, "ProgressTrackImageSlot", new Vector2(0f, -555f), new Vector2(650f, 70f));
            var progressFill = AddImageSlot(safe, "ProgressFillImageSlot", new Vector2(-170f, -555f), new Vector2(310f, 54f));
            progressFill.GetComponent<Image>().type = Image.Type.Filled;
            progressFill.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
            AddLabel(safe, "VersionText", "", new Vector2(0f, -845f), new Vector2(500f, 55f), 24f);

            var controller = safe.gameObject.AddComponent<BootstrapWireframeController>();
            AssignObject(controller, "loadingLabelText", FindText(safe.gameObject, "LoadingLabelText"));
            AssignObject(controller, "versionText", FindText(safe.gameObject, "VersionText"));
            AssignObject(controller, "progressFillImage", progressFill.GetComponent<Image>());
        }

        static void BuildTitle(RectTransform safe, RuntimeNavigationButtons router)
        {
            AddImageSlot(safe, "BackgroundImageSlot", Vector2.zero, new Vector2(Width, Height));
            AddImageSlot(safe, "LogoImageSlot", new Vector2(0f, 390f), new Vector2(570f, 500f));
            AddLocalizedLabel(safe, "TitleText", "ui_color", new Vector2(-100f, 80f), new Vector2(360f, 110f), 88f, TextAlignmentOptions.Right, FontStyles.Bold);
            AddLocalizedLabel(safe, "TitleText_Paths", "ui_paths", new Vector2(175f, 80f), new Vector2(360f, 110f), 88f, TextAlignmentOptions.Left, FontStyles.Bold);

            var start = AddWireButton(safe, "StartButton", new Vector2(0f, -410f), new Vector2(720f, 150f));
            AddButtonLabel(start, "StartButtonText", "ui_start", 52f);
            UnityEventTools.AddPersistentListener(start.onClick, router.LoadLobby);

            var language = AddWireButton(safe, "LanguageButton", new Vector2(-220f, -685f), new Vector2(360f, 120f));
            AddButtonLabel(language, "LanguageButtonText", "settings_language", 34f);
            UnityEventTools.AddPersistentListener(language.onClick, router.OpenSettingsPopup);

            var account = AddWireButton(safe, "AccountButton", new Vector2(220f, -685f), new Vector2(360f, 120f));
            AddButtonLabel(account, "AccountButtonText", "settings_account", 34f);
            UnityEventTools.AddPersistentListener(account.onClick, router.OpenAccountPopup);

            AddLabel(safe, "VersionText", "", new Vector2(0f, -880f), new Vector2(280f, 55f), 24f);

            var controller = safe.gameObject.AddComponent<TitleWireframeController>();
            AssignObject(controller, "startButton", start);
            AssignObject(controller, "languageButton", language);
            AssignObject(controller, "accountButton", account);
            AssignObject(controller, "versionText", FindText(safe.gameObject, "VersionText"));
            AssignObject(controller, "accountButtonText", FindText(safe.gameObject, "AccountButtonText"));
        }

        static void BuildLobby(RectTransform safe, RuntimeNavigationButtons router)
        {
            var background = AddImageSlot(safe, "BackgroundImageSlot", Vector2.zero, new Vector2(Width, Height));
            Stretch(background);

            var topBar = AddEdgePanel(safe, "TopBar", true, 0f, 150f, new Vector2(40f, 0f));
            var topLayout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(18, 18, 20, 20);
            topLayout.spacing = 18f;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = false;
            topLayout.childForceExpandHeight = true;

            var profile = AddLayoutButton(topBar, "ProfileButton", 120f, 110f);
            AddLabel(profile.GetComponent<RectTransform>(), "ProfileNameText", "", Vector2.zero, new Vector2(120f, 92f), 26f);
            UnityEventTools.AddPersistentListener(profile.onClick, router.OpenAccountPopup);

            var energySlot = AddFlexibleSlot(topBar, "EnergyPill", 280f, 110f);
            AddLabel(energySlot, "EnergyValueText", "", new Vector2(20f, 0f), new Vector2(190f, 80f), 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            UnityEventTools.AddPersistentListener(AddLayoutButton(topBar, "EnergyPlusButton", 90f, 90f).onClick, router.OpenEnergyPopup);

            var coinSlot = AddFlexibleSlot(topBar, "CoinPill", 310f, 110f);
            AddLabel(coinSlot, "CoinValueText", "", new Vector2(20f, 0f), new Vector2(210f, 80f), 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            UnityEventTools.AddPersistentListener(AddLayoutButton(topBar, "CoinPlusButton", 90f, 90f).onClick, router.OpenBuyItemPopup);

            var settings = AddLayoutButton(topBar, "SettingsButton", 120f, 110f);
            AddLabel(settings.GetComponent<RectTransform>(), "SettingsIconText", "SET", Vector2.zero, new Vector2(110f, 90f), 32f);
            UnityEventTools.AddPersistentListener(settings.onClick, router.OpenSettingsPopup);

            var content = AddStretchPanel(safe, "TabContent", 170f, 230f, new Vector2(44f, 0f), ContentSlot);
            var shopPanel = AddChildPanel(content, "ShopPanel", ScrollSlot);
            var homePanel = AddChildPanel(content, "HomePanel", new Color(0.08f, 0.26f, 0.52f, 0.2f));
            var rankingPanel = AddChildPanel(content, "RankingPanel", ScrollSlot);

            BuildLobbyShopPanel(shopPanel);
            BuildLobbyHomePanel(homePanel, router);
            BuildLobbyRankingPanel(rankingPanel);

            var tabs = AddEdgePanel(safe, "BottomTabBar", false, 24f, 180f, new Vector2(80f, 0f));
            var tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabLayout.padding = new RectOffset(16, 16, 16, 16);
            tabLayout.spacing = 14f;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = true;
            tabLayout.childForceExpandHeight = true;

            var shopTab = AddLayoutButton(tabs, "ShopTabButton", 0f, 140f, true);
            var homeTab = AddLayoutButton(tabs, "HomeTabButton", 0f, 140f, true);
            var rankingTab = AddLayoutButton(tabs, "RankingTabButton", 0f, 140f, true);

            var lobbyController = safe.gameObject.AddComponent<LobbyWireframeController>();
            AssignObject(lobbyController, "shopContent", FindRect(safe.gameObject, "ShopContent"));
            AssignObject(lobbyController, "rankingContent", FindRect(safe.gameObject, "RankingContent"));
            AssignObject(lobbyController, "playButton", FindButton(safe.gameObject, "PlayButton"));
            AssignObject(lobbyController, "refillButton", FindButton(safe.gameObject, "RefillCTAButton"));

            AddLobbyTabController(safe.gameObject, shopTab, homeTab, rankingTab, shopPanel.gameObject, homePanel.gameObject, rankingPanel.gameObject);
        }

        static void BuildLobbyShopPanel(RectTransform panel)
        {
            AddLocalizedLabel(panel, "ShopTitleText", "nav_shop", new Vector2(0f, 535f), new Vector2(400f, 90f), 52f, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLabel(panel, "ShopBalanceText", "", new Vector2(335f, 535f), new Vector2(250f, 75f), 34f, TextAlignmentOptions.Right, FontStyles.Bold);

            var tabs = AddEdgePanel(panel, "CategoryTabs", true, 135f, 95f, new Vector2(72f, 0f));
            var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            string[] categoryNames = { "Coins", "Items", "Bundles", "NoAds" };
            foreach (var categoryName in categoryNames)
                AddLabel(AddLayoutImage(tabs, $"Tab_{categoryName}", 0f, 78f, ButtonSlot), "Label", categoryName, Vector2.zero, new Vector2(160f, 70f), 24f, TextAlignmentOptions.Center, FontStyles.Bold);

            var productScroll = AddScrollView(panel, "ProductScroll", 255f, 32f, 0, "ProductCard");
            productScroll.content.name = "ShopContent";
        }

        static void BuildLobbyHomePanel(RectTransform panel, RuntimeNavigationButtons router)
        {
            var daily = AddWireButton(panel, "DailyChallengeButton", new Vector2(-310f, 460f), new Vector2(360f, 130f));
            AddButtonLabel(daily, "DailyChallengeButtonText", "lobby_daily_challenge", 28f);
            AddLabel(daily.GetComponent<RectTransform>(), "DailyProgressText", "", new Vector2(0f, -38f), new Vector2(220f, 40f), 20f);
            UnityEventTools.AddPersistentListener(daily.onClick, router.OpenDailyChallengePopup);

            var colorCup = AddWireButton(panel, "ColorCupButton", new Vector2(310f, 460f), new Vector2(360f, 130f));
            AddButtonLabel(colorCup, "ColorCupButtonText", "lobby_color_cup", 28f);
            AddLabel(colorCup.GetComponent<RectTransform>(), "ColorCupTimerText", "", new Vector2(0f, -38f), new Vector2(260f, 40f), 20f);

            AddImageSlot(panel, "CurrentStageCardSlot", new Vector2(0f, -10f), new Vector2(430f, 650f));
            AddLocalizedLabel(panel, "CurrentStageTitleText", "lobby_current_stage", new Vector2(0f, 205f), new Vector2(360f, 60f), 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLabel(panel, "CurrentStageNumberText", "", new Vector2(0f, 55f), new Vector2(280f, 130f), 86f, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLabel(panel, "StageStarsValueText", "", new Vector2(0f, -65f), new Vector2(260f, 58f), 34f);
            AddImageSlot(panel, "NextStageCardLeftSlot", new Vector2(-395f, -10f), new Vector2(160f, 420f));
            AddImageSlot(panel, "NextStageCardRightSlot", new Vector2(395f, -10f), new Vector2(160f, 420f));
            var play = AddWireButton(panel, "PlayButton", new Vector2(0f, -330f), new Vector2(460f, 130f));
            AddButtonLabel(play, "PlayButtonText", "ui_play", 46f);
            UnityEventTools.AddPersistentListener(play.onClick, router.LoadGame);

            AddLabel(panel, "PlayDisabledReasonText", "", new Vector2(0f, -435f), new Vector2(520f, 55f), 28f, TextAlignmentOptions.Center, FontStyles.Bold);
            var refill = AddWireButton(panel, "RefillCTAButton", new Vector2(0f, -520f), new Vector2(500f, 110f));
            AddButtonLabel(refill, "RefillCTAButtonText", "popup_refill", 36f);
            UnityEventTools.AddPersistentListener(refill.onClick, router.OpenEnergyPopup);
            refill.gameObject.SetActive(false);
        }

        static void BuildLobbyRankingPanel(RectTransform panel)
        {
            AddLocalizedLabel(panel, "RankingTitleText", "nav_ranking", new Vector2(0f, 535f), new Vector2(420f, 90f), 52f, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLabel(panel, "RankingMetricText", "", new Vector2(330f, 535f), new Vector2(280f, 64f), 26f, TextAlignmentOptions.Right);

            var tabs = AddEdgePanel(panel, "SegmentTabs", true, 135f, 95f, new Vector2(100f, 0f));
            var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            string[] segments = { "Friends", "Global", "Color Cup" };
            foreach (var segment in segments)
                AddLabel(AddLayoutImage(tabs, $"Tab_{segment.Replace(" ", "")}", 0f, 78f, ButtonSlot), "Label", segment, Vector2.zero, new Vector2(230f, 70f), 24f, TextAlignmentOptions.Center, FontStyles.Bold);

            AddLabel(panel, "RankingErrorText", "", new Vector2(0f, 430f), new Vector2(760f, 48f), 24f);
            var rankList = AddScrollView(panel, "RankList", 255f, 32f, 0, "RankRow");
            rankList.content.name = "RankingContent";
        }

        static void BuildGameShell(RectTransform safe, RuntimeNavigationButtons router)
        {
            AddImageSlot(safe, "BackgroundImageSlot", Vector2.zero, new Vector2(Width, Height));
            AddImageSlot(safe, "LevelHeaderSlot", new Vector2(0f, 805f), new Vector2(420f, 180f));
            AddLabel(safe, "LevelLabelText", "", new Vector2(0f, 840f), new Vector2(360f, 60f), 34f, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLabel(safe, "MoveCounterText", "", new Vector2(0f, 775f), new Vector2(280f, 78f), 56f, TextAlignmentOptions.Center, FontStyles.Bold);
            var progress = AddImageSlot(safe, "ColorProgressBar", new Vector2(0f, 690f), new Vector2(360f, 54f));
            var progressLayout = progress.gameObject.AddComponent<HorizontalLayoutGroup>();
            progressLayout.spacing = 14f;
            progressLayout.padding = new RectOffset(16, 16, 6, 6);
            progressLayout.childAlignment = TextAnchor.MiddleCenter;
            progressLayout.childControlWidth = false;
            for (int i = 0; i < 4; i++)
                AddLayoutImage(progress, $"ColorProgressDot_{i + 1}", 44f, 44f, AccentButtonSlot);
            AddImageSlot(safe, "PlayfieldSlot", new Vector2(0f, 120f), new Vector2(940f, 1240f));
            AddImageSlot(safe, "BottomToolBarSlot", new Vector2(0f, -795f), new Vector2(980f, 170f));

            var back = AddWireButton(safe, "BackButton", new Vector2(-425f, 805f), new Vector2(120f, 120f));
            AddLabel(back.GetComponent<RectTransform>(), "BackIconText", "BACK", Vector2.zero, new Vector2(100f, 100f), 28f);
            UnityEventTools.AddPersistentListener(back.onClick, router.OpenReturnTitlePopup);

            var settings = AddWireButton(safe, "SettingsButton", new Vector2(425f, 805f), new Vector2(120f, 120f));
            AddLabel(settings.GetComponent<RectTransform>(), "SettingsIconText", "SET", Vector2.zero, new Vector2(100f, 100f), 28f);
            UnityEventTools.AddPersistentListener(settings.onClick, router.OpenSettingsPopup);

            AddToolButton(safe, "HintButton", "game_hint", new Vector2(-405f, -795f), router.OpenBuyItemPopup);
            AddToolButton(safe, "UndoButton", "game_undo", new Vector2(-225f, -795f), router.OpenBuyItemPopup);
            AddToolButton(safe, "PaintItemButton", "item_paint", new Vector2(120f, -795f), router.OpenBuyItemPopup);
            AddToolButton(safe, "HammerButton", "item_hammer", new Vector2(295f, -795f), router.OpenBuyItemPopup);
            AddToolButton(safe, "BrushButton", "item_shuffle", new Vector2(470f, -795f), router.OpenBuyItemPopup);

            var controller = safe.gameObject.AddComponent<GameWireframeController>();
            AssignObject(controller, "backButton", back);
            AssignObject(controller, "settingsButton", settings);
            AssignObject(controller, "hintButton", FindButton(safe.gameObject, "HintButton"));
            AssignObject(controller, "undoButton", FindButton(safe.gameObject, "UndoButton"));
            AssignObject(controller, "paintItemButton", FindButton(safe.gameObject, "PaintItemButton"));
            AssignObject(controller, "hammerButton", FindButton(safe.gameObject, "HammerButton"));
            AssignObject(controller, "brushButton", FindButton(safe.gameObject, "BrushButton"));
            AssignObject(controller, "levelLabelText", FindText(safe.gameObject, "LevelLabelText"));
            AssignObject(controller, "moveCounterText", FindText(safe.gameObject, "MoveCounterText"));
        }

        static void AddToolButton(RectTransform parent, string name, string labelId, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            var button = AddWireButton(parent, name, position, new Vector2(140f, 140f));
            AddButtonLabel(button, $"{name}Text", labelId, 20f);
            AddLabel(button.GetComponent<RectTransform>(), $"{name}CountText", "", new Vector2(38f, -42f), new Vector2(54f, 36f), 20f, TextAlignmentOptions.Center, FontStyles.Bold);
            UnityEventTools.AddPersistentListener(button.onClick, onClick);
        }

        static TextMeshProUGUI AddLabel(RectTransform parent, string name, string text, Vector2 position, Vector2 size, float fontSize = 36f, TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Center(go.GetComponent<RectTransform>(), position, size);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontSizeMin = Mathf.Max(12f, fontSize * 0.55f);
            label.fontSizeMax = fontSize;
            label.enableAutoSizing = true;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = new Color(0.96f, 0.98f, 1f, 1f);
            label.raycastTarget = false;
            return label;
        }

        static TextMeshProUGUI AddLocalizedLabel(RectTransform parent, string name, string stringId, Vector2 position, Vector2 size, float fontSize = 36f, TextAlignmentOptions alignment = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            var label = AddLabel(parent, name, "", position, size, fontSize, alignment, style);
            label.gameObject.AddComponent<LocalizedText>().SetStringId(stringId);
            return label;
        }

        static void AddButtonLabel(Button button, string name, string stringId, float fontSize = 34f)
        {
            AddLocalizedLabel(button.GetComponent<RectTransform>(), name, stringId, Vector2.zero, button.GetComponent<RectTransform>().sizeDelta, fontSize, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        static RectTransform AddImageSlot(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Center(go.GetComponent<RectTransform>(), position, size);

            var image = go.GetComponent<Image>();
            image.color = name.Contains("Background") ? BackgroundSlot : ContentSlot;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return go.GetComponent<RectTransform>();
        }

        static Button AddWireButton(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Center(go.GetComponent<RectTransform>(), position, size);

            var image = go.GetComponent<Image>();
            image.color = ButtonSlot;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }

        static RectTransform AddEdgePanel(RectTransform parent, string name, bool top, float margin, float height, Vector2 horizontalPadding)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = top ? new Vector2(0f, 1f) : Vector2.zero;
            rect.anchorMax = top ? Vector2.one : new Vector2(1f, 0f);
            rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(horizontalPadding.x, top ? -margin - height : margin);
            rect.offsetMax = new Vector2(-horizontalPadding.x, top ? -margin : margin + height);
            go.GetComponent<Image>().color = ContentSlot;
            go.GetComponent<Image>().raycastTarget = false;
            return rect;
        }

        static RectTransform AddStretchPanel(RectTransform parent, string name, float top, float bottom, Vector2 horizontalPadding, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalPadding.x, bottom);
            rect.offsetMax = new Vector2(-horizontalPadding.x, -top);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform AddChildPanel(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static RectTransform AddFlexibleSlot(RectTransform parent, string name, float preferredWidth, float preferredHeight)
        {
            var slot = AddLayoutImage(parent, name, preferredWidth, preferredHeight, ContentSlot);
            var layout = slot.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1f;
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
            return slot;
        }

        static Button AddLayoutButton(RectTransform parent, string name, float preferredWidth, float preferredHeight, bool flexible = false)
        {
            var rect = AddLayoutImage(parent, name, preferredWidth, preferredHeight, flexible ? AccentButtonSlot : ButtonSlot);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth;
            layout.preferredHeight = preferredHeight;
            layout.flexibleWidth = flexible ? 1f : 0f;
            layout.flexibleHeight = 1f;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        static RectTransform AddLayoutImage(RectTransform parent, string name, float preferredWidth, float preferredHeight, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(preferredWidth, preferredHeight);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        static ScrollRect AddScrollView(RectTransform parent, string name, float top, float bottom, int itemCount, string itemPrefix)
        {
            var root = AddStretchPanel(parent, name, top, bottom, new Vector2(32f, 0f), ScrollSlot);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(root, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < itemCount; i++)
            {
                var item = AddLayoutImage(contentRect, $"{itemPrefix}_{i + 1:00}", 0f, 120f, ContentSlot);
                var element = item.gameObject.AddComponent<LayoutElement>();
                element.preferredHeight = 120f;
                element.flexibleWidth = 1f;
            }

            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return scroll;
        }

        static void CreateWireframePopupPrefab<T>(string prefabName, IReadOnlyList<RectSpec> imageSlots, IReadOnlyList<RectSpec> buttons) where T : PopupBase
        {
            var root = new GameObject(prefabName, typeof(RectTransform), typeof(T));
            Stretch(root.GetComponent<RectTransform>());

            foreach (var slot in imageSlots)
                AddImageSlot(root.GetComponent<RectTransform>(), slot.Name, slot.Position, slot.Size);

            foreach (var button in buttons)
                AddWireButton(root.GetComponent<RectTransform>(), button.Name, button.Position, button.Size);

            DecoratePopup(root, prefabName);
            AssignPopupReferences(root);
            PrefabUtility.SaveAsPrefabAsset(root, $"{PopupPrefabRoot}/{prefabName}.prefab");
            Object.DestroyImmediate(root);
        }

        static void DecoratePopup(GameObject root, string prefabName)
        {
            var rect = root.GetComponent<RectTransform>();

            switch (prefabName)
            {
                case "SettingPopup":
                    AddLocalizedLabel(rect, "TitleText", "settings_language", new Vector2(0f, 620f), new Vector2(520f, 80f), 48f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddSettingRow(rect, "Bgm", "settings_bgm", 350f);
                    AddSettingRow(rect, "Sfx", "settings_sfx", 195f);
                    AddSettingRow(rect, "Haptics", "settings_haptics", 40f);
                    AddSettingRow(rect, "Notifications", "settings_notifications", -115f);
                    AddSettingRow(rect, "Language", "settings_language", -270f);
                    AddLocalizedLabel(rect, "AccountLabelText", "settings_account", new Vector2(-215f, -455f), new Vector2(260f, 70f), 32f, TextAlignmentOptions.Left, FontStyles.Bold);
                    AddLabel(rect, "AccountStatusText", "", new Vector2(155f, -455f), new Vector2(250f, 70f), 28f, TextAlignmentOptions.Right);
                    AddButtonLabel(FindButton(root, "CloseButton"), "CloseButtonText", "ui_close", 32f);
                    AddButtonLabel(FindButton(root, "SaveButton"), "SaveButtonText", "ui_save", 32f);
                    break;
                case "BuyItemPopup":
                    AddLocalizedLabel(rect, "TitleText", "popup_buy_item_title", new Vector2(0f, 610f), new Vector2(520f, 80f), 48f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "FeaturedNameText", "", new Vector2(0f, 350f), new Vector2(560f, 70f), 38f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "FeaturedDescriptionText", "", new Vector2(0f, 260f), new Vector2(560f, 100f), 28f);
                    var grid = AddImageSlot(rect, "ItemGrid", new Vector2(0f, -125f), new Vector2(770f, 340f));
                    var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
                    gridLayout.cellSize = new Vector2(220f, 220f);
                    gridLayout.spacing = new Vector2(24f, 24f);
                    gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    gridLayout.constraintCount = 3;
                    gridLayout.childAlignment = TextAnchor.MiddleCenter;
                    AddLabel(rect, "SoftBalanceText", "", new Vector2(-180f, -485f), new Vector2(270f, 60f), 28f, TextAlignmentOptions.Left);
                    AddLabel(rect, "PriceText", "", new Vector2(0f, -535f), new Vector2(300f, 70f), 38f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddButtonLabel(FindButton(root, "BuyButton"), "BuyButtonText", "ui_buy", 38f);
                    break;
                case "EnergyPopup":
                    AddLocalizedLabel(rect, "TitleText", "popup_refill", new Vector2(0f, 575f), new Vector2(560f, 90f), 52f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "EnergyCounterText", "", new Vector2(0f, 70f), new Vector2(360f, 90f), 46f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLocalizedLabel(rect, "DescriptionText", "popup_energy_empty", new Vector2(0f, -105f), new Vector2(620f, 120f), 28f);
                    AddButtonLabel(FindButton(root, "WatchAdButton"), "WatchAdButtonText", "popup_watch_ad", 34f);
                    AddLabel(FindButton(root, "WatchAdButton").GetComponent<RectTransform>(), "WatchAdRewardText", "", new Vector2(210f, 0f), new Vector2(160f, 80f), 30f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddButtonLabel(FindButton(root, "RefillButton"), "RefillButtonText", "popup_refill", 34f);
                    AddLabel(FindButton(root, "RefillButton").GetComponent<RectTransform>(), "RefillRewardText", "", new Vector2(210f, 0f), new Vector2(160f, 80f), 30f, TextAlignmentOptions.Center, FontStyles.Bold);
                    break;
                case "DailyChallengePopup":
                    AddLocalizedLabel(rect, "TitleText", "lobby_daily_challenge", new Vector2(0f, 535f), new Vector2(560f, 90f), 50f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "CountdownTimerText", "", new Vector2(0f, 330f), new Vector2(620f, 60f), 30f);
                    AddLabel(rect, "DailyProgressText", "", new Vector2(0f, 245f), new Vector2(500f, 60f), 34f, TextAlignmentOptions.Center, FontStyles.Bold);
                    var streak = AddImageSlot(rect, "WeekStreakRow", new Vector2(0f, 55f), new Vector2(700f, 100f));
                    var streakLayout = streak.gameObject.AddComponent<HorizontalLayoutGroup>();
                    streakLayout.spacing = 12f;
                    streakLayout.childAlignment = TextAnchor.MiddleCenter;
                    AddLabel(rect, "RewardPreviewText", "", new Vector2(0f, -230f), new Vector2(620f, 90f), 30f);
                    AddButtonLabel(FindButton(root, "PlayButton"), "PlayButtonText", "ui_play", 38f);
                    break;
                case "AccountPopup":
                    AddLocalizedLabel(rect, "TitleText", "settings_account", new Vector2(0f, 560f), new Vector2(520f, 90f), 52f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "DisplayNameText", "", new Vector2(0f, 170f), new Vector2(600f, 70f), 34f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "AccountStatusText", "", new Vector2(0f, 105f), new Vector2(620f, 60f), 26f);
                    AddLabel(FindButton(root, "GoogleButton").GetComponent<RectTransform>(), "GoogleButtonText", "Continue with Google", Vector2.zero, new Vector2(560f, 80f), 28f);
                    AddLabel(FindButton(root, "AppleButton").GetComponent<RectTransform>(), "AppleButtonText", "Continue with Apple", Vector2.zero, new Vector2(560f, 80f), 28f);
                    AddLabel(FindButton(root, "FacebookButton").GetComponent<RectTransform>(), "FacebookButtonText", "Continue with Facebook", Vector2.zero, new Vector2(560f, 80f), 28f);
                    AddLabel(FindButton(root, "EmailButton").GetComponent<RectTransform>(), "EmailButtonText", "Continue with Email", Vector2.zero, new Vector2(560f, 80f), 28f);
                    break;
                case "RewardPopup":
                    AddLabel(rect, "TitleText", "You got a reward!", new Vector2(0f, 535f), new Vector2(620f, 90f), 48f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "RewardAmountText", "", new Vector2(0f, 120f), new Vector2(620f, 100f), 60f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "UpsellText", "Watch a video to 3x your reward!", new Vector2(0f, -90f), new Vector2(620f, 80f), 28f);
                    AddButtonLabel(FindButton(root, "WatchAdButton"), "WatchAdButtonText", "popup_watch_ad", 34f);
                    AddLabel(FindButton(root, "ClaimButton").GetComponent<RectTransform>(), "ClaimButtonText", "Claim", Vector2.zero, new Vector2(580f, 85f), 34f, TextAlignmentOptions.Center, FontStyles.Bold);
                    break;
                case "ClearPopup":
                    AddLocalizedLabel(rect, "TitleText", "game_stage_clear", new Vector2(0f, 430f), new Vector2(620f, 90f), 54f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "StageText", "", new Vector2(0f, 340f), new Vector2(560f, 64f), 38f, TextAlignmentOptions.Center, FontStyles.Bold);
                    var starRow = FindRect(root, "StarRow");
                    var starLayout = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
                    starLayout.spacing = 28f;
                    starLayout.childAlignment = TextAnchor.MiddleCenter;
                    AddLabel(rect, "RewardText", "", new Vector2(0f, 80f), new Vector2(620f, 62f), 34f, TextAlignmentOptions.Center, FontStyles.Bold);
                    AddLabel(rect, "MovesText", "", new Vector2(0f, 10f), new Vector2(620f, 56f), 30f);
                    AddLabel(rect, "ScoreText", "", new Vector2(0f, -55f), new Vector2(620f, 56f), 30f);
                    AddButtonLabel(FindButton(root, "NextButton"), "NextButtonText", "game_next", 36f);
                    AddButtonLabel(FindButton(root, "RetryButton"), "RetryButtonText", "game_retry", 36f);
                    AddButtonLabel(FindButton(root, "LobbyButton"), "LobbyButtonText", "game_lobby", 36f);
                    break;
            }
        }

        static void AddSettingRow(RectTransform parent, string prefix, string stringId, float y)
        {
            AddLocalizedLabel(parent, $"{prefix}LabelText", stringId, new Vector2(-230f, y), new Vector2(320f, 70f), 30f, TextAlignmentOptions.Left, FontStyles.Bold);
            AddLabel(parent, $"{prefix}ValueText", "", new Vector2(235f, y), new Vector2(190f, 70f), 28f, TextAlignmentOptions.Right);
        }

        static void AssignPopupReferences(GameObject root)
        {
            if (root.TryGetComponent(out SettingPopup settingPopup))
            {
                AssignObject(settingPopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(settingPopup, "closeIconButton", FindButton(root, "CloseIconButton"));
                AssignObject(settingPopup, "saveButton", FindButton(root, "SaveButton"));
                AssignObject(settingPopup, "accountStatusText", FindText(root, "AccountStatusText"));
            }

            if (root.TryGetComponent(out BuyItemPopup buyItemPopup))
            {
                AssignObject(buyItemPopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(buyItemPopup, "closeIconButton", FindButton(root, "CloseIconButton"));
                AssignObject(buyItemPopup, "buyButton", FindButton(root, "BuyButton"));
                AssignObject(buyItemPopup, "featuredNameText", FindText(root, "FeaturedNameText"));
                AssignObject(buyItemPopup, "featuredDescriptionText", FindText(root, "FeaturedDescriptionText"));
                AssignObject(buyItemPopup, "priceText", FindText(root, "PriceText"));
                AssignObject(buyItemPopup, "softBalanceText", FindText(root, "SoftBalanceText"));
                AssignObject(buyItemPopup, "itemGrid", FindRect(root, "ItemGrid"));
            }

            if (root.TryGetComponent(out EnergyPopup energyPopup))
            {
                AssignObject(energyPopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(energyPopup, "closeIconButton", FindButton(root, "CloseIconButton"));
                AssignObject(energyPopup, "watchAdButton", FindButton(root, "WatchAdButton"));
                AssignObject(energyPopup, "refillButton", FindButton(root, "RefillButton"));
                AssignObject(energyPopup, "energyCounterText", FindText(root, "EnergyCounterText"));
                AssignObject(energyPopup, "watchAdRewardText", FindText(root, "WatchAdRewardText"));
                AssignObject(energyPopup, "refillRewardText", FindText(root, "RefillRewardText"));
            }

            if (root.TryGetComponent(out ReturnTitlePopup returnTitlePopup))
            {
                AssignObject(returnTitlePopup, "closeIconButton", FindButton(root, "CloseIconButton"));
                AssignObject(returnTitlePopup, "cancelButton", FindButton(root, "CancelButton"));
                AssignObject(returnTitlePopup, "confirmButton", FindButton(root, "ConfirmButton"));
            }

            if (root.TryGetComponent(out ExitGamePopup exitGamePopup))
            {
                AssignObject(exitGamePopup, "closeIconButton", FindButton(root, "CloseIconButton"));
                AssignObject(exitGamePopup, "cancelButton", FindButton(root, "CancelButton"));
                AssignObject(exitGamePopup, "confirmButton", FindButton(root, "ConfirmButton"));
            }

            if (root.TryGetComponent(out DailyChallengePopup dailyChallengePopup))
            {
                AssignObject(dailyChallengePopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(dailyChallengePopup, "playButton", FindButton(root, "PlayButton"));
                AssignObject(dailyChallengePopup, "countdownText", FindText(root, "CountdownTimerText"));
                AssignObject(dailyChallengePopup, "progressText", FindText(root, "DailyProgressText"));
                AssignObject(dailyChallengePopup, "rewardText", FindText(root, "RewardPreviewText"));
                AssignObject(dailyChallengePopup, "streakRow", FindRect(root, "WeekStreakRow"));
            }

            if (root.TryGetComponent(out AccountPopup accountPopup))
            {
                AssignObject(accountPopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(accountPopup, "displayNameText", FindText(root, "DisplayNameText"));
                AssignObject(accountPopup, "accountStatusText", FindText(root, "AccountStatusText"));
            }

            if (root.TryGetComponent(out RewardPopup rewardPopup))
            {
                AssignObject(rewardPopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(rewardPopup, "watchAdButton", FindButton(root, "WatchAdButton"));
                AssignObject(rewardPopup, "claimButton", FindButton(root, "ClaimButton"));
                AssignObject(rewardPopup, "rewardAmountText", FindText(root, "RewardAmountText"));
            }

            if (root.TryGetComponent(out ClearPopup clearPopup))
            {
                AssignObject(clearPopup, "nextButton", FindButton(root, "NextButton"));
                AssignObject(clearPopup, "retryButton", FindButton(root, "RetryButton"));
                AssignObject(clearPopup, "lobbyButton", FindButton(root, "LobbyButton"));
                AssignObject(clearPopup, "stageText", FindText(root, "StageText"));
                AssignObject(clearPopup, "rewardText", FindText(root, "RewardText"));
                AssignObject(clearPopup, "movesText", FindText(root, "MovesText"));
                AssignObject(clearPopup, "scoreText", FindText(root, "ScoreText"));
                AssignObject(clearPopup, "starRow", FindRect(root, "StarRow"));
            }
        }

        static void AddLobbyTabController(GameObject root, Button shopTab, Button homeTab, Button rankingTab, GameObject shopPanel, GameObject homePanel, GameObject rankingPanel)
        {
            var controllerType = System.Type.GetType("ProjectLink.OutGame.UI.LobbyTabController, Assembly-CSharp");
            if (controllerType == null)
            {
                Debug.LogWarning("LobbyTabController type not found. Regenerate Unity project files or let Unity compile scripts before rebuilding Lobby UI.");
                return;
            }

            var controller = root.AddComponent(controllerType);
            AssignObject(controller, "shopTabButton", shopTab);
            AssignObject(controller, "homeTabButton", homeTab);
            AssignObject(controller, "rankingTabButton", rankingTab);
            AssignObject(controller, "shopPanel", shopPanel);
            AssignObject(controller, "homePanel", homePanel);
            AssignObject(controller, "rankingPanel", rankingPanel);
        }

        static Button FindButton(GameObject root, string buttonName)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                    return button;
            }

            return null;
        }

        static RectTransform FindRect(GameObject root, string rectName)
        {
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == rectName)
                    return rect;
            }

            return null;
        }

        static TextMeshProUGUI FindText(GameObject root, string textName)
        {
            foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text.name == textName)
                    return text;
            }

            return null;
        }

        static void AssignObject(Object target, string propertyName, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null) return;

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureSpriteSheet(string path, int columns, int rows, string prefix)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null) return;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null) return;

            dataProvider.InitSpriteEditorDataProvider();

            int cellW = texture.width / columns;
            int cellH = texture.height / rows;
            var rects = new SpriteRect[columns * rows];
            var pairs = new List<SpriteNameFileIdPair>(rects.Length);
            int index = 0;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    var spriteId = GUID.Generate();
                    string spriteName = $"{prefix}_{index:00}";
                    rects[index] = new SpriteRect
                    {
                        name = spriteName,
                        spriteID = spriteId,
                        rect = new Rect(col * cellW, texture.height - (row + 1) * cellH, cellW, cellH),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                    pairs.Add(new SpriteNameFileIdPair(spriteName, spriteId));
                    index++;
                }
            }

            dataProvider.SetSpriteRects(rects);
            var nameProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            nameProvider?.SetNameFileIdPairs(pairs);
            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        static void ConfigureEscapeHandler(GameObject root, string sceneName, RuntimeNavigationButtons router)
        {
            var handler = root.AddComponent<SceneEscapeHandler>();
            var so = new SerializedObject(handler);
            so.FindProperty("navigation").objectReferenceValue = router;

            int action = sceneName switch
            {
                "Title" => (int)SceneEscapeHandler.EscapeAction.ExitGame,
                "Lobby" => (int)SceneEscapeHandler.EscapeAction.ReturnToTitle,
                _ => (int)SceneEscapeHandler.EscapeAction.None
            };

            so.FindProperty("action").enumValueIndex = action;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
