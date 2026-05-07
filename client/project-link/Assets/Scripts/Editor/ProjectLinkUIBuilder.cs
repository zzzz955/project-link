using System.Collections.Generic;
using ProjectLink.Core;
using ProjectLink.OutGame.UI;
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
            AddImageSlot(safe, "LogoImageSlot", new Vector2(0f, 305f), new Vector2(650f, 620f));
            AddImageSlot(safe, "LoadingLabelSlot", new Vector2(0f, -430f), new Vector2(420f, 85f));
            AddImageSlot(safe, "ProgressTrackImageSlot", new Vector2(0f, -555f), new Vector2(650f, 70f));
            AddImageSlot(safe, "ProgressFillImageSlot", new Vector2(-170f, -555f), new Vector2(310f, 54f));
        }

        static void BuildTitle(RectTransform safe, RuntimeNavigationButtons router)
        {
            AddImageSlot(safe, "BackgroundImageSlot", Vector2.zero, new Vector2(Width, Height));
            AddImageSlot(safe, "LogoImageSlot", new Vector2(0f, 280f), new Vector2(720f, 640f));

            var start = AddWireButton(safe, "StartButton", new Vector2(0f, -410f), new Vector2(720f, 150f));
            UnityEventTools.AddPersistentListener(start.onClick, router.LoadLobby);

            var language = AddWireButton(safe, "LanguageButton", new Vector2(-220f, -685f), new Vector2(360f, 120f));
            UnityEventTools.AddPersistentListener(language.onClick, router.OpenSettingsPopup);

            var account = AddWireButton(safe, "AccountButton", new Vector2(220f, -685f), new Vector2(360f, 120f));
            UnityEventTools.AddPersistentListener(account.onClick, router.OpenSettingsPopup);

            AddImageSlot(safe, "VersionLabelSlot", new Vector2(0f, -880f), new Vector2(180f, 55f));
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

            UnityEventTools.AddPersistentListener(AddLayoutButton(topBar, "ProfileButton", 120f, 110f).onClick, router.OpenSettingsPopup);
            AddFlexibleSlot(topBar, "EnergyBarSlot", 280f, 110f);
            UnityEventTools.AddPersistentListener(AddLayoutButton(topBar, "EnergyPlusButton", 90f, 90f).onClick, router.OpenEnergyPopup);
            AddFlexibleSlot(topBar, "CoinBarSlot", 310f, 110f);
            UnityEventTools.AddPersistentListener(AddLayoutButton(topBar, "CoinPlusButton", 90f, 90f).onClick, router.OpenBuyItemPopup);
            UnityEventTools.AddPersistentListener(AddLayoutButton(topBar, "SettingsButton", 120f, 110f).onClick, router.OpenSettingsPopup);

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

            var tabController = safe.gameObject.AddComponent<LobbyTabController>();
            tabController.Configure(shopTab, homeTab, rankingTab, shopPanel.gameObject, homePanel.gameObject, rankingPanel.gameObject);
        }

        static void BuildLobbyShopPanel(RectTransform panel)
        {
            AddEdgePanel(panel, "ShopHeaderSlot", true, 20f, 120f, new Vector2(32f, 0f));
            AddScrollView(panel, "ShopScrollView", 160f, 32f, 12, "ShopItemSlot");
        }

        static void BuildLobbyHomePanel(RectTransform panel, RuntimeNavigationButtons router)
        {
            AddImageSlot(panel, "DailyChallengeSlot", new Vector2(-310f, 460f), new Vector2(360f, 130f));
            AddImageSlot(panel, "ColorCupSlot", new Vector2(310f, 460f), new Vector2(360f, 130f));
            AddImageSlot(panel, "CurrentStageCardSlot", new Vector2(0f, -10f), new Vector2(430f, 650f));
            AddImageSlot(panel, "NextStageCardLeftSlot", new Vector2(-395f, -10f), new Vector2(160f, 420f));
            AddImageSlot(panel, "NextStageCardRightSlot", new Vector2(395f, -10f), new Vector2(160f, 420f));
            UnityEventTools.AddPersistentListener(AddWireButton(panel, "PlayButton", new Vector2(0f, -330f), new Vector2(460f, 130f)).onClick, router.LoadGame);
        }

        static void BuildLobbyRankingPanel(RectTransform panel)
        {
            AddEdgePanel(panel, "RankingHeaderSlot", true, 20f, 120f, new Vector2(32f, 0f));
            AddScrollView(panel, "RankingScrollView", 160f, 32f, 24, "RankingRowSlot");
        }

        static void BuildGameShell(RectTransform safe, RuntimeNavigationButtons router)
        {
            AddImageSlot(safe, "BackgroundImageSlot", Vector2.zero, new Vector2(Width, Height));
            AddImageSlot(safe, "LevelHeaderSlot", new Vector2(0f, 805f), new Vector2(360f, 160f));
            AddImageSlot(safe, "PlayfieldSlot", new Vector2(0f, 120f), new Vector2(940f, 1240f));
            AddImageSlot(safe, "BottomToolBarSlot", new Vector2(0f, -795f), new Vector2(980f, 170f));

            UnityEventTools.AddPersistentListener(AddWireButton(safe, "BackButton", new Vector2(-425f, 805f), new Vector2(120f, 120f)).onClick, router.OpenReturnTitlePopup);
            UnityEventTools.AddPersistentListener(AddWireButton(safe, "SettingsButton", new Vector2(425f, 805f), new Vector2(120f, 120f)).onClick, router.OpenSettingsPopup);
            UnityEventTools.AddPersistentListener(AddWireButton(safe, "HintButton", new Vector2(-405f, -795f), new Vector2(150f, 150f)).onClick, router.OpenBuyItemPopup);
            UnityEventTools.AddPersistentListener(AddWireButton(safe, "UndoButton", new Vector2(-225f, -795f), new Vector2(150f, 150f)).onClick, router.OpenBuyItemPopup);
            UnityEventTools.AddPersistentListener(AddWireButton(safe, "ShuffleButton", new Vector2(120f, -795f), new Vector2(140f, 140f)).onClick, router.OpenBuyItemPopup);
            UnityEventTools.AddPersistentListener(AddWireButton(safe, "HammerButton", new Vector2(295f, -795f), new Vector2(140f, 140f)).onClick, router.OpenBuyItemPopup);
            UnityEventTools.AddPersistentListener(AddWireButton(safe, "PaintButton", new Vector2(470f, -795f), new Vector2(140f, 140f)).onClick, router.OpenBuyItemPopup);
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

            AssignPopupReferences(root);
            PrefabUtility.SaveAsPrefabAsset(root, $"{PopupPrefabRoot}/{prefabName}.prefab");
            Object.DestroyImmediate(root);
        }

        static void AssignPopupReferences(GameObject root)
        {
            if (root.TryGetComponent(out SettingPopup settingPopup))
            {
                AssignObject(settingPopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(settingPopup, "closeIconButton", FindButton(root, "CloseIconButton"));
                AssignObject(settingPopup, "saveButton", FindButton(root, "SaveButton"));
            }

            if (root.TryGetComponent(out BuyItemPopup buyItemPopup))
            {
                AssignObject(buyItemPopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(buyItemPopup, "closeIconButton", FindButton(root, "CloseIconButton"));
                AssignObject(buyItemPopup, "buyButton", FindButton(root, "BuyButton"));
            }

            if (root.TryGetComponent(out EnergyPopup energyPopup))
            {
                AssignObject(energyPopup, "closeButton", FindButton(root, "CloseButton"));
                AssignObject(energyPopup, "closeIconButton", FindButton(root, "CloseIconButton"));
                AssignObject(energyPopup, "watchAdButton", FindButton(root, "WatchAdButton"));
                AssignObject(energyPopup, "refillButton", FindButton(root, "RefillButton"));
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
