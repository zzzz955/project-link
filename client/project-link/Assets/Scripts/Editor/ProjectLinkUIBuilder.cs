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
        const float RefW = 1080f;
        const float RefH = 1920f;
        const string PopupPrefabRoot = "Assets/Resources/Prefabs/UI";
        const string ResourceRoot = "Assets/Resources/UI";
        const string SkinAssetPath = "Assets/Editor/UIButtonSkin.asset";

        // Palette (scenes.json)
        static readonly Color BgPrimary   = HexColor("#1A1A2E");
        static readonly Color BgSurface   = HexColor("#16213E");
        static readonly Color AccentA     = HexColor("#7B2FBE");
        static readonly Color AccentB     = HexColor("#E040FB");
        static readonly Color Positive    = HexColor("#00E5FF");
        static readonly Color Lime        = HexColor("#76FF03");
        static readonly Color Warning     = HexColor("#FFB300");
        static readonly Color Danger      = HexColor("#FF5252");
        static readonly Color TextCol     = HexColor("#FFFFFF");
        static readonly Color TextMuted   = HexColor("#B0BEC5");
        static readonly Color TextDisabled= HexColor("#546E7A");
        static readonly Color Scrim       = HexColor("#000000B3");
        static readonly Color HudBg       = HexColor("#0E1430CC");
        static readonly Color SurfaceEE   = HexColor("#16213EEE");
        static readonly Color SlotPlaceholder = new(0.08f, 0.16f, 0.28f, 0.4f);

        static UIButtonSkin _skin;

        // ─── Menu entries ──────────────────────────────────────────────────

        [MenuItem("Tools/Project Link/UI Build/Create UI Button Skin")]
        public static void CreateUIButtonSkin()
        {
            EnsureFolder("Assets/Editor");
            if (AssetDatabase.LoadAssetAtPath<UIButtonSkin>(SkinAssetPath) != null)
            {
                Debug.Log($"UIButtonSkin already exists at {SkinAssetPath}");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UIButtonSkin>(SkinAssetPath);
                return;
            }
            var skin = ScriptableObject.CreateInstance<UIButtonSkin>();
            AssetDatabase.CreateAsset(skin, SkinAssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = skin;
        }

        [MenuItem("Tools/Project Link/UI Build/Build Current Scene UI")]
        public static void BuildCurrentSceneUI()
        {
            _skin = null;
            BuildScene(SceneManager.GetActiveScene().name);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Project Link/UI Build/Build All Scene UI")]
        public static void BuildAllSceneUI()
        {
            _skin = null;
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

        public static void BuildAllSceneUIBatch() => BuildAllSceneUI();

        [MenuItem("Tools/Project Link/UI Build/Build Popup Prefabs")]
        public static void BuildPopupPrefabs()
        {
            _skin = null;
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder(PopupPrefabRoot);

            // Confirm popups (simple 2-button)
            BuildConfirmPopupPrefab<ReturnTitlePopup>("ReturnTitlePopup",
                "popup.return_title.title", "popup.return_title.body");
            BuildConfirmPopupPrefab<ExitGamePopup>("ExitGamePopup",
                "popup.exit_game.title", "popup.exit_game.body");

            // Standard popups from popups.json
            BuildForceUpdatePopup();
            BuildMaintenancePopup();
            BuildSessionExpiredPopup();
            BuildSettingPopup();
            BuildEnergyPopup();
            BuildStageDetailPopup();
            BuildDailyChallengePopup();
            BuildRewardPopup();
            BuildAccountPopup();
            BuildClearPopup();
            BuildPausePopup();
            BuildTimeoutPopup();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Project Link/UI Build/Configure UI Texture Imports")]
        public static void ConfigureUiTextureImports()
        {
            ConfigureSpriteSheet($"{ResourceRoot}/AssetResource1.png", 4, 6, "AssetResource1");
            ConfigureSpriteSheet($"{ResourceRoot}/AssetResource2.png", 3, 5, "AssetResource2");
            ConfigureSpriteSheet($"{ResourceRoot}/AssetResource3.png", 3, 3, "AssetResource3");
            AssetDatabase.SaveAssets();
        }

        // ─── Scene dispatch ────────────────────────────────────────────────

        static void BuildScene(string sceneName)
        {
            EnsureEventSystem();
            DestroyExistingRoots(sceneName);

            var (canvas, safe) = CreateSceneCanvas(sceneName + "Canvas");
            var router = canvas.AddComponent<RuntimeNavigationButtons>();
            ConfigureEscapeHandler(canvas, sceneName, router);

            switch (sceneName)
            {
                case "Bootstrap": BuildBootstrap(safe, canvas); break;
                case "Title":     BuildTitle(safe, canvas, router); break;
                case "Lobby":     BuildLobby(safe, canvas, router); break;
                case "Game":      BuildGame(safe, canvas, router); break;
                default:
                    Debug.LogWarning($"No UI builder registered for scene '{sceneName}'.");
                    break;
            }
        }

        // ─── Bootstrap ────────────────────────────────────────────────────

        static void BuildBootstrap(RectTransform safe, GameObject canvasRoot)
        {
            // Slot_Logo — center placeholder (SpineGraphic at runtime)
            var logo = MakeSlot(safe, "Slot_Logo", new Vector2(0, 200), new Vector2(560, 560));

            // ProgressBar — bottom, Slider
            var bar = MakeChild(safe, "ProgressBar");
            SetAnchor(bar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            bar.sizeDelta = new Vector2(0, 24);
            bar.anchoredPosition = new Vector2(0, 240);
            bar.offsetMin = new Vector2(80, bar.offsetMin.y);
            bar.offsetMax = new Vector2(-80, bar.offsetMax.y);
            var barBg = bar.gameObject.AddComponent<Image>();
            barBg.color = HexColor("#FFFFFF22");
            ApplySlotSkin(barBg, "slot_progress_track");
            var fill = MakeChild(bar, "Fill");
            Stretch(fill, 2, 2, 2, 2);
            var fillImg = fill.gameObject.AddComponent<Image>();
            fillImg.color = AccentA;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0.4f;
            var slider = bar.gameObject.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 0.4f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.interactable = false;
            slider.fillRect = fill;
            slider.handleRect = null;

            // Btn_Retry — hidden by default
            var retry = MakeButton(safe, "Btn_Retry", "common.retry", new Vector2(400, 128), "btn_primary");
            retry.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
            Center(retry.transform.GetComponent<RectTransform>(), new Vector2(0, -120), new Vector2(400, 128));
            retry.gameObject.SetActive(false);

            // Txt_NetworkError — hidden by default
            var errText = MakeText(safe, "Txt_NetworkError", "bootstrap.network_error", 28, Danger, TextAlignmentOptions.Center);
            Center(errText.GetComponent<RectTransform>(), new Vector2(0, -40), new Vector2(800, 60));
            errText.gameObject.SetActive(false);

            // PopupLayer
            AddPopupLayer(canvasRoot, 100);

            // Controller refs
            var ctrl = safe.gameObject.AddComponent<BootstrapWireframeController>();
            Assign(ctrl, "progressFillImage", fillImg);
        }

        // ─── Title ────────────────────────────────────────────────────────

        static void BuildTitle(RectTransform safe, GameObject canvasRoot, RuntimeNavigationButtons router)
        {
            // Btn_Settings — top-right
            var btnSettings = MakeIconButton(safe, "Btn_Settings", "btn_icon_settings",
                new Vector2(96, 96), new Vector2(-32, -32), AnchorPreset.TopRight);
            UnityEventTools.AddPersistentListener(btnSettings.onClick, router.OpenSettingsPopup);

            // Slot_Logo — top-center
            MakeSlot(safe, "Slot_Logo", new Vector2(0, -360), new Vector2(720, 480),
                anchor: AnchorPreset.TopCenter);

            // Group_AuthButtons — middle-center VLG
            var authGroup = MakeChild(safe, "Group_AuthButtons");
            Center(authGroup, new Vector2(0, -40), new Vector2(720, 320));
            var vlg = authGroup.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 24; vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var btnGoogle = MakeAuthProviderButton(authGroup, "Btn_Google",
                "title.continue_google", "slot_provider_google", "btn_secondary");
            var btnApple = MakeAuthProviderButton(authGroup, "Btn_Apple",
                "title.continue_apple", "slot_provider_apple", "btn_secondary");

            // Btn_TapToStart — bottom-center
            var btnStart = MakeButton(safe, "Btn_TapToStart", "title.tap_to_start",
                new Vector2(640, 144), "btn_primary", fontSize: 36);
            Center(btnStart.transform.GetComponent<RectTransform>(), new Vector2(0, 320), new Vector2(640, 144));
            SetAnchor(btnStart.transform.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            btnStart.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 320);

            // Txt_Version — bottom-right
            var ver = MakeText(safe, "Txt_Version", "", 22, TextDisabled, TextAlignmentOptions.MidlineRight);
            ver.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 40);
            SetAnchor(ver.GetComponent<RectTransform>(),
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0));
            ver.GetComponent<RectTransform>().anchoredPosition = new Vector2(-32, 32);

            // PopupLayer
            AddPopupLayer(canvasRoot, 100);

            // Controller refs
            var ctrl = safe.gameObject.AddComponent<TitleWireframeController>();
            Assign(ctrl, "btnTapToStart", btnStart);
            Assign(ctrl, "btnGoogle", btnGoogle);
            Assign(ctrl, "btnApple", btnApple);
            Assign(ctrl, "txtVersion", ver.GetComponent<TextMeshProUGUI>());
        }

        static Button MakeAuthProviderButton(RectTransform parent, string name,
            string labelKey, string iconSkinKey, string bgSkinKey)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 128);
            var img = go.GetComponent<Image>();
            img.color = BgSurface;
            ApplyButtonSkin(img, bgSkinKey);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 128; le.flexibleWidth = 1;

            // Row_Row (HLG with icon + label)
            var row = MakeChild(rect, "Group_Row");
            Stretch(row, 32, 0, 32, 0);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;

            var icon = MakeChild(row, "Icon_Provider");
            icon.sizeDelta = new Vector2(56, 56);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.color = SlotPlaceholder;
            ApplySlotSkin(iconImg, iconSkinKey);
            var iconLE = icon.gameObject.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 56; iconLE.preferredHeight = 56; iconLE.flexibleWidth = 0;

            var lbl = MakeChild(row, "Txt_Label");
            lbl.sizeDelta = new Vector2(0, 40);
            var txt = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            txt.text = "";
            lbl.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);
            txt.fontSize = 28; txt.fontStyle = FontStyles.Bold;
            txt.color = TextCol; txt.alignment = TextAlignmentOptions.MidlineLeft;
            var lblLE = lbl.gameObject.AddComponent<LayoutElement>();
            lblLE.flexibleWidth = 1;

            return btn;
        }

        // ─── Lobby ────────────────────────────────────────────────────────

        static void BuildLobby(RectTransform safe, GameObject canvasRoot, RuntimeNavigationButtons router)
        {
            // HUD_Strip
            var hud = MakeChild(safe, "HUD_Strip");
            SetAnchor(hud, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            hud.sizeDelta = new Vector2(0, 240);
            hud.anchoredPosition = Vector2.zero;
            var hudImg = hud.gameObject.AddComponent<Image>();
            hudImg.color = HudBg;
            ApplySlotSkin(hudImg, "slot_hud_bg");
            var hudVlg = hud.gameObject.AddComponent<VerticalLayoutGroup>();
            hudVlg.spacing = 8;
            hudVlg.padding = new RectOffset(24, 24, 16, 16);
            hudVlg.childAlignment = TextAnchor.MiddleLeft;
            hudVlg.childControlWidth = true; hudVlg.childControlHeight = true;
            hudVlg.childForceExpandWidth = true; hudVlg.childForceExpandHeight = false;

            // Row_Profile
            var rowProfile = MakeChild(hud, "Row_Profile");
            rowProfile.sizeDelta = new Vector2(0, 100);
            var rpHlg = rowProfile.gameObject.AddComponent<HorizontalLayoutGroup>();
            rpHlg.spacing = 16; rpHlg.childAlignment = TextAnchor.MiddleLeft;
            rpHlg.childControlWidth = true; rpHlg.childControlHeight = true;
            rowProfile.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;

            var avatar = MakeChild(rowProfile, "Slot_Avatar");
            avatar.sizeDelta = new Vector2(88, 88);
            var avatarImg = avatar.gameObject.AddComponent<Image>();
            avatarImg.color = SlotPlaceholder;
            ApplySlotSkin(avatarImg, "slot_avatar");
            avatar.gameObject.AddComponent<Mask>();
            var avatarBtn = avatar.gameObject.AddComponent<Button>();
            avatarBtn.targetGraphic = avatarImg;
            UnityEventTools.AddPersistentListener(avatarBtn.onClick, router.OpenAccountPopup);
            var avatarLE = avatar.gameObject.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 88; avatarLE.preferredHeight = 88; avatarLE.flexibleWidth = 0;

            var nickText = MakeChild(rowProfile, "Txt_Nickname");
            nickText.sizeDelta = new Vector2(0, 40);
            var nickTmp = nickText.gameObject.AddComponent<TextMeshProUGUI>();
            nickTmp.text = "Player"; nickTmp.fontSize = 30;
            nickTmp.fontStyle = FontStyles.Bold; nickTmp.color = TextCol;
            nickTmp.alignment = TextAlignmentOptions.MidlineLeft;
            var nickLE = nickText.gameObject.AddComponent<LayoutElement>();
            nickLE.flexibleWidth = 1;

            var menuBtn = MakeChild(rowProfile, "Btn_Menu");
            menuBtn.sizeDelta = new Vector2(88, 88);
            var menuImg = menuBtn.gameObject.AddComponent<Image>();
            menuImg.color = HexColor("#FFFFFF26");
            ApplyButtonSkin(menuImg, "btn_icon_menu");
            var menuButton = menuBtn.gameObject.AddComponent<Button>();
            menuButton.targetGraphic = menuImg;
            UnityEventTools.AddPersistentListener(menuButton.onClick, router.OpenSettingsPopup);
            menuBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 88;

            // Row_Stats
            var rowStats = MakeChild(hud, "Row_Stats");
            rowStats.sizeDelta = new Vector2(0, 80);
            var rsHlg = rowStats.gameObject.AddComponent<HorizontalLayoutGroup>();
            rsHlg.spacing = 24; rsHlg.childAlignment = TextAnchor.MiddleLeft;
            rsHlg.childControlWidth = true; rsHlg.childControlHeight = true;
            rowStats.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;

            AddStaminaGroup(rowStats, router);
            AddCurrencyGroup(rowStats);

            // MenuDropdown (hidden)
            var dropdown = MakeChild(safe, "MenuDropdown");
            SetAnchor(dropdown, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            dropdown.sizeDelta = new Vector2(360, 200);
            dropdown.anchoredPosition = new Vector2(-32, -240);
            var ddImg = dropdown.gameObject.AddComponent<Image>();
            ddImg.color = SurfaceEE;
            ApplySlotSkin(ddImg, "slot_popup_bg");
            var ddVlg = dropdown.gameObject.AddComponent<VerticalLayoutGroup>();
            ddVlg.spacing = 4; ddVlg.padding = new RectOffset(12, 12, 12, 12);
            ddVlg.childControlWidth = true; ddVlg.childControlHeight = true;
            AddDropdownItem(dropdown, "Btn_Settings", "hud.settings", router.OpenSettingsPopup);
            AddDropdownItem(dropdown, "Btn_Language", "hud.language", router.OpenSettingsPopup);
            dropdown.gameObject.SetActive(false);

            // Group_TabBodies
            var tabBodies = MakeChild(safe, "Group_TabBodies");
            SetAnchor(tabBodies, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            tabBodies.offsetMin = new Vector2(0, 128);
            tabBodies.offsetMax = new Vector2(0, -240);

            var tabHome = BuildHomeTab(tabBodies, router);
            var tabShop = BuildShopTab(tabBodies);
            var tabRanking = BuildRankingTab(tabBodies);

            // TabBar
            var tabBar = MakeChild(safe, "TabBar");
            SetAnchor(tabBar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            tabBar.sizeDelta = new Vector2(0, 128);
            tabBar.anchoredPosition = Vector2.zero;
            var tbImg = tabBar.gameObject.AddComponent<Image>();
            tbImg.color = HudBg;
            var tbHlg = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tbHlg.spacing = 0; tbHlg.childAlignment = TextAnchor.MiddleCenter;
            tbHlg.childControlWidth = true; tbHlg.childControlHeight = true;
            tbHlg.childForceExpandWidth = true; tbHlg.childForceExpandHeight = true;

            var shopTabBtn = AddTabBarButton(tabBar, "Tab_Shop", "tab.shop",
                "btn_tab_shop", "evt.tab.goShop", false);
            var homeTabBtn = AddTabBarButton(tabBar, "Tab_Home", "tab.home",
                "btn_tab_home", "evt.tab.goHome", true);
            var rankTabBtn = AddTabBarButton(tabBar, "Tab_Ranking", "tab.ranking",
                "btn_tab_ranking", "evt.tab.goRanking", false);

            // Layers
            AddPopupLayer(canvasRoot, 100);
            AddToastLayer(canvasRoot, 200);

            // Controllers
            var lobbyCtrl = safe.gameObject.AddComponent<LobbyWireframeController>();
            Assign(lobbyCtrl, "playButton",           FindButtonInChildren(tabHome, "Btn_Play"));
            Assign(lobbyCtrl, "refillButton",         FindButtonInChildren(tabHome, "Btn_Refill"));
            Assign(lobbyCtrl, "profileNameText",      FindTmpInChildren(safe, "Txt_Nickname"));
            Assign(lobbyCtrl, "energyText",           FindTmpInChildren(safe, "Txt_StaminaCount"));
            Assign(lobbyCtrl, "coinText",             FindTmpInChildren(safe, "Txt_CurrencyCount"));
            Assign(lobbyCtrl, "stageNumberText",      FindTmpInChildren(tabHome, "Txt_StageNum"));
            Assign(lobbyCtrl, "starsText",            FindTmpInChildren(tabHome, "Txt_Stars"));
            Assign(lobbyCtrl, "dailyProgressText",    FindTmpInChildren(tabHome, "Txt_Frac"));
            Assign(lobbyCtrl, "colorCupTimerText",    FindTmpInChildren(tabHome, "Txt_Ends"));
            Assign(lobbyCtrl, "playDisabledReasonText", FindTmpInChildren(tabHome, "Txt_PlayDisabled"));
            Assign(lobbyCtrl, "shopBalanceText",      FindTmpInChildren(tabShop, "Txt_Balance"));
            Assign(lobbyCtrl, "rankingMetricText",    FindTmpInChildren(tabRanking, "Txt_Score"));
            Assign(lobbyCtrl, "rankingErrorText",     FindTmpInChildren(tabRanking, "Txt_RankError"));
            Assign(lobbyCtrl, "shopContent",          FindRectInChildren(tabShop, "Content"));
            Assign(lobbyCtrl, "rankingContent",       FindRectInChildren(tabRanking, "Content"));

            AddLobbyTabController(safe.gameObject,
                shopTabBtn, homeTabBtn, rankTabBtn,
                tabShop.gameObject, tabHome.gameObject, tabRanking.gameObject);
        }

        static void AddStaminaGroup(RectTransform parent, RuntimeNavigationButtons router)
        {
            var group = MakeChild(parent, "Group_Stamina");
            group.sizeDelta = new Vector2(340, 60);
            var hlg = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            var btn = group.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(btn.onClick, router.OpenEnergyPopup);
            group.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var icon = MakeChild(group, "Icon_Stamina");
            icon.sizeDelta = new Vector2(48, 48);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.color = SlotPlaceholder;
            ApplySlotSkin(iconImg, "slot_stamina_icon");
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 48;

            var count = MakeChild(group, "Txt_StaminaCount");
            count.sizeDelta = new Vector2(80, 40);
            var countTmp = count.gameObject.AddComponent<TextMeshProUGUI>();
            countTmp.text = "5/5"; countTmp.fontSize = 26; countTmp.color = TextCol;
            count.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;

            var timer = MakeChild(group, "Txt_StaminaTimer");
            timer.sizeDelta = new Vector2(140, 40);
            var timerTmp = timer.gameObject.AddComponent<TextMeshProUGUI>();
            timerTmp.text = ""; timerTmp.fontSize = 22; timerTmp.color = TextMuted;
            timer.gameObject.AddComponent<LayoutElement>().preferredWidth = 140;
        }

        static void AddCurrencyGroup(RectTransform parent)
        {
            var group = MakeChild(parent, "Group_Currency");
            group.sizeDelta = new Vector2(200, 60);
            var hlg = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.childAlignment = TextAnchor.MiddleRight;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            group.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;

            var icon = MakeChild(group, "Icon_Currency");
            icon.sizeDelta = new Vector2(48, 48);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.color = SlotPlaceholder;
            ApplySlotSkin(iconImg, "slot_currency_soft");
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 48;

            var count = MakeChild(group, "Txt_CurrencyCount");
            count.sizeDelta = new Vector2(120, 40);
            var countTmp = count.gameObject.AddComponent<TextMeshProUGUI>();
            countTmp.text = "0"; countTmp.fontSize = 26; countTmp.color = TextCol;
            countTmp.alignment = TextAlignmentOptions.MidlineRight;
            count.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;
        }

        static void AddDropdownItem(RectTransform parent, string name, string labelKey,
            UnityEngine.Events.UnityAction onClick)
        {
            var item = MakeChild(parent, name);
            item.sizeDelta = new Vector2(0, 80);
            var img = item.gameObject.AddComponent<Image>();
            img.color = HexColor("#FFFFFF11");
            var btn = item.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            UnityEventTools.AddPersistentListener(btn.onClick, onClick);
            item.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            // TMP must be on a child — a GO cannot have both Image and TextMeshProUGUI (both are Graphic)
            var lbl = MakeChild(item, "Lbl");
            Stretch(lbl, 24, 0, 24, 0);
            var txt = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 26; txt.color = TextCol;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            txt.raycastTarget = false;
            lbl.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);
        }

        static RectTransform BuildHomeTab(RectTransform parent, RuntimeNavigationButtons router)
        {
            var tab = MakeChild(parent, "Tab_Home");
            Stretch(tab);
            tab.gameObject.AddComponent<CanvasGroup>();

            // Carousel_Stages
            var carousel = MakeChild(tab, "Carousel_Stages");
            SetAnchor(carousel, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            carousel.sizeDelta = new Vector2(0, 880);
            carousel.anchoredPosition = new Vector2(0, -32);

            var carouselBg = carousel.gameObject.AddComponent<Image>();
            carouselBg.color = new Color(0, 0, 0, 0);

            var prevBtn = MakeIconButton(carousel, "Btn_Prev", "btn_carousel_prev",
                new Vector2(96, 96), new Vector2(16, 0), AnchorPreset.MiddleLeft);
            var nextBtn = MakeIconButton(carousel, "Btn_Next", "btn_carousel_next",
                new Vector2(96, 96), new Vector2(-16, 0), AnchorPreset.MiddleRight);

            // Group_Track (stage node area)
            var track = MakeChild(carousel, "Group_Track");
            SetAnchor(track, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            track.offsetMin = new Vector2(120, 80);
            track.offsetMax = new Vector2(-120, 0);
            var trackHlg = track.gameObject.AddComponent<HorizontalLayoutGroup>();
            trackHlg.spacing = 40; trackHlg.childAlignment = TextAnchor.MiddleCenter;

            // Stage node placeholder
            var node = MakeChild(track, "StageNode_Center");
            node.sizeDelta = new Vector2(400, 560);
            var nodeImg = node.gameObject.AddComponent<Image>();
            nodeImg.color = BgSurface;
            ApplySlotSkin(nodeImg, "slot_stage_node");
            var stageNum = MakeChild(node, "Txt_StageNum");
            Center(stageNum, new Vector2(0, 210), new Vector2(120, 56));
            var numTmp = stageNum.gameObject.AddComponent<TextMeshProUGUI>();
            numTmp.text = "1"; numTmp.fontSize = 36; numTmp.fontStyle = FontStyles.Bold;
            numTmp.color = TextCol; numTmp.alignment = TextAlignmentOptions.Center;

            var starsLbl = MakeChild(node, "Txt_Stars");
            Center(starsLbl, new Vector2(0, 150), new Vector2(180, 40));
            var starsTmp = starsLbl.gameObject.AddComponent<TextMeshProUGUI>();
            starsTmp.text = "0"; starsTmp.fontSize = 26; starsTmp.color = TextMuted;
            starsTmp.alignment = TextAlignmentOptions.Center;

            // Btn_Play
            var play = MakeButton(carousel, "Btn_Play", "home.stage_play",
                new Vector2(400, 112), "btn_primary", fontSize: 32);
            SetAnchor(play.GetComponent<RectTransform>(),
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            play.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 16);
            UnityEventTools.AddPersistentListener(play.onClick, router.LoadGame);

            var playDisabled = MakeChild(carousel, "Txt_PlayDisabled");
            SetAnchor(playDisabled, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            playDisabled.sizeDelta = new Vector2(400, 40);
            playDisabled.anchoredPosition = new Vector2(0, 140);
            var pdTmp = playDisabled.gameObject.AddComponent<TextMeshProUGUI>();
            pdTmp.fontSize = 22; pdTmp.color = Danger;
            pdTmp.alignment = TextAlignmentOptions.Center;
            playDisabled.gameObject.SetActive(false);

            // Card_Daily
            var cardDaily = MakeChild(tab, "Card_Daily");
            SetAnchor(cardDaily, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            cardDaily.offsetMin = new Vector2(32, 280);
            cardDaily.offsetMax = new Vector2(-32, 480);
            var dailyImg = cardDaily.gameObject.AddComponent<Image>();
            dailyImg.color = BgSurface;
            ApplySlotSkin(dailyImg, "slot_daily_card");
            var dailyBtn = cardDaily.gameObject.AddComponent<Button>();
            dailyBtn.targetGraphic = dailyImg;
            UnityEventTools.AddPersistentListener(dailyBtn.onClick, router.OpenDailyChallengePopup);
            var dailyVlg = cardDaily.gameObject.AddComponent<VerticalLayoutGroup>();
            dailyVlg.spacing = 8; dailyVlg.padding = new RectOffset(24, 24, 16, 16);
            dailyVlg.childControlWidth = true; dailyVlg.childControlHeight = true;

            var dailyTitle = MakeChild(cardDaily, "Txt_Title");
            dailyTitle.sizeDelta = new Vector2(0, 40);
            var dtTmp = dailyTitle.gameObject.AddComponent<TextMeshProUGUI>();
            dtTmp.fontSize = 26; dtTmp.fontStyle = FontStyles.Bold; dtTmp.color = TextCol;
            dailyTitle.gameObject.AddComponent<LocalizedText>().SetStringId("home.daily_title");
            dailyTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;

            var dailyFrac = MakeChild(cardDaily, "Txt_Frac");
            dailyFrac.sizeDelta = new Vector2(0, 32);
            var dfTmp = dailyFrac.gameObject.AddComponent<TextMeshProUGUI>();
            dfTmp.text = "0/5"; dfTmp.fontSize = 22; dfTmp.color = TextCol;
            dfTmp.alignment = TextAlignmentOptions.MidlineRight;
            dailyFrac.gameObject.AddComponent<LayoutElement>().preferredHeight = 32;

            // Card_Event (hidden by default)
            var cardEvent = MakeChild(tab, "Card_Event");
            SetAnchor(cardEvent, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            cardEvent.offsetMin = new Vector2(32, 32);
            cardEvent.offsetMax = new Vector2(-32, 252);
            var evtImg = cardEvent.gameObject.AddComponent<Image>();
            evtImg.color = BgSurface;
            ApplySlotSkin(evtImg, "slot_event_banner");
            var evtBtn = cardEvent.gameObject.AddComponent<Button>();
            evtBtn.targetGraphic = evtImg;
            var evtTitle = MakeChild(cardEvent, "Txt_Title");
            Center(evtTitle, new Vector2(0, 20), new Vector2(600, 40));
            var etTmp = evtTitle.gameObject.AddComponent<TextMeshProUGUI>();
            etTmp.text = "Event"; etTmp.fontSize = 28; etTmp.fontStyle = FontStyles.Bold; etTmp.color = TextCol;

            var evtEnds = MakeChild(cardEvent, "Txt_Ends");
            Center(evtEnds, new Vector2(0, -20), new Vector2(600, 32));
            var endsTmp = evtEnds.gameObject.AddComponent<TextMeshProUGUI>();
            endsTmp.text = ""; endsTmp.fontSize = 22; endsTmp.color = TextMuted;
            endsTmp.alignment = TextAlignmentOptions.Center;
            cardEvent.gameObject.SetActive(false);

            return tab;
        }

        static RectTransform BuildShopTab(RectTransform parent)
        {
            var tab = MakeChild(parent, "Tab_Shop");
            Stretch(tab);
            tab.gameObject.SetActive(false);

            var scroll = tab.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            var viewport = MakeChild(tab, "Viewport");
            Stretch(viewport, 16, 16, 16, 16);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = MakeChild(viewport, "Content");
            SetAnchor(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var contentVlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 24; contentVlg.padding = new RectOffset(8, 8, 8, 8);
            contentVlg.childControlWidth = true; contentVlg.childControlHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var balanceRow = MakeChild(content, "Row_Balance");
            balanceRow.sizeDelta = new Vector2(0, 56);
            var balHlg = balanceRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            balHlg.spacing = 8; balHlg.padding = new RectOffset(8, 8, 8, 8);
            balHlg.childAlignment = TextAnchor.MiddleRight;
            balHlg.childControlWidth = true; balHlg.childControlHeight = true;
            balanceRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var balLbl = MakeChild(balanceRow, "Lbl_Balance");
            balLbl.sizeDelta = new Vector2(0, 32);
            var balLblTmp = balLbl.gameObject.AddComponent<TextMeshProUGUI>();
            balLblTmp.fontSize = 22; balLblTmp.color = TextMuted;
            balLbl.gameObject.AddComponent<LocalizedText>().SetStringId("shop.balance_label");
            balLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var balTxt = MakeChild(balanceRow, "Txt_Balance");
            balTxt.sizeDelta = new Vector2(160, 32);
            var balTmp = balTxt.gameObject.AddComponent<TextMeshProUGUI>();
            balTmp.text = "0"; balTmp.fontSize = 26; balTmp.fontStyle = FontStyles.Bold;
            balTmp.color = TextCol; balTmp.alignment = TextAlignmentOptions.MidlineRight;
            balTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;

            var header = MakeChild(content, "Header_Stamina");
            header.sizeDelta = new Vector2(0, 48);
            var hTmp = header.gameObject.AddComponent<TextMeshProUGUI>();
            hTmp.fontSize = 22; hTmp.color = TextMuted; hTmp.fontStyle = FontStyles.Bold;
            header.gameObject.AddComponent<LocalizedText>().SetStringId("shop.section_stamina");
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            return tab;
        }

        static RectTransform BuildRankingTab(RectTransform parent)
        {
            var tab = MakeChild(parent, "Tab_Ranking");
            Stretch(tab);
            tab.gameObject.SetActive(false);

            // Seg_Mode
            var seg = MakeChild(tab, "Seg_Mode");
            SetAnchor(seg, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            seg.sizeDelta = new Vector2(0, 80);
            seg.offsetMin = new Vector2(80, seg.offsetMin.y);
            seg.offsetMax = new Vector2(-80, seg.offsetMax.y);
            seg.gameObject.AddComponent<Image>().color = HexColor("#FFFFFF11");
            var segHlg = seg.gameObject.AddComponent<HorizontalLayoutGroup>();
            segHlg.spacing = 0; segHlg.childAlignment = TextAnchor.MiddleCenter;
            segHlg.childControlWidth = true; segHlg.childControlHeight = true;
            segHlg.childForceExpandWidth = true; segHlg.childForceExpandHeight = true;

            AddSegmentItem(seg, "Seg_Clear", "rank.tab_clear");
            AddSegmentItem(seg, "Seg_Score", "rank.tab_score");

            var rankError = MakeChild(tab, "Txt_RankError");
            SetAnchor(rankError, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            rankError.sizeDelta = new Vector2(600, 48);
            rankError.anchoredPosition = new Vector2(0, -96);
            var reTmp = rankError.gameObject.AddComponent<TextMeshProUGUI>();
            reTmp.text = ""; reTmp.fontSize = 22; reTmp.color = Danger;
            reTmp.alignment = TextAlignmentOptions.Center;
            rankError.gameObject.SetActive(false);

            // ScrollList
            var scrollList = MakeChild(tab, "ScrollList");
            SetAnchor(scrollList, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            scrollList.offsetMin = new Vector2(16, 120);
            scrollList.offsetMax = new Vector2(-16, -96);
            var scrollRect = scrollList.gameObject.AddComponent<ScrollRect>();
            scrollRect.vertical = true; scrollRect.horizontal = false;

            var viewport = MakeChild(scrollList, "Viewport");
            Stretch(viewport);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = MakeChild(viewport, "Content");
            SetAnchor(content, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var contentVlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 8; contentVlg.childControlWidth = true; contentVlg.childControlHeight = true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = content;

            // Row_MyRank_Pinned
            var myRank = MakeChild(tab, "Row_MyRank_Pinned");
            SetAnchor(myRank, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            myRank.sizeDelta = new Vector2(0, 96);
            myRank.offsetMin = new Vector2(16, 16);
            myRank.offsetMax = new Vector2(-16, 112);
            var mrImg = myRank.gameObject.AddComponent<Image>();
            mrImg.color = HexColor("#7B2FBE40");
            var mrHlg = myRank.gameObject.AddComponent<HorizontalLayoutGroup>();
            mrHlg.spacing = 16; mrHlg.padding = new RectOffset(16, 16, 8, 8);
            mrHlg.childAlignment = TextAnchor.MiddleLeft;
            mrHlg.childControlWidth = true; mrHlg.childControlHeight = true;

            var rankTxt = MakeChild(myRank, "Txt_Rank");
            rankTxt.sizeDelta = new Vector2(64, 48);
            var rtTmp = rankTxt.gameObject.AddComponent<TextMeshProUGUI>();
            rtTmp.text = "#--"; rtTmp.fontSize = 28; rtTmp.fontStyle = FontStyles.Bold; rtTmp.color = TextCol;
            rankTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 64;

            var youTxt = MakeChild(myRank, "Txt_You");
            youTxt.sizeDelta = new Vector2(0, 48);
            var ytTmp = youTxt.gameObject.AddComponent<TextMeshProUGUI>();
            ytTmp.fontSize = 26; ytTmp.fontStyle = FontStyles.Bold; ytTmp.color = AccentB;
            ytTmp.alignment = TextAlignmentOptions.MidlineLeft;
            youTxt.gameObject.AddComponent<LocalizedText>().SetStringId("rank.you");
            var youLE = youTxt.gameObject.AddComponent<LayoutElement>();
            youLE.flexibleWidth = 1;

            var scoreTxt = MakeChild(myRank, "Txt_Score");
            scoreTxt.sizeDelta = new Vector2(140, 48);
            var stTmp = scoreTxt.gameObject.AddComponent<TextMeshProUGUI>();
            stTmp.text = "0"; stTmp.fontSize = 28; stTmp.color = TextCol;
            stTmp.alignment = TextAlignmentOptions.MidlineRight;
            scoreTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 140;

            return tab;
        }

        static void AddSegmentItem(RectTransform parent, string name, string labelKey)
        {
            var go = MakeChild(parent, name);
            Stretch(go);
            var img = go.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            go.gameObject.AddComponent<Button>().targetGraphic = img;
            // TMP must be on a child — Image and TextMeshProUGUI cannot share a GO
            var lbl = MakeChild(go, "Lbl");
            Stretch(lbl);
            var txt = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 24; txt.color = TextCol; txt.alignment = TextAlignmentOptions.Center;
            txt.raycastTarget = false;
            lbl.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);
        }

        static Button AddTabBarButton(RectTransform parent, string name,
            string labelKey, string iconSkinKey, string evtId, bool isDefault)
        {
            var go = MakeChild(parent, name);
            Stretch(go);
            var img = go.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);
            var btn = go.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var vlg = go.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            go.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var icon = MakeChild(go, "Icon");
            SetAnchor(icon, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            icon.sizeDelta = new Vector2(56, 56);
            icon.anchoredPosition = new Vector2(0, -20);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.color = isDefault ? TextCol : TextMuted;
            ApplyButtonSkin(iconImg, iconSkinKey);
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;

            var txt = MakeChild(go, "Txt");
            SetAnchor(txt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            txt.sizeDelta = new Vector2(120, 32);
            txt.anchoredPosition = new Vector2(0, 16);
            var tmpTxt = txt.gameObject.AddComponent<TextMeshProUGUI>();
            tmpTxt.fontSize = 22; tmpTxt.color = isDefault ? TextCol : TextMuted;
            tmpTxt.alignment = TextAlignmentOptions.Center;
            txt.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);

            var indicator = MakeChild(go, "Indicator");
            SetAnchor(indicator, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            indicator.sizeDelta = new Vector2(56, 8);
            indicator.anchoredPosition = new Vector2(0, 4);
            var indImg = indicator.gameObject.AddComponent<Image>();
            indImg.color = AccentB;
            ApplySlotSkin(indImg, "slot_tab_indicator");
            if (!isDefault) indicator.gameObject.SetActive(false);

            return btn;
        }

        // ─── Game ─────────────────────────────────────────────────────────

        static void BuildGame(RectTransform safe, GameObject canvasRoot, RuntimeNavigationButtons router)
        {
            // HUD_Top
            var hud = MakeChild(safe, "HUD_Top");
            SetAnchor(hud, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            hud.sizeDelta = new Vector2(0, 180);
            var hudImg = hud.gameObject.AddComponent<Image>();
            hudImg.color = HudBg;
            var hudVlg = hud.gameObject.AddComponent<VerticalLayoutGroup>();
            hudVlg.spacing = 12; hudVlg.padding = new RectOffset(24, 24, 16, 16);
            hudVlg.childControlWidth = true; hudVlg.childControlHeight = true;
            hudVlg.childForceExpandWidth = true; hudVlg.childForceExpandHeight = false;

            // Row_TopBar
            var topBar = MakeChild(hud, "Row_TopBar");
            topBar.sizeDelta = new Vector2(0, 80);
            var tbHlg = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tbHlg.spacing = 16; tbHlg.childAlignment = TextAnchor.MiddleLeft;
            tbHlg.childControlWidth = true; tbHlg.childControlHeight = true;
            topBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;

            var pauseBtn = MakeChild(topBar, "Btn_Pause");
            pauseBtn.sizeDelta = new Vector2(80, 80);
            var pauseImg = pauseBtn.gameObject.AddComponent<Image>();
            pauseImg.color = HexColor("#FFFFFF26");
            ApplyButtonSkin(pauseImg, "btn_icon_pause");
            var pauseButton = pauseBtn.gameObject.AddComponent<Button>();
            pauseButton.targetGraphic = pauseImg;
            UnityEventTools.AddPersistentListener(pauseButton.onClick, router.OpenPausePopup);
            var pauseLE = pauseBtn.gameObject.AddComponent<LayoutElement>();
            pauseLE.preferredWidth = 80; pauseLE.preferredHeight = 80; pauseLE.flexibleWidth = 0;

            var stageLbl = MakeChild(topBar, "Txt_Stage");
            stageLbl.sizeDelta = new Vector2(0, 60);
            var stTmp = stageLbl.gameObject.AddComponent<TextMeshProUGUI>();
            stTmp.text = "Stage 1"; stTmp.fontSize = 30; stTmp.fontStyle = FontStyles.Bold;
            stTmp.color = TextCol; stTmp.alignment = TextAlignmentOptions.Center;
            stageLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var timer = MakeChild(topBar, "Txt_Timer");
            timer.sizeDelta = new Vector2(160, 60);
            var timerTmp = timer.gameObject.AddComponent<TextMeshProUGUI>();
            timerTmp.text = ""; timerTmp.fontSize = 32; timerTmp.color = TextCol;
            timerTmp.alignment = TextAlignmentOptions.MidlineRight;
            timer.gameObject.AddComponent<LayoutElement>().preferredWidth = 160;

            // Row_Objectives
            var objRow = MakeChild(hud, "Row_Objectives");
            objRow.sizeDelta = new Vector2(0, 56);
            var objHlg = objRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            objHlg.spacing = 16; objHlg.childAlignment = TextAnchor.MiddleLeft;
            objHlg.childControlWidth = true; objHlg.childControlHeight = true;
            objRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;

            var objBar = MakeChild(objRow, "Bar_Objective");
            objBar.sizeDelta = new Vector2(0, 28);
            var objBarImg = objBar.gameObject.AddComponent<Image>();
            objBarImg.color = HexColor("#FFFFFF22");
            var objBarLE = objBar.gameObject.AddComponent<LayoutElement>();
            objBarLE.preferredHeight = 28; objBarLE.flexibleWidth = 1;

            var moveTxt = MakeChild(objRow, "Txt_Moves");
            moveTxt.sizeDelta = new Vector2(200, 40);
            var moveTmp = moveTxt.gameObject.AddComponent<TextMeshProUGUI>();
            moveTmp.text = ""; moveTmp.fontSize = 24; moveTmp.color = TextCol;
            moveTmp.alignment = TextAlignmentOptions.MidlineRight;
            moveTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;

            // Toolbar_Items
            var toolbar = MakeChild(safe, "Toolbar_Items");
            SetAnchor(toolbar, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            toolbar.sizeDelta = new Vector2(0, 140);
            toolbar.offsetMin = new Vector2(40, 40);
            toolbar.offsetMax = new Vector2(-40, 180);
            var toolHlg = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            toolHlg.spacing = 24; toolHlg.childAlignment = TextAnchor.MiddleCenter;
            toolHlg.childControlWidth = true; toolHlg.childControlHeight = true;
            toolHlg.childForceExpandWidth = true; toolHlg.childForceExpandHeight = true;

            for (int i = 0; i < 3; i++)
            {
                var slot = MakeChild(toolbar, $"ItemSlot_{i + 1}");
                slot.sizeDelta = new Vector2(0, 140);
                var slotImg = slot.gameObject.AddComponent<Image>();
                slotImg.color = HexColor("#16213EE0");
                var slotBtn = slot.gameObject.AddComponent<Button>();
                slotBtn.targetGraphic = slotImg;
                UnityEventTools.AddPersistentListener(slotBtn.onClick, router.OpenBuyItemPopup);
                slot.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            }

            // Layers
            AddPopupLayer(canvasRoot, 100);
            AddToastLayer(canvasRoot, 200);

            // Controller refs
            var ctrl = safe.gameObject.AddComponent<GameWireframeController>();
            Assign(ctrl, "levelLabelText", stTmp);
            Assign(ctrl, "moveCounterText", moveTmp);
        }

        // ─── Popup prefab builders ─────────────────────────────────────────

        static void BuildForceUpdatePopup()
        {
            var (root, panel, _, footer) = CreatePopupShell<ForceUpdatePopup>(
                "ForceUpdatePopup", "popup.force_update.title", dismissible: false);
            AddPopupIcon(panel, Warning);
            AddPopupBodyText(panel, "popup.force_update.body");
            var storeBtn = AddFooterButton(footer, "Btn_OpenStore", "popup.force_update.cta",
                "btn_primary", isPrimary: true);
            var popup = root.GetComponent<ForceUpdatePopup>();
            Assign(popup, "btnOpenStore", storeBtn);
            SavePopupPrefab(root, "ForceUpdatePopup");
        }

        static void BuildMaintenancePopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<MaintenancePopup>(
                "MaintenancePopup", "popup.maintenance.title", dismissible: false);
            AddPopupIcon(panel, Warning);
            var body = AddPopupBodyText(panel, "popup.maintenance.body");
            var popup = root.GetComponent<MaintenancePopup>();
            Assign(popup, "txtBody", body);
            SavePopupPrefab(root, "MaintenancePopup");
        }

        static void BuildSessionExpiredPopup()
        {
            var (root, panel, _, footer) = CreatePopupShell<SessionExpiredPopup>(
                "SessionExpiredPopup", "popup.session.title", dismissible: false);
            AddPopupIcon(panel, Danger);
            AddPopupBodyText(panel, "popup.session.body");
            AddFooterButton(footer, "Btn_Confirm", "common.confirm", "btn_primary", isPrimary: true);
            SavePopupPrefab(root, "SessionExpiredPopup");
        }

        static void BuildSettingPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<SettingPopup>(
                "SettingPopup", "popup.settings.title", dismissible: true);
            AddToggleRow(content, "Row_Bgm", "popup.settings.bgm");
            AddToggleRow(content, "Row_Sfx", "popup.settings.sfx");
            AddToggleRow(content, "Row_Haptics", "popup.settings.haptics");
            AddToggleRow(content, "Row_Notif", "popup.settings.notif");
            AddDropdownRow(content, "Row_Language", "hud.language");
            AddFooterButton(footer, "Btn_Cancel", "common.cancel", "btn_secondary");
            AddFooterButton(footer, "Btn_Save", "common.save", "btn_primary", isPrimary: true);
            var popup = root.GetComponent<SettingPopup>();
            Assign(popup, "closeIconButton", FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "closeButton", FindButtonInChildren(root, "Btn_Cancel"));
            Assign(popup, "saveButton", FindButtonInChildren(root, "Btn_Save"));
            SavePopupPrefab(root, "SettingPopup");
        }

        static void BuildEnergyPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<EnergyPopup>(
                "EnergyPopup", "popup.energy.title", dismissible: true);
            // Hearts row placeholder
            var hearts = MakeChild(content, "Group_Hearts");
            hearts.sizeDelta = new Vector2(0, 100);
            hearts.gameObject.AddComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            hearts.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
            // Full-in timer
            var timerTxt = MakeText(content, "Txt_FullIn", "popup.energy.full_in_fmt",
                24, TextMuted, TextAlignmentOptions.Center);
            timerTxt.AddComponent<LayoutElement>().preferredHeight = 48;

            AddFooterButton(footer, "Btn_WatchAd", "popup.energy.watch_ad_fmt", "btn_primary", isPrimary: true);
            AddFooterButton(footer, "Btn_Refill", "popup.energy.refill_fmt", "btn_secondary");
            var popup = root.GetComponent<EnergyPopup>();
            Assign(popup, "closeIconButton", FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "watchAdButton", FindButtonInChildren(root, "Btn_WatchAd"));
            Assign(popup, "refillButton", FindButtonInChildren(root, "Btn_Refill"));
            SavePopupPrefab(root, "EnergyPopup");
        }

        static void BuildStageDetailPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<StageDetailPopup>(
                "StageDetailPopup", "popup.stage.title_fmt", dismissible: true);
            var starRow = MakeChild(content, "Group_Stars");
            starRow.sizeDelta = new Vector2(0, 80);
            starRow.gameObject.AddComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            starRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var best = MakeText(content, "Txt_Best", "popup.stage.best_fmt",
                26, TextMuted, TextAlignmentOptions.Center);
            best.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var rank = MakeText(content, "Txt_MyRank", "popup.stage.my_rank_fmt",
                26, TextMuted, TextAlignmentOptions.Center);
            rank.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            AddFooterButton(footer, "Btn_Play", "common.play", "btn_primary", isPrimary: true);
            var popup = root.GetComponent<StageDetailPopup>();
            Assign(popup, "btnClose", FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "btnPlay", FindButtonInChildren(root, "Btn_Play"));
            Assign(popup, "txtBest", best.GetComponent<TextMeshProUGUI>());
            Assign(popup, "txtMyRank", rank.GetComponent<TextMeshProUGUI>());
            SavePopupPrefab(root, "StageDetailPopup");
        }

        static void BuildDailyChallengePopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<DailyChallengePopup>(
                "DailyChallengePopup", "popup.daily.title", dismissible: true);
            var streakTxt = MakeText(content, "Txt_Streak", "home.daily_streak_fmt",
                28, Warning, TextAlignmentOptions.Center);
            streakTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var tileRow = MakeChild(content, "Row_Streak_Tiles");
            tileRow.sizeDelta = new Vector2(0, 96);
            tileRow.gameObject.AddComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            tileRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            var todayTxt = MakeText(content, "Txt_Today", "popup.daily.today_fmt",
                26, TextCol, TextAlignmentOptions.Center);
            todayTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var progressBar = MakeChild(content, "Bar_Progress");
            progressBar.sizeDelta = new Vector2(0, 24);
            progressBar.gameObject.AddComponent<Image>().color = HexColor("#FFFFFF22");
            progressBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 24;
            var rewardTxt = MakeText(content, "Txt_Reward", "popup.daily.reward_fmt",
                26, Positive, TextAlignmentOptions.Center);
            rewardTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            AddFooterButton(footer, "Btn_Complete", "common.complete", "btn_primary", isPrimary: true);
            var popup = root.GetComponent<DailyChallengePopup>();
            Assign(popup, "closeButton", FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "playButton", FindButtonInChildren(root, "Btn_Complete"));
            Assign(popup, "progressText", todayTxt.GetComponent<TextMeshProUGUI>());
            Assign(popup, "rewardText", rewardTxt.GetComponent<TextMeshProUGUI>());
            Assign(popup, "streakRow", tileRow);
            SavePopupPrefab(root, "DailyChallengePopup");
        }

        static void BuildRewardPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<RewardPopup>(
                "RewardPopup", "popup.reward.title", dismissible: true);
            var spine = MakeChild(content, "Slot_Spine");
            spine.sizeDelta = new Vector2(400, 400);
            spine.gameObject.AddComponent<Image>().color = SlotPlaceholder;
            spine.gameObject.AddComponent<LayoutElement>().preferredHeight = 400;
            var currencyRow = MakeChild(content, "Row_Currency");
            currencyRow.sizeDelta = new Vector2(0, 80);
            currencyRow.gameObject.AddComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            currencyRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var amtTxt = MakeChild(currencyRow, "Txt_Amount");
            amtTxt.sizeDelta = new Vector2(200, 80);
            var atTmp = amtTxt.gameObject.AddComponent<TextMeshProUGUI>();
            atTmp.text = "×0"; atTmp.fontSize = 48; atTmp.fontStyle = FontStyles.Bold; atTmp.color = TextCol;
            amtTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
            AddFooterButton(footer, "Btn_Claim", "common.claim", "btn_primary", isPrimary: true);
            AddFooterButton(footer, "Btn_ClaimX2", "popup.reward.x2", "btn_secondary");
            var popup = root.GetComponent<RewardPopup>();
            Assign(popup, "closeButton", FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "claimButton", FindButtonInChildren(root, "Btn_Claim"));
            Assign(popup, "watchAdButton", FindButtonInChildren(root, "Btn_ClaimX2"));
            Assign(popup, "rewardAmountText", atTmp);
            SavePopupPrefab(root, "RewardPopup");
        }

        static void BuildAccountPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<AccountPopup>(
                "AccountPopup", "popup.account.title", dismissible: true);
            var avatarSlot = MakeChild(content, "Slot_Avatar");
            avatarSlot.sizeDelta = new Vector2(160, 160);
            avatarSlot.gameObject.AddComponent<Image>().color = SlotPlaceholder;
            avatarSlot.gameObject.AddComponent<Mask>();
            avatarSlot.gameObject.AddComponent<LayoutElement>().preferredHeight = 160;
            var nickTxt = MakeText(content, "Txt_Nickname", "",
                32, TextCol, TextAlignmentOptions.Center);
            nickTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var joinedTxt = MakeText(content, "Txt_Joined", "popup.account.joined_fmt",
                22, TextMuted, TextAlignmentOptions.Center);
            joinedTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;
            AddProviderRow(content, "Row_ProviderA", "slot_provider_google", "Continue with Google");
            AddProviderRow(content, "Row_ProviderB", "slot_provider_apple", "Continue with Apple");
            var popup = root.GetComponent<AccountPopup>();
            Assign(popup, "closeButton", FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "displayNameText", nickTxt.GetComponent<TextMeshProUGUI>());
            SavePopupPrefab(root, "AccountPopup");
        }

        static void BuildClearPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<ClearPopup>(
                "ClearPopup", "popup.clear.title", dismissible: true);
            var spine = MakeChild(content, "Slot_Spine");
            spine.sizeDelta = new Vector2(440, 440);
            spine.gameObject.AddComponent<Image>().color = SlotPlaceholder;
            spine.gameObject.AddComponent<LayoutElement>().preferredHeight = 440;
            var starRow = MakeChild(content, "Group_Stars");
            starRow.sizeDelta = new Vector2(0, 96);
            starRow.gameObject.AddComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            starRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            var stageTxt = MakeText(content, "StageText", "",
                38, TextCol, TextAlignmentOptions.Center);
            stageTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var scoreTxt = MakeText(content, "ScoreText", "",
                30, TextMuted, TextAlignmentOptions.Center);
            scoreTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var movesTxt = MakeText(content, "MovesText", "",
                30, TextMuted, TextAlignmentOptions.Center);
            movesTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var rewardTxt = MakeText(content, "RewardText", "",
                28, TextCol, TextAlignmentOptions.Center);
            rewardTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            AddFooterButton(footer, "Btn_Lobby", "common.lobby", "btn_secondary");
            AddFooterButton(footer, "Btn_Retry", "popup.clear.retry", "btn_secondary");
            AddFooterButton(footer, "Btn_Next", "common.next", "btn_primary", isPrimary: true);
            var popup = root.GetComponent<ClearPopup>();
            Assign(popup, "nextButton",  FindButtonInChildren(root, "Btn_Next"));
            Assign(popup, "retryButton", FindButtonInChildren(root, "Btn_Retry"));
            Assign(popup, "lobbyButton", FindButtonInChildren(root, "Btn_Lobby"));
            Assign(popup, "stageText",   stageTxt.GetComponent<TextMeshProUGUI>());
            Assign(popup, "scoreText",   scoreTxt.GetComponent<TextMeshProUGUI>());
            Assign(popup, "movesText",   movesTxt.GetComponent<TextMeshProUGUI>());
            Assign(popup, "rewardText",  rewardTxt.GetComponent<TextMeshProUGUI>());
            Assign(popup, "starRow",     starRow);
            SavePopupPrefab(root, "ClearPopup");
        }

        static void BuildPausePopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<PausePopup>(
                "PausePopup", "popup.pause.title", dismissible: true);
            // Pause popup footer is vertical
            var fRect = footer.GetComponent<RectTransform>() ?? footer;
            Object.DestroyImmediate(footer.gameObject.GetComponent<HorizontalLayoutGroup>());
            footer.gameObject.AddComponent<VerticalLayoutGroup>().spacing = 12;
            AddFooterButton(footer, "Btn_Resume", "common.resume", "btn_primary", isPrimary: true, fullWidth: true);
            AddFooterButton(footer, "Btn_Retry", "popup.pause.retry", "btn_secondary", fullWidth: true);
            AddFooterButton(footer, "Btn_Lobby", "common.lobby", "btn_secondary", fullWidth: true);
            SavePopupPrefab(root, "PausePopup");
        }

        static void BuildTimeoutPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<TimeoutPopup>(
                "TimeoutPopup", "popup.timeout.title", dismissible: false);
            var spine = MakeChild(content, "Slot_Spine");
            spine.sizeDelta = new Vector2(400, 400);
            spine.gameObject.AddComponent<Image>().color = SlotPlaceholder;
            spine.gameObject.AddComponent<LayoutElement>().preferredHeight = 400;
            AddFooterButton(footer, "Btn_Retry", "common.retry", "btn_primary", isPrimary: true);
            AddFooterButton(footer, "Btn_Lobby", "common.lobby", "btn_secondary");
            SavePopupPrefab(root, "TimeoutPopup");
        }

        static void BuildConfirmPopupPrefab<T>(string prefabName, string titleKey, string bodyKey)
            where T : PopupBase
        {
            var (root, panel, content, footer) = CreatePopupShell<T>(
                prefabName, titleKey, dismissible: true);
            var bodyTxt = MakeText(content, "Txt_Body", bodyKey, 26, TextMuted, TextAlignmentOptions.Center);
            bodyTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            AddFooterButton(footer, "Btn_Cancel", "common.cancel", "btn_secondary");
            AddFooterButton(footer, "Btn_Confirm", "common.confirm", "btn_primary", isPrimary: true);
            AssignConfirmPopupRefs(root);
            SavePopupPrefab(root, prefabName);
        }

        static void AssignConfirmPopupRefs(GameObject root)
        {
            if (root.TryGetComponent(out ReturnTitlePopup rtp))
            {
                Assign(rtp, "closeIconButton", FindButtonInChildren(root, "Btn_Close"));
                Assign(rtp, "cancelButton",    FindButtonInChildren(root, "Btn_Cancel"));
                Assign(rtp, "confirmButton",   FindButtonInChildren(root, "Btn_Confirm"));
            }
            if (root.TryGetComponent(out ExitGamePopup egp))
            {
                Assign(egp, "closeIconButton", FindButtonInChildren(root, "Btn_Close"));
                Assign(egp, "cancelButton",    FindButtonInChildren(root, "Btn_Cancel"));
                Assign(egp, "confirmButton",   FindButtonInChildren(root, "Btn_Confirm"));
            }
        }

        // ─── Popup shell factory ──────────────────────────────────────────

        static (GameObject root, RectTransform panel, RectTransform content, RectTransform footer)
            CreatePopupShell<T>(string id, string titleKey, bool dismissible) where T : PopupBase
        {
            var root = new GameObject("Pop_" + id, typeof(RectTransform));
            Stretch(root.GetComponent<RectTransform>());
            root.AddComponent<T>();

            // Overlay
            var overlay = MakeChild(root.GetComponent<RectTransform>(), "Overlay");
            Stretch(overlay);
            var ovImg = overlay.gameObject.AddComponent<Image>();
            ovImg.color = Scrim;
            ApplySlotSkin(ovImg, "slot_popup_overlay");
            if (dismissible)
            {
                var ovBtn = overlay.gameObject.AddComponent<Button>();
                ovBtn.targetGraphic = ovImg;
            }

            // Panel
            var panel = MakeChild(root.GetComponent<RectTransform>(), "Panel");
            SetAnchor(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            panel.sizeDelta = new Vector2(960, 0);
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = BgSurface;
            ApplySlotSkin(panelImg, "slot_popup_bg");
            var panelVlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelVlg.spacing = 24; panelVlg.padding = new RectOffset(40, 40, 32, 32);
            panelVlg.childAlignment = TextAnchor.UpperCenter;
            panelVlg.childControlWidth = true; panelVlg.childControlHeight = true;
            panelVlg.childForceExpandWidth = true; panelVlg.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // Header
            var header = MakeChild(panel, "Header");
            header.sizeDelta = new Vector2(0, 80);
            var headerHlg = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerHlg.spacing = 16; headerHlg.childAlignment = TextAnchor.MiddleLeft;
            headerHlg.childControlWidth = false; headerHlg.childControlHeight = true;
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;

            var titleGo = MakeChild(header, "Txt_Title");
            titleGo.sizeDelta = new Vector2(0, 60);
            var titleTmp = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            titleTmp.fontSize = 36; titleTmp.fontStyle = FontStyles.Bold; titleTmp.color = TextCol;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleGo.gameObject.AddComponent<LocalizedText>().SetStringId(titleKey);
            var titleLE = titleGo.gameObject.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            if (dismissible)
            {
                var closeBtn = MakeChild(header, "Btn_Close");
                closeBtn.sizeDelta = new Vector2(72, 72);
                var closeImg = closeBtn.gameObject.AddComponent<Image>();
                closeImg.color = HexColor("#FFFFFF26");
                ApplyButtonSkin(closeImg, "btn_icon_close");
                var closeBtnComp = closeBtn.gameObject.AddComponent<Button>();
                closeBtnComp.targetGraphic = closeImg;
                closeBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 72;
            }

            // Divider top
            var divTop = MakeChild(panel, "Divider_Top");
            divTop.sizeDelta = new Vector2(0, 2);
            divTop.gameObject.AddComponent<Image>().color = HexColor("#FFFFFF1A");
            divTop.gameObject.AddComponent<LayoutElement>().preferredHeight = 2;

            // Content
            var content = MakeChild(panel, "Content");
            content.sizeDelta = new Vector2(0, 0);
            var contentVlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 16; contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true; contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true; contentVlg.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // Divider bot
            var divBot = MakeChild(panel, "Divider_Bot");
            divBot.sizeDelta = new Vector2(0, 2);
            divBot.gameObject.AddComponent<Image>().color = HexColor("#FFFFFF1A");
            divBot.gameObject.AddComponent<LayoutElement>().preferredHeight = 2;

            // Footer
            var footer = MakeChild(panel, "Footer");
            footer.sizeDelta = new Vector2(0, 0);
            var footerHlg = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            footerHlg.spacing = 16; footerHlg.childAlignment = TextAnchor.MiddleCenter;
            footerHlg.childControlWidth = true; footerHlg.childControlHeight = true;
            footerHlg.childForceExpandWidth = true; footerHlg.childForceExpandHeight = false;
            footer.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            return (root, panel, content, footer);
        }

        // ─── Popup content helpers ─────────────────────────────────────────

        static void AddPopupIcon(RectTransform parent, Color tint)
        {
            var icon = MakeChild(parent, "Icon");
            icon.sizeDelta = new Vector2(128, 128);
            var img = icon.gameObject.AddComponent<Image>();
            img.color = tint;
            icon.gameObject.AddComponent<LayoutElement>().preferredHeight = 128;
        }

        static GameObject AddPopupBodyText(RectTransform parent, string stringKey)
        {
            var go = MakeText(parent, "Txt_Body", stringKey, 26, TextMuted, TextAlignmentOptions.Center);
            go.gameObject.AddComponent<LayoutElement>().preferredHeight = 160;
            return go.gameObject;
        }

        static Button AddFooterButton(RectTransform parent, string name, string labelKey,
            string skinKey, bool isPrimary = false, bool fullWidth = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 112);
            var img = go.GetComponent<Image>();
            img.color = isPrimary ? AccentA : BgSurface;
            ApplyButtonSkin(img, skinKey);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 112;
            if (fullWidth) le.flexibleWidth = 1;
            else le.flexibleWidth = 1;
            AddLocalizedLabel(rect, "Txt_Label", labelKey, Vector2.zero,
                Vector2.zero, 28f, TextAlignmentOptions.Center, FontStyles.Bold);
            return btn;
        }

        static void AddToggleRow(RectTransform parent, string name, string labelKey)
        {
            var row = MakeChild(parent, name);
            row.sizeDelta = new Vector2(0, 88);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 88;

            var lbl = MakeChild(row, "Txt_Label");
            lbl.sizeDelta = new Vector2(0, 48);
            var lblTmp = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            lblTmp.fontSize = 26; lblTmp.color = TextCol;
            lbl.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var toggle = MakeChild(row, "Toggle");
            toggle.sizeDelta = new Vector2(120, 56);
            var toggleImg = toggle.gameObject.AddComponent<Image>();
            toggleImg.color = HexColor("#FFFFFF33");
            ApplySlotSkin(toggleImg, "slot_toggle_track");
            toggle.gameObject.AddComponent<Toggle>().targetGraphic = toggleImg;
            toggle.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;

            var handle = MakeChild(toggle, "Handle");
            handle.sizeDelta = new Vector2(48, 48);
            handle.anchoredPosition = new Vector2(4, 0);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = TextCol;
            ApplySlotSkin(handleImg, "slot_toggle_handle");
        }

        static void AddDropdownRow(RectTransform parent, string name, string labelKey)
        {
            var row = MakeChild(parent, name);
            row.sizeDelta = new Vector2(0, 88);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 88;

            var lbl = MakeChild(row, "Txt_Label");
            lbl.sizeDelta = new Vector2(0, 48);
            var lblTmp = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            lblTmp.fontSize = 26; lblTmp.color = TextCol;
            lbl.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var dd = MakeChild(row, "Dropdown");
            dd.sizeDelta = new Vector2(320, 72);
            var ddImg = dd.gameObject.AddComponent<Image>();
            ddImg.color = HexColor("#FFFFFF11");
            dd.gameObject.AddComponent<TMP_Dropdown>();
            dd.gameObject.AddComponent<LayoutElement>().preferredWidth = 320;
        }

        static void AddProviderRow(RectTransform parent, string name, string iconSkinKey, string labelText)
        {
            var row = MakeChild(parent, name);
            row.sizeDelta = new Vector2(0, 96);
            var img = row.gameObject.AddComponent<Image>();
            img.color = HexColor("#FFFFFF0D");
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.padding = new RectOffset(16, 16, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;

            var icon = MakeChild(row, "Icon");
            icon.sizeDelta = new Vector2(56, 56);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.color = SlotPlaceholder;
            ApplySlotSkin(iconImg, iconSkinKey);
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;

            var nameGo = MakeChild(row, "Txt_Name");
            nameGo.sizeDelta = new Vector2(0, 48);
            var nameTmp = nameGo.gameObject.AddComponent<TextMeshProUGUI>();
            nameTmp.text = labelText; nameTmp.fontSize = 26; nameTmp.color = TextCol;
            nameGo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var linkBtn = MakeChild(row, "Btn_LinkState");
            linkBtn.sizeDelta = new Vector2(200, 72);
            var linkImg = linkBtn.gameObject.AddComponent<Image>();
            linkImg.color = BgSurface;
            ApplyButtonSkin(linkImg, "btn_secondary");
            var linkBtnComp = linkBtn.gameObject.AddComponent<Button>();
            linkBtnComp.targetGraphic = linkImg;
            linkBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
        }

        static void SavePopupPrefab(GameObject root, string prefabName)
        {
            PrefabUtility.SaveAsPrefabAsset(root, $"{PopupPrefabRoot}/{prefabName}.prefab");
            Object.DestroyImmediate(root);
        }

        // ─── Shared canvas / layer helpers ───────────────────────────────

        static (GameObject canvas, RectTransform safe) CreateSceneCanvas(string canvasName)
        {
            var root = new GameObject(canvasName,
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Stretch(root.GetComponent<RectTransform>());

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefW, RefH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100;

            // Panel_Background
            var bg = new GameObject("Panel_Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = BgPrimary;
            bg.GetComponent<Image>().raycastTarget = false;

            // SafeArea
            var safeGo = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaFitter));
            safeGo.transform.SetParent(root.transform, false);
            Stretch(safeGo.GetComponent<RectTransform>());

            return (root, safeGo.GetComponent<RectTransform>());
        }

        static void DestroyExistingRoots(string sceneName)
        {
            // Destroy both old naming convention and new canvas naming
            foreach (string n in new[] {
                $"StaticUIRoot_{sceneName}",
                $"{sceneName}Canvas"
            })
            {
                var existing = GameObject.Find(n);
                if (existing != null)
                    Object.DestroyImmediate(existing);
            }
        }

        static void AddPopupLayer(GameObject canvasRoot, int sortingOrder)
        {
            var go = new GameObject("PopupLayer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(canvasRoot.transform, false);
            Stretch(go.GetComponent<RectTransform>());
            var c = go.GetComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = sortingOrder;
        }

        static void AddToastLayer(GameObject canvasRoot, int sortingOrder)
        {
            var go = new GameObject("ToastLayer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            go.transform.SetParent(canvasRoot.transform, false);
            Stretch(go.GetComponent<RectTransform>());
            var c = go.GetComponent<Canvas>();
            c.overrideSorting = true;
            c.sortingOrder = sortingOrder;
        }

        // ─── Low-level UI element factories ──────────────────────────────

        enum AnchorPreset { TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight,
            BottomLeft, BottomCenter, BottomRight, Stretch }

        static RectTransform MakeChild(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static RectTransform MakeSlot(RectTransform parent, string name,
            Vector2 anchoredPos, Vector2 size,
            AnchorPreset anchor = AnchorPreset.MiddleCenter)
        {
            var rect = MakeChild(parent, name);
            ApplyAnchorPreset(rect, anchor);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var img = rect.gameObject.AddComponent<Image>();
            img.color = SlotPlaceholder;
            img.raycastTarget = false;
            ApplySlotSkin(img, name.ToLower().Replace("_", "").Replace("slot", "slot_"));
            return rect;
        }

        static Button MakeButton(RectTransform parent, string name, string labelKey,
            Vector2 size, string skinKey, int fontSize = 32)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = AccentA;
            ApplyButtonSkin(img, skinKey);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.ColorTint;

            AddLocalizedLabel(rect, "Txt_Label", labelKey, Vector2.zero,
                Vector2.zero, fontSize, TextAlignmentOptions.Center, FontStyles.Bold);

            return btn;
        }

        static Button MakeIconButton(RectTransform parent, string name, string skinKey,
            Vector2 size, Vector2 anchoredPos, AnchorPreset anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            ApplyAnchorPreset(rect, anchor);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;

            var img = go.GetComponent<Image>();
            img.color = HexColor("#FFFFFF26");
            ApplyButtonSkin(img, skinKey);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            return btn;
        }

        static GameObject MakeText(RectTransform parent, string name, string stringKey,
            float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize; tmp.color = color; tmp.alignment = alignment;
            tmp.raycastTarget = false;
            if (!string.IsNullOrEmpty(stringKey))
                go.AddComponent<LocalizedText>().SetStringId(stringKey);
            return go;
        }

        static TextMeshProUGUI AddLocalizedLabel(RectTransform parent, string name, string stringId,
            Vector2 position, Vector2 size, float fontSize = 28f,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (size == Vector2.zero)
            {
                Stretch(rect);
            }
            else
            {
                SetAnchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                rect.sizeDelta = size;
                rect.anchoredPosition = position;
            }
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize; tmp.fontStyle = style;
            tmp.color = TextCol; tmp.alignment = alignment;
            tmp.raycastTarget = false;
            if (!string.IsNullOrEmpty(stringId))
                go.AddComponent<LocalizedText>().SetStringId(stringId);
            return tmp;
        }

        // ─── RectTransform helpers ────────────────────────────────────────

        static void ApplyAnchorPreset(RectTransform rect, AnchorPreset preset)
        {
            switch (preset)
            {
                case AnchorPreset.TopLeft:     SetAnchor(rect, new(0,1), new(0,1), new(0,1));   break;
                case AnchorPreset.TopCenter:   SetAnchor(rect, new(.5f,1), new(.5f,1), new(.5f,1)); break;
                case AnchorPreset.TopRight:    SetAnchor(rect, new(1,1), new(1,1), new(1,1));   break;
                case AnchorPreset.MiddleLeft:  SetAnchor(rect, new(0,.5f), new(0,.5f), new(0,.5f)); break;
                case AnchorPreset.MiddleCenter:SetAnchor(rect, new(.5f,.5f), new(.5f,.5f), new(.5f,.5f)); break;
                case AnchorPreset.MiddleRight: SetAnchor(rect, new(1,.5f), new(1,.5f), new(1,.5f)); break;
                case AnchorPreset.BottomLeft:  SetAnchor(rect, new(0,0), new(0,0), new(0,0));   break;
                case AnchorPreset.BottomCenter:SetAnchor(rect, new(.5f,0), new(.5f,0), new(.5f,0)); break;
                case AnchorPreset.BottomRight: SetAnchor(rect, new(1,0), new(1,0), new(1,0));   break;
                case AnchorPreset.Stretch:     Stretch(rect); break;
            }
        }

        static void SetAnchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot;
        }

        static void Stretch(RectTransform rect, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(l, b);
            rect.offsetMax = new Vector2(-r, -t);
        }

        static void Center(RectTransform rect, Vector2 pos, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        // ─── Skin helpers ──────────────────────────────────────────────────

        static UIButtonSkin LoadSkin() =>
            _skin ??= AssetDatabase.LoadAssetAtPath<UIButtonSkin>(SkinAssetPath);

        static void ApplyButtonSkin(Image image, string key)
        {
            var sprite = LoadSkin()?.GetButton(key);
            if (sprite == null) return;
            image.sprite = sprite;
            image.color = Color.white;
        }

        static void ApplySlotSkin(Image image, string key)
        {
            var sprite = LoadSkin()?.GetSlot(key);
            if (sprite == null) return;
            image.sprite = sprite;
            image.color = Color.white;
        }

        // ─── Scene infrastructure helpers ────────────────────────────────

        static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
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
                "Game"  => (int)SceneEscapeHandler.EscapeAction.OpenPauseMenu,
                _ => (int)SceneEscapeHandler.EscapeAction.None
            };
            so.FindProperty("action").enumValueIndex = action;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddLobbyTabController(GameObject root,
            Button shopTab, Button homeTab, Button rankingTab,
            GameObject shopPanel, GameObject homePanel, GameObject rankingPanel)
        {
            var type = System.Type.GetType("ProjectLink.OutGame.UI.LobbyTabController, Assembly-CSharp");
            if (type == null)
            {
                Debug.LogWarning("LobbyTabController not found — compile scripts before rebuilding Lobby UI.");
                return;
            }
            var ctrl = root.AddComponent(type);
            Assign(ctrl, "shopTabButton",  shopTab);
            Assign(ctrl, "homeTabButton",  homeTab);
            Assign(ctrl, "rankingTabButton", rankingTab);
            Assign(ctrl, "shopPanel",    shopPanel);
            Assign(ctrl, "homePanel",    homePanel);
            Assign(ctrl, "rankingPanel", rankingPanel);
        }

        // ─── Serialized field assignment ──────────────────────────────────

        static void Assign(Object target, string propertyName, Object value)
        {
            if (target == null || value == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ─── Find helpers ─────────────────────────────────────────────────

        static Button FindButtonInChildren(RectTransform root, string name)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                if (b.name == name) return b;
            return null;
        }

        static Button FindButtonInChildren(GameObject root, string name)
        {
            foreach (var b in root.GetComponentsInChildren<Button>(true))
                if (b.name == name) return b;
            return null;
        }

        static TextMeshProUGUI FindTmpInChildren(RectTransform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == name) return t;
            return null;
        }

        static RectTransform FindRectInChildren(RectTransform root, string name)
        {
            foreach (var r in root.GetComponentsInChildren<RectTransform>(true))
                if (r.name == name) return r;
            return null;
        }

        // ─── Sprite sheet import ──────────────────────────────────────────

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

        // ─── Misc ─────────────────────────────────────────────────────────

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
