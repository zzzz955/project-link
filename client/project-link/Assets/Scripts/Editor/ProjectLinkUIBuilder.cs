using ProjectLink.OutGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
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

        static readonly Color Ink = new(0.06f, 0.08f, 0.12f, 1f);
        static readonly Color Aqua = new(0.19f, 0.91f, 0.78f, 1f);
        static readonly Color Coral = new(1f, 0.42f, 0.38f, 1f);
        static readonly Color Lemon = new(1f, 0.78f, 0.32f, 1f);
        static readonly Color White = new(0.96f, 0.98f, 1f, 1f);
        static readonly Color Muted = new(0.63f, 0.7f, 0.78f, 1f);

        [MenuItem("Tools/Project Link/UI Build/Build Current Scene UI")]
        public static void BuildCurrentSceneUI()
        {
            BuildScene(SceneManager.GetActiveScene().name);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Project Link/UI Build/Build All Scene UI")]
        public static void BuildAllSceneUI()
        {
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
                    BuildGameShell(safe);
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
            var rect = root.GetComponent<RectTransform>();
            Stretch(rect);

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
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        static void BuildBootstrap(RectTransform safe)
        {
            AddFullBleed(safe, Ink);
            AddGrid(safe, new Color(1f, 1f, 1f, 0.05f), 120f);

            AddLocalizedLabel(safe, "app_title", 76, FontStyles.Bold, White, new Vector2(0f, 120f), new Vector2(760f, 120f));
            AddLocalizedLabel(safe, "bootstrap_syncing_puzzle_data", 28, FontStyles.UpperCase, Muted, new Vector2(0f, 46f), new Vector2(620f, 52f));

            var rail = AddPanel(safe, "LoadingRail", new Vector2(0f, -52f), new Vector2(620f, 18f), new Color(1f, 1f, 1f, 0.12f));
            var fill = AddPanel(rail, "LoadingFill", new Vector2(-99f, 0f), new Vector2(420f, 18f), Aqua);
            SetLeft(fill);

            AddStatusRow(safe, -222f, "status_data", "status_packets", "status_account");
            AddLocalizedLabel(safe, "bootstrap_server_ready", 24, FontStyles.Normal, Muted, new Vector2(0f, -760f), new Vector2(520f, 44f));
        }

        static void BuildTitle(RectTransform safe, RuntimeNavigationButtons router)
        {
            AddFullBleed(safe, Ink);
            AddGrid(safe, new Color(1f, 1f, 1f, 0.04f), 135f);
            AddPanel(safe, "GearButton", new Vector2(450f, -68f), new Vector2(76f, 76f), new Color(1f, 1f, 1f, 0.1f));
            AddLanguageSelector(safe);

            AddPreviewBoard(safe, new Vector2(0f, 220f), 560f, 7);

            AddLocalizedLabel(safe, "app_title", 88, FontStyles.Bold, White, new Vector2(0f, -178f), new Vector2(820f, 120f));
            AddLocalizedLabel(safe, "title_tagline", 28, FontStyles.UpperCase, Muted, new Vector2(0f, -255f), new Vector2(760f, 54f));

            var play = AddLocalizedButton(safe, "PlayButton", "title_tap_to_start", new Vector2(0f, -520f), new Vector2(760f, 112f), Aqua, Ink);
            UnityEventTools.AddPersistentListener(play.onClick, router.LoadLobby);

            AddLocalizedLabel(safe, "title_guest_profile_region", 24, FontStyles.Normal, Muted, new Vector2(0f, -640f), new Vector2(760f, 44f));
            AddFooterLinks(safe);
        }

        static void BuildLobby(RectTransform safe, RuntimeNavigationButtons router)
        {
            AddFullBleed(safe, new Color(0.055f, 0.07f, 0.1f, 1f));
            AddResourceHeader(safe);
            var back = AddLocalizedButton(safe, "TitleBackButton", "lobby_title_button", new Vector2(-428f, -184f), new Vector2(150f, 68f), new Color(1f, 1f, 1f, 0.1f), White);
            SetTop(back.GetComponent<RectTransform>());
            UnityEventTools.AddPersistentListener(back.onClick, router.OpenReturnTitlePopup);

            AddLocalizedLabel(safe, "lobby_stage_map", 54, FontStyles.Bold, White, new Vector2(-296f, 604f), new Vector2(420f, 78f), TextAlignmentOptions.Left);
            AddLocalizedLabel(safe, "lobby_world_1", 24, FontStyles.UpperCase, Muted, new Vector2(-250f, 552f), new Vector2(560f, 44f), TextAlignmentOptions.Left);

            var mapPanel = AddPanel(safe, "StageMapPanel", new Vector2(0f, 190f), new Vector2(900f, 720f), new Color(1f, 1f, 1f, 0.07f));
            AddGrid(mapPanel, new Color(1f, 1f, 1f, 0.045f), 90f);

            var nodeHost = AddPanel(mapPanel, "StageNodeHost", Vector2.zero, new Vector2(820f, 520f), new Color(0f, 0f, 0f, 0f));
            var nodeTemplate = CreateStageNodeTemplate(mapPanel);
            var previous = AddButton(mapPanel, "PreviousPageButton", "<", new Vector2(-305f, -304f), new Vector2(96f, 66f), new Color(1f, 1f, 1f, 0.1f), White);
            var next = AddButton(mapPanel, "NextPageButton", ">", new Vector2(305f, -304f), new Vector2(96f, 66f), new Color(1f, 1f, 1f, 0.1f), White);
            var pageLabel = AddLabel(mapPanel, "1 / 50", 26, FontStyles.Bold, Muted, new Vector2(0f, -304f), new Vector2(220f, 54f));

            var mapView = mapPanel.gameObject.AddComponent<LobbyStageMapView>();
            var so = new SerializedObject(mapView);
            so.FindProperty("nodeHost").objectReferenceValue = nodeHost;
            so.FindProperty("stageNodePrefab").objectReferenceValue = nodeTemplate.gameObject;
            so.FindProperty("pageLabel").objectReferenceValue = pageLabel;
            so.FindProperty("previousPageButton").objectReferenceValue = previous;
            so.FindProperty("nextPageButton").objectReferenceValue = next;
            so.FindProperty("totalStageCount").intValue = 1000;
            so.FindProperty("stagesPerPage").intValue = 20;
            so.FindProperty("playableStageCount").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();

            AddModeChips(safe);

            var start = AddLocalizedButton(safe, "StartStageButton", "lobby_play_stage", new Vector2(0f, -612f), new Vector2(780f, 104f), Lemon, Ink);
            UnityEventTools.AddIntPersistentListener(start.onClick, router.LoadGameWithStage, 1);

            AddBottomNav(safe);
        }

        static void BuildGameShell(RectTransform safe)
        {
            AddLocalizedLabel(safe, "game_drag_hint", 24, FontStyles.UpperCase, new Color(1f, 1f, 1f, 0.38f), new Vector2(0f, -716f), new Vector2(820f, 44f));
        }

        static void AddResourceHeader(RectTransform parent)
        {
            var header = AddPanel(parent, "TopResourceBar", new Vector2(0f, -76f), new Vector2(940f, 96f), new Color(1f, 1f, 1f, 0.08f));
            SetTop(header);
            AddLocalizedLabel(header, "lobby_player", 28, FontStyles.Bold, White, new Vector2(-346f, 0f), new Vector2(210f, 54f), TextAlignmentOptions.Left);
            AddChip(header, "HeartChip", "5", new Vector2(108f, 0f), Coral);
            AddChip(header, "CoinChip", "1 240", new Vector2(270f, 0f), Lemon);
            AddChip(header, "GemChip", "32", new Vector2(430f, 0f), Aqua);
        }

        static void AddModeChips(RectTransform parent)
        {
            AddLocalizedChip(parent, "DailyChip", "lobby_daily", new Vector2(-270f, -255f), Aqua);
            AddLocalizedChip(parent, "EventChip", "lobby_event", new Vector2(0f, -255f), Coral);
            AddLocalizedChip(parent, "SeasonChip", "lobby_season", new Vector2(270f, -255f), Lemon);
        }

        static void AddBottomNav(RectTransform parent)
        {
            var nav = AddPanel(parent, "BottomNav", new Vector2(0f, 54f), new Vector2(900f, 116f), new Color(0.04f, 0.055f, 0.08f, 0.94f));
            SetBottom(nav);
            AddLocalizedLabel(nav, "nav_home", 22, FontStyles.Bold, Aqua, new Vector2(-315f, -16f), new Vector2(140f, 34f));
            AddLocalizedLabel(nav, "nav_stage", 22, FontStyles.Bold, White, new Vector2(-105f, -16f), new Vector2(140f, 34f));
            AddLocalizedLabel(nav, "nav_shop", 22, FontStyles.Bold, Muted, new Vector2(105f, -16f), new Vector2(140f, 34f));
            AddLocalizedLabel(nav, "nav_bag", 22, FontStyles.Bold, Muted, new Vector2(315f, -16f), new Vector2(140f, 34f));
            AddPanel(nav, "ActiveIndicator", new Vector2(-105f, 36f), new Vector2(64f, 6f), Aqua);
        }

        static RectTransform CreateStageNodeTemplate(RectTransform parent)
        {
            var node = AddPanel(parent, "StageNodeTemplate", new Vector2(-330f, -80f), new Vector2(96f, 96f), Aqua);
            var image = node.GetComponent<Image>();
            image.raycastTarget = true;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            AddLabel(node, "1", 38, FontStyles.Bold, Ink, Vector2.zero, new Vector2(80f, 70f)).gameObject.name = "StageLabel";
            node.gameObject.SetActive(false);
            return node;
        }

        static void AddStatusRow(RectTransform parent, float y, params string[] labels)
        {
            float startX = -(labels.Length - 1) * 142f;
            for (int i = 0; i < labels.Length; i++)
                AddLocalizedChip(parent, $"Status_{labels[i]}", labels[i], new Vector2(startX + i * 284f, y), i == 1 ? Lemon : Aqua);
        }

        static void AddFooterLinks(RectTransform parent)
        {
            AddLocalizedLabel(parent, "footer_notice", 24, FontStyles.Normal, Muted, new Vector2(-250f, -822f), new Vector2(160f, 44f));
            AddLocalizedLabel(parent, "footer_terms", 24, FontStyles.Normal, Muted, new Vector2(0f, -822f), new Vector2(160f, 44f));
            AddLocalizedLabel(parent, "footer_server", 24, FontStyles.Normal, Muted, new Vector2(250f, -822f), new Vector2(160f, 44f));
        }

        static void AddPreviewBoard(RectTransform parent, Vector2 position, float size, int cells)
        {
            var board = AddPanel(parent, "PuzzlePreviewBoard", position, new Vector2(size, size), new Color(1f, 1f, 1f, 0.07f));
            float cell = size / cells;
            for (int i = 1; i < cells; i++)
            {
                AddPanel(board, $"GridV_{i}", new Vector2(-size * 0.5f + cell * i, 0f), new Vector2(2f, size), new Color(1f, 1f, 1f, 0.08f));
                AddPanel(board, $"GridH_{i}", new Vector2(0f, -size * 0.5f + cell * i), new Vector2(size, 2f), new Color(1f, 1f, 1f, 0.08f));
            }

            AddPanel(board, "LinkA", new Vector2(-120f, 120f), new Vector2(250f, 16f), Aqua);
            AddPanel(board, "LinkB", new Vector2(100f, -120f), new Vector2(16f, 250f), Coral);
            AddPanel(board, "NodeA1", new Vector2(-230f, 120f), new Vector2(58f, 58f), Aqua);
            AddPanel(board, "NodeA2", new Vector2(5f, 120f), new Vector2(58f, 58f), Aqua);
            AddPanel(board, "NodeB1", new Vector2(100f, -235f), new Vector2(58f, 58f), Coral);
            AddPanel(board, "NodeB2", new Vector2(100f, 0f), new Vector2(58f, 58f), Coral);
        }

        static void AddGrid(RectTransform parent, Color color, float spacing)
        {
            for (float x = -Width * 0.5f; x <= Width * 0.5f; x += spacing)
                AddPanel(parent, $"GridV_{x:0}", new Vector2(x, 0f), new Vector2(2f, Height), color);

            for (float y = -Height * 0.5f; y <= Height * 0.5f; y += spacing)
                AddPanel(parent, $"GridH_{y:0}", new Vector2(0f, y), new Vector2(Width, 2f), color);
        }

        static void AddTopIcon(RectTransform parent, string name, string label, Vector2 position)
        {
            AddPanel(parent, name, position, new Vector2(76f, 76f), new Color(1f, 1f, 1f, 0.1f));
            AddLabel(parent, label, 28, FontStyles.Bold, White, position, new Vector2(76f, 76f));
        }

        static void AddLanguageSelector(RectTransform parent)
        {
            var rect = AddPanel(parent, "LanguageSelector", new Vector2(294f, -68f), new Vector2(220f, 76f), new Color(1f, 1f, 1f, 0.1f));
            SetTop(rect);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;

            var dropdown = rect.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.captionText = AddLabel(rect, "US", 28, FontStyles.Bold, White, new Vector2(-10f, 0f), new Vector2(140f, 58f));
            dropdown.captionText.raycastTarget = true;
            dropdown.template = CreateDropdownTemplate(rect);
            dropdown.gameObject.AddComponent<LanguageSelector>();
        }

        static RectTransform CreateDropdownTemplate(RectTransform parent)
        {
            var template = AddPanel(parent, "Template", new Vector2(0f, -124f), new Vector2(220f, 320f), new Color(0.04f, 0.055f, 0.08f, 0.98f));
            SetTop(template);
            template.gameObject.SetActive(false);

            var viewport = AddPanel(template, "Viewport", Vector2.zero, new Vector2(220f, 320f), new Color(0f, 0f, 0f, 0f));
            Stretch(viewport);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = AddPanel(viewport, "Content", new Vector2(0f, -160f), new Vector2(220f, 320f), new Color(0f, 0f, 0f, 0f));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            var item = AddPanel(content, "Item", new Vector2(0f, -32f), new Vector2(220f, 64f), new Color(1f, 1f, 1f, 0.08f));
            item.anchorMin = new Vector2(0f, 1f);
            item.anchorMax = new Vector2(1f, 1f);
            item.pivot = new Vector2(0.5f, 1f);
            item.GetComponent<Image>().raycastTarget = true;
            var toggle = item.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = item.GetComponent<Image>();
            toggle.graphic = AddPanel(item, "Item Checkmark", new Vector2(-82f, 0f), new Vector2(18f, 18f), Aqua).GetComponent<Image>();

            var label = AddLabel(item, "US", 24, FontStyles.Bold, White, new Vector2(20f, 0f), new Vector2(150f, 44f), TextAlignmentOptions.Left);
            label.gameObject.name = "Item Label";
            label.raycastTarget = true;

            var scrollRect = template.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = false;

            return template;
        }

        static Button AddButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, Color background, Color foreground)
        {
            var rect = AddPanel(parent, name, position, size, background);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.08f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.12f);
            colors.selectedColor = background;
            button.colors = colors;
            AddLabel(rect, label, 34, FontStyles.Bold, foreground, Vector2.zero, size);
            return button;
        }

        static Button AddLocalizedButton(RectTransform parent, string name, string stringId, Vector2 position, Vector2 size, Color background, Color foreground)
        {
            var rect = AddPanel(parent, name, position, size, background);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.08f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.12f);
            colors.selectedColor = background;
            button.colors = colors;
            AddLocalizedLabel(rect, stringId, 34, FontStyles.Bold, foreground, Vector2.zero, size);
            return button;
        }

        static RectTransform AddChip(RectTransform parent, string name, string label, Vector2 position, Color accent)
        {
            var chip = AddPanel(parent, name, position, new Vector2(220f, 72f), new Color(1f, 1f, 1f, 0.08f));
            AddPanel(chip, "Accent", new Vector2(-88f, 0f), new Vector2(18f, 44f), accent);
            AddLabel(chip, label, 24, FontStyles.Bold, White, new Vector2(24f, 0f), new Vector2(150f, 44f));
            return chip;
        }

        static RectTransform AddLocalizedChip(RectTransform parent, string name, string stringId, Vector2 position, Color accent)
        {
            var chip = AddPanel(parent, name, position, new Vector2(220f, 72f), new Color(1f, 1f, 1f, 0.08f));
            AddPanel(chip, "Accent", new Vector2(-88f, 0f), new Vector2(18f, 44f), accent);
            AddLocalizedLabel(chip, stringId, 24, FontStyles.Bold, White, new Vector2(24f, 0f), new Vector2(150f, 44f));
            return chip;
        }

        static RectTransform AddFullBleed(RectTransform parent, Color color)
        {
            var rect = AddPanel(parent, "Background", Vector2.zero, Vector2.zero, color);
            Stretch(rect);
            rect.SetAsFirstSibling();
            return rect;
        }

        static TextMeshProUGUI AddLabel(RectTransform parent, string text, float size, FontStyles style, Color color, Vector2 position, Vector2 rectSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = rectSize;

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        static TextMeshProUGUI AddLocalizedLabel(RectTransform parent, string stringId, float size, FontStyles style, Color color, Vector2 position, Vector2 rectSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = AddLabel(parent, stringId, size, style, color, position, rectSize, alignment);
            label.gameObject.AddComponent<LocalizedText>().SetStringId(stringId);
            return label;
        }

        static RectTransform AddPanel(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
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

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void SetTop(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }

        static void SetBottom(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
        }

        static void SetLeft(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
        }
    }
}
