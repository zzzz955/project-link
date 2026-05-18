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
        const string SkinAssetPath = "Assets/Editor/UISpriteSkin.asset";

        // Palette (scenes.json)
        static readonly Color BgPrimary   = HexColor("#151525");
        static readonly Color BgSurface   = HexColor("#1E2340");
        static readonly Color AccentA     = HexColor("#7B2FBE");
        static readonly Color AccentB     = HexColor("#E040FB");
        static readonly Color CtaPrimary  = HexColor("#FF7043"); // warm orange — hyper-casual CTA
        static readonly Color CtaSecondary= HexColor("#3D4A6B"); // muted blue-grey for secondary
        static readonly Color Positive    = HexColor("#00E5FF");
        static readonly Color Lime        = HexColor("#76FF03");
        static readonly Color Warning     = HexColor("#FFB300");
        static readonly Color Danger      = HexColor("#FF5252");
        static readonly Color TextCol     = HexColor("#FFFFFF");
        static readonly Color TextMuted   = HexColor("#9BAABB");
        static readonly Color TextDisabled= HexColor("#546E7A");
        static readonly Color Scrim       = HexColor("#000000CC");
        static readonly Color HudBg       = HexColor("#0E1430E6");
        static readonly Color SurfaceEE   = HexColor("#1E2340EE");
        static readonly Color SlotPlaceholder = new(0.10f, 0.18f, 0.32f, 0.45f);

        static UISpriteSkin _skin;

        // ─── Menu entries ──────────────────────────────────────────────────

        [MenuItem("Tools/Project Link/UI Build/Create UI Sprite Skin")]
        public static void CreateUISpriteSkin()
        {
            EnsureFolder("Assets/Editor");
            var skin = AssetDatabase.LoadAssetAtPath<UISpriteSkin>(SkinAssetPath);
            if (skin == null)
            {
                skin = ScriptableObject.CreateInstance<UISpriteSkin>();
                AssetDatabase.CreateAsset(skin, SkinAssetPath);
            }

            var existing = new System.Collections.Generic.Dictionary<string, Sprite>();
            foreach (var e in skin.sprites)
                if (!string.IsNullOrEmpty(e.elementName))
                    existing[e.elementName] = e.sprite;

            var scanned = CollectSkinKeysFromSource();
            foreach (var key in scanned)
                if (!existing.ContainsKey(key))
                    existing[key] = null;

            // scanned keys first (sorted), then user-added custom keys
            var entries = new System.Collections.Generic.List<UISpriteSkin.Entry>();
            var sorted = new System.Collections.Generic.List<string>(scanned);
            sorted.Sort();
            foreach (var key in sorted)
                entries.Add(new UISpriteSkin.Entry { elementName = key, sprite = existing[key] });
            foreach (var kvp in existing)
                if (!scanned.Contains(kvp.Key))
                    entries.Add(new UISpriteSkin.Entry { elementName = kvp.Key, sprite = kvp.Value });

            skin.sprites = entries.ToArray();
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Selection.activeObject = skin;
            Debug.Log($"UISpriteSkin synced: {entries.Count} entries at {SkinAssetPath}");
        }

        static System.Collections.Generic.HashSet<string> CollectSkinKeysFromSource()
        {
            var keys = new System.Collections.Generic.HashSet<string>();
            var guids = AssetDatabase.FindAssets("ProjectLinkUIBuilder t:Script");
            if (guids.Length == 0) return keys;

            var src = System.IO.File.ReadAllText(
                System.IO.Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guids[0])));

            // All "btn_*" and "slot_*" string literals in the source
            var re = new System.Text.RegularExpressions.Regex(@"""((btn|slot)_[a-z_0-9]+)""");
            foreach (System.Text.RegularExpressions.Match m in re.Matches(src))
                keys.Add(m.Groups[1].Value);

            // MakeSlot: name arg is "Slot_Xxx" -> derives key via ToLower/Replace
            var slotRe = new System.Text.RegularExpressions.Regex(@"MakeSlot\s*\([^,]+,\s*""([^""]+)""");
            foreach (System.Text.RegularExpressions.Match m in slotRe.Matches(src))
            {
                var derived = m.Groups[1].Value.ToLower().Replace("_", "").Replace("slot", "slot_");
                keys.Add(derived);
            }

            return keys;
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
                string prev = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                BuildScene(scene.name);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);

                if (prev != null && ContentMatchesIgnoringFileIds(System.IO.File.ReadAllText(path), prev))
                {
                    System.IO.File.WriteAllText(path, prev);
                    // Reload from disk so the in-memory scene is clean (no "unsaved changes" prompt)
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                }
            }

            if (!Application.isBatchMode)
                EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity", OpenSceneMode.Single);
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
            BuildClearNextStageConfirmPopup();

            // Standard popups from popups.json
            BuildForceUpdatePopup();
            BuildMaintenancePopup();
            BuildSessionExpiredPopup();
            BuildSettingPopup();
            BuildEnergyPopup();
            BuildStageDetailPopup();
            BuildStreakChallengePopup();
            BuildRewardPopup();
            BuildAccountPopup();
            BuildClearPopup();
            BuildPausePopup();
            BuildTimeoutPopup();
            BuildShopItemConfirmPopup();
            BuildShopItemResultPopup();
            CreateShopProductCardPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Project Link/UI Build/Assign Icon Animations")]
        public static void AssignIconAnimations()
        {
            _skin = null;
            int total = 0;

            if (!Application.isBatchMode)
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            string[] scenePaths =
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Title.unity",
                "Assets/Scenes/Lobby.unity",
                "Assets/Scenes/Game.unity"
            };

            foreach (var path in scenePaths)
            {
                if (!System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                    total += AddAnimatorToIconImages(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PopupPrefabRoot });
            foreach (var guid in guids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                int n = AddAnimatorToIconImages(prefab);
                if (n > 0) PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefab);
                total += n;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"UIIconAnimator assigned to {total} icon images.");
        }

        [MenuItem("Tools/Project Link/UI Build/Register Untracked Sprites")]
        public static void RegisterUntrackedSprites()
        {
            _skin = null;
            var skin = LoadSkin();
            if (skin == null) { Debug.LogError("UISpriteSkin not found. Run 'Create UI Sprite Skin' first."); return; }

            // Track already-registered sprites and keys
            var trackedSprites = new System.Collections.Generic.HashSet<Sprite>();
            var existingKeys   = new System.Collections.Generic.HashSet<string>();
            foreach (var e in skin.sprites)
            {
                if (e.sprite != null) trackedSprites.Add(e.sprite);
                if (!string.IsNullOrEmpty(e.elementName)) existingKeys.Add(e.elementName);
            }

            // sprite != null → group by Sprite reference (same sprite = same key = shared)
            var spriteGroups = new System.Collections.Generic.Dictionary<Sprite, (string goName, string parentName)>();
            // sprite == null → group by GO name (same name = same key = shared)
            var nullGroups   = new System.Collections.Generic.Dictionary<string, string>(); // goName → parentName

            void ScanGo(GameObject root)
            {
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                {
                    if (img.sprite != null)
                    {
                        if (trackedSprites.Contains(img.sprite)) continue;
                        if (!spriteGroups.ContainsKey(img.sprite))
                            spriteGroups[img.sprite] = (img.gameObject.name, img.transform.parent?.name ?? "");
                    }
                    else
                    {
                        var goName = img.gameObject.name;
                        if (!nullGroups.ContainsKey(goName))
                            nullGroups[goName] = img.transform.parent?.name ?? "";
                    }
                }
            }

            if (!Application.isBatchMode)
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            string[] scenePaths =
            {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Title.unity",
                "Assets/Scenes/Lobby.unity",
                "Assets/Scenes/Game.unity"
            };

            foreach (var path in scenePaths)
            {
                if (!System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var root in scene.GetRootGameObjects())
                    ScanGo(root);
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { PopupPrefabRoot });
            foreach (var guid in guids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                ScanGo(prefab);
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            int added = 0;
            var newEntries = new System.Collections.Generic.List<UISpriteSkin.Entry>(skin.sprites);

            // Pass 1: sprite groups (real sprites — higher priority for key names)
            foreach (var kvp in spriteGroups)
            {
                var key = DeriveKey(kvp.Value.goName, kvp.Value.parentName, existingKeys);
                existingKeys.Add(key);
                newEntries.Add(new UISpriteSkin.Entry { elementName = key, sprite = kvp.Key });
                added++;
            }

            // Pass 2: null-sprite groups (registered with null sprite so builder can load them later)
            foreach (var kvp in nullGroups)
            {
                var key = DeriveKey(kvp.Key, kvp.Value, existingKeys);
                if (existingKeys.Contains(key)) continue; // already claimed by sprite pass or pre-existing
                existingKeys.Add(key);
                newEntries.Add(new UISpriteSkin.Entry { elementName = key, sprite = null });
                added++;
            }

            if (added == 0) { Debug.Log("RegisterUntrackedSprites: all images already tracked."); return; }

            skin.sprites = newEntries.ToArray();
            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            Selection.activeObject = skin;
            Debug.Log($"RegisterUntrackedSprites: added {added} entries ({spriteGroups.Count} with sprite, {nullGroups.Count} null-sprite) to UISpriteSkin.");
        }

        static string DeriveKey(string goName, string parentName, System.Collections.Generic.HashSet<string> existingKeys)
        {
            string prefix = "slot_";
            string base_;

            if (goName.StartsWith("Btn_", System.StringComparison.OrdinalIgnoreCase))
            { prefix = "btn_"; base_ = goName.Substring(4); }
            else if (goName.StartsWith("Img_", System.StringComparison.OrdinalIgnoreCase))
                base_ = goName.Substring(4);
            else if (goName.StartsWith("Slot_", System.StringComparison.OrdinalIgnoreCase))
                base_ = goName.Substring(5);
            else if (goName.StartsWith("Icon_", System.StringComparison.OrdinalIgnoreCase))
                base_ = "icon_" + goName.Substring(5);
            else
                base_ = goName;

            base_ = base_.ToLower().Replace(" ", "_");
            string key = prefix + base_;

            if (existingKeys.Contains(key) && !string.IsNullOrEmpty(parentName))
            {
                var ctx = parentName.ToLower()
                    .Replace("group_", "").Replace("panel_", "").Replace("slot_", "")
                    .Replace("img_", "").Replace("_", "");
                key = prefix + ctx + "_" + base_;
            }

            int idx = 2;
            while (existingKeys.Contains(key)) { key = prefix + base_ + "_" + idx; idx++; }

            return key;
        }

        static int AddAnimatorToIconImages(GameObject root)
        {
            var skin = LoadSkin();
            var btnIconSprites = new System.Collections.Generic.HashSet<Sprite>();
            if (skin != null)
                foreach (var e in skin.sprites)
                    if (e.elementName.StartsWith("btn_icon_") && e.sprite != null)
                        btnIconSprites.Add(e.sprite);

            int count = 0;
            foreach (var img in root.GetComponentsInChildren<Image>(true))
            {
                var n = img.gameObject.name;
                bool named = n.StartsWith("Icon_", System.StringComparison.OrdinalIgnoreCase)
                          || string.Equals(n, "Icon", System.StringComparison.OrdinalIgnoreCase);
                bool isBtnIcon = img.sprite != null && btnIconSprites.Contains(img.sprite);

                if (!named && !isBtnIcon) continue;
                if (img.GetComponent<UIIconAnimator>() != null) continue;

                img.gameObject.AddComponent<UIIconAnimator>();
                EditorUtility.SetDirty(img.gameObject);
                count++;
            }
            return count;
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

            var (canvas, safe) = CreateSceneCanvas(sceneName + "Canvas", sceneName != "Game");
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

            NormalizeLayoutText(canvas);
            EnsureLocalizedFonts(canvas);
            AddAnimatorToIconImages(canvas);
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
            ApplySkin(barBg, "slot_progress_track", false);
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
            var errText = MakeText(safe, "Txt_NetworkError", "bootstrap.network_error", 28, Danger, TextAlignmentOptions.Midline);
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
            ApplySkin(img, bgSkinKey);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<ProjectLink.Core.ButtonPressEffect>();

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
            ApplySkin(iconImg, iconSkinKey);
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
            // HUD_Strip — single-row HLG: Avatar | Stamina | Currency | Menu
            var hud = MakeChild(safe, "HUD_Strip");
            SetAnchor(hud, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            hud.sizeDelta = new Vector2(0, 136);
            hud.anchoredPosition = Vector2.zero;
            var hudImg = hud.gameObject.AddComponent<Image>();
            hudImg.color = HudBg;
            ApplySkin(hudImg, "slot_hud_bg", false);
            var hudHlg = hud.gameObject.AddComponent<HorizontalLayoutGroup>();
            hudHlg.spacing = 16;
            hudHlg.padding = new RectOffset(28, 28, 20, 20);
            hudHlg.childAlignment = TextAnchor.MiddleCenter;
            hudHlg.childControlWidth = true; hudHlg.childControlHeight = true;
            hudHlg.childForceExpandWidth = true; hudHlg.childForceExpandHeight = true;

            var avatar = MakeChild(hud, "Slot_Avatar");
            avatar.sizeDelta = new Vector2(80, 80);
            var avatarImg = avatar.gameObject.AddComponent<Image>();
            avatarImg.color = SlotPlaceholder;
            ApplySkin(avatarImg, "slot_avatar");
            avatar.gameObject.AddComponent<Mask>();
            var avatarBtn = avatar.gameObject.AddComponent<Button>();
            avatarBtn.targetGraphic = avatarImg;
            UnityEventTools.AddPersistentListener(avatarBtn.onClick, router.OpenAccountPopup);
            var avatarLE = avatar.gameObject.AddComponent<LayoutElement>();
            avatarLE.preferredWidth = 80; avatarLE.preferredHeight = 80; avatarLE.flexibleWidth = 0;

            AddStaminaGroup(hud, router);
            AddCurrencyGroup(hud);

            var menuBtn = MakeChild(hud, "Btn_Menu");
            menuBtn.sizeDelta = new Vector2(80, 80);
            var menuImg = menuBtn.gameObject.AddComponent<Image>();
            menuImg.color = HexColor("#FFFFFF26");
            ApplySkin(menuImg, "btn_icon_menu");
            var menuButton = menuBtn.gameObject.AddComponent<Button>();
            menuButton.targetGraphic = menuImg;
            UnityEventTools.AddPersistentListener(menuButton.onClick, router.OpenSettingsPopup);
            var menuLE = menuBtn.gameObject.AddComponent<LayoutElement>();
            menuLE.preferredWidth = 80; menuLE.flexibleWidth = 0;

            // MenuDropdown (hidden)
            var dropdown = MakeChild(safe, "MenuDropdown");
            SetAnchor(dropdown, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));
            dropdown.sizeDelta = new Vector2(360, 200);
            dropdown.anchoredPosition = new Vector2(-32, -240);
            var ddImg = dropdown.gameObject.AddComponent<Image>();
            ddImg.color = SurfaceEE;
            ApplySkin(ddImg, "slot_popup_bg", false);
            var ddVlg = dropdown.gameObject.AddComponent<VerticalLayoutGroup>();
            ddVlg.spacing = 4; ddVlg.padding = new RectOffset(12, 12, 12, 12);
            ddVlg.childControlWidth = true; ddVlg.childControlHeight = true;
            AddDropdownItem(dropdown, "Btn_Settings", "hud.settings", router.OpenSettingsPopup);
            AddDropdownItem(dropdown, "Btn_Language", "hud.language", router.OpenSettingsPopup);
            dropdown.gameObject.SetActive(false);

            // Group_TabBodies
            var tabBodies = MakeChild(safe, "Group_TabBodies");
            SetAnchor(tabBodies, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            tabBodies.offsetMin = new Vector2(0, 136);
            tabBodies.offsetMax = new Vector2(0, -136);

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
            Assign(lobbyCtrl, "starOnSprite",         LoadSkin()?.Get("slot_star_on"));
            Assign(lobbyCtrl, "starOffSprite",        LoadSkin()?.Get("slot_star_off"));
            Assign(lobbyCtrl, "lockSprite",           LoadSkin()?.Get("slot_lock_icon"));
            AssignSpriteArray(lobbyCtrl, "difficultySprites", new[]
            {
                LoadSkin()?.Get("slot_difficulty_1"),
                LoadSkin()?.Get("slot_difficulty_2"),
                LoadSkin()?.Get("slot_difficulty_3"),
                LoadSkin()?.Get("slot_difficulty_4"),
                LoadSkin()?.Get("slot_difficulty_5"),
            });
            Assign(lobbyCtrl, "playButton",           FindButtonInChildren(tabHome, "StageNode_Center"));
            Assign(lobbyCtrl, "refillButton",         FindButtonInChildren(tabHome, "Btn_Refill"));
            Assign(lobbyCtrl, "energyText",           FindTmpInChildren(safe, "Txt_StaminaCount"));
            Assign(lobbyCtrl, "coinText",             FindTmpInChildren(safe, "Txt_CurrencyCount"));
            Assign(lobbyCtrl, "stageNumberText",      FindTmpInChildren(tabHome, "Txt_StageNum"));
            Assign(lobbyCtrl, "dailyProgressText",    FindTmpInChildren(tabHome, "Txt_Frac"));
            Assign(lobbyCtrl, "colorCupTimerText",    FindTmpInChildren(tabHome, "Txt_Ends"));
            Assign(lobbyCtrl, "playDisabledReasonText", FindTmpInChildren(tabHome, "Txt_PlayDisabled"));
            Assign(lobbyCtrl, "shopBalanceText",      FindTmpInChildren(tabShop, "Txt_Balance"));
            Assign(lobbyCtrl, "rankingMetricText",    FindTmpInChildren(tabRanking, "Txt_Score"));
            Assign(lobbyCtrl, "rankingErrorText",     FindTmpInChildren(tabRanking, "Txt_RankError"));
            Assign(lobbyCtrl, "shopContent",          FindRectInChildren(tabShop, "Content"));
            Assign(lobbyCtrl, "rankingContent",       FindRectInChildren(tabRanking, "Content"));

            // ShopProductCard prefab + inventory strip + item icon sprites
            var cardPrefab = AssetDatabase.LoadAssetAtPath<ShopProductCard>($"{PopupPrefabRoot}/ShopProductCard.prefab");
            if (cardPrefab != null)
                Assign(lobbyCtrl, "shopProductCardPrefab", cardPrefab);
            var inventoryStrip = FindRectInChildren(tabShop, "Row_InventoryStrip")?.GetComponent<ShopInventoryStrip>();
            if (inventoryStrip != null)
                Assign(lobbyCtrl, "shopInventoryStrip", inventoryStrip);
            AssignSpriteArray(lobbyCtrl, "itemIconSprites", new[]
            {
                LoadSkin()?.Get("slot_item_1"),
                LoadSkin()?.Get("slot_item_2"),
                LoadSkin()?.Get("slot_item_3"),
                LoadSkin()?.Get("slot_item_4"),
            });

            AddLobbyTabController(safe.gameObject,
                shopTabBtn, homeTabBtn, rankTabBtn,
                tabShop.gameObject, tabHome.gameObject, tabRanking.gameObject);
        }

        static void AddStaminaGroup(RectTransform parent, RuntimeNavigationButtons router)
        {
            var group = MakeChild(parent, "Group_Stamina");
            group.sizeDelta = new Vector2(0, 88);
            var groupImg = group.gameObject.AddComponent<Image>();
            groupImg.color = new Color(1, 1, 1, 0);
            ApplySkin(groupImg, "slot_resource_bg", false);
            var hlg = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            var btn = group.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            UnityEventTools.AddPersistentListener(btn.onClick, router.OpenEnergyPopup);
            var staminaLe = group.gameObject.AddComponent<LayoutElement>();
            staminaLe.preferredWidth = 240; staminaLe.flexibleWidth = 0;

            // Icon with count overlaid on top
            var iconStack = MakeChild(group, "Stack_Stamina");
            iconStack.sizeDelta = new Vector2(64, 64);
            iconStack.gameObject.AddComponent<LayoutElement>().preferredWidth = 64;

            var icon = MakeChild(iconStack, "Icon_Stamina");
            Stretch(icon);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.color = SlotPlaceholder;
            ApplySkin(iconImg, "slot_stamina_icon");

            var count = MakeChild(iconStack, "Txt_StaminaCount");
            Stretch(count);
            var countTmp = count.gameObject.AddComponent<TextMeshProUGUI>();
            countTmp.text = "5"; countTmp.fontSize = 26; countTmp.fontStyle = FontStyles.Bold;
            countTmp.color = Color.white; countTmp.alignment = TextAlignmentOptions.Midline;
            countTmp.raycastTarget = false;

            var timer = MakeChild(group, "Txt_StaminaTimer");
            timer.sizeDelta = new Vector2(136, 44);
            var timerTmp = timer.gameObject.AddComponent<TextMeshProUGUI>();
            timerTmp.text = ""; timerTmp.fontSize = 24; timerTmp.color = Warning;
            timerTmp.alignment = TextAlignmentOptions.MidlineLeft;
            timer.gameObject.AddComponent<LayoutElement>().preferredWidth = 136;
        }

        static void AddCurrencyGroup(RectTransform parent)
        {
            var group = MakeChild(parent, "Group_Currency");
            group.sizeDelta = new Vector2(176, 88);
            var groupImg = group.gameObject.AddComponent<Image>();
            groupImg.color = new Color(1, 1, 1, 0);
            ApplySkin(groupImg, "slot_resource_bg", false);
            var hlg = group.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            var le = group.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 176; le.flexibleWidth = 0;

            var icon = MakeChild(group, "Icon_Currency");
            icon.sizeDelta = new Vector2(56, 56);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.color = SlotPlaceholder;
            ApplySkin(iconImg, "slot_currency_soft");
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;

            var count = MakeChild(group, "Txt_CurrencyCount");
            count.sizeDelta = new Vector2(96, 44);
            var countTmp = count.gameObject.AddComponent<TextMeshProUGUI>();
            countTmp.text = "0"; countTmp.fontSize = 28; countTmp.fontStyle = FontStyles.Bold; countTmp.color = TextCol;
            countTmp.alignment = TextAlignmentOptions.MidlineRight;
            count.gameObject.AddComponent<LayoutElement>().preferredWidth = 96;
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
            item.gameObject.AddComponent<ProjectLink.Core.ButtonPressEffect>();
            item.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            // TMP must be on a child — a GO cannot have both Image and TextMeshProUGUI (both are Graphic)
            var lbl = MakeChild(item, "Lbl");
            Stretch(lbl, 24, 0, 24, 0);
            var txt = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            txt.fontSize = 28; txt.color = TextCol;
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

            // Group_Track (stage node area)
            var track = MakeChild(carousel, "Group_Track");
            SetAnchor(track, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            track.offsetMin = new Vector2(120, 80);
            track.offsetMax = new Vector2(-120, 0);
            var trackHlg = track.gameObject.AddComponent<HorizontalLayoutGroup>();
            trackHlg.spacing = 40; trackHlg.childAlignment = TextAnchor.MiddleCenter;

            // Stage node — acts as play button
            var node = MakeChild(track, "StageNode_Center");
            node.sizeDelta = new Vector2(500, 500);
            var nodeImg = node.gameObject.AddComponent<Image>();
            nodeImg.color = BgSurface;
            ApplySkin(nodeImg, "slot_stage_node");
            var nodeBtn = node.gameObject.AddComponent<Button>();
            nodeBtn.targetGraphic = nodeImg;
            UnityEventTools.AddPersistentListener(nodeBtn.onClick, router.LoadGame);
            node.gameObject.AddComponent<ProjectLink.Core.ButtonPressEffect>();
            // "STAGE" label — small, above number
            var stageLabelGo = MakeChild(node, "Txt_StageLabel");
            Center(stageLabelGo, new Vector2(0, 200), new Vector2(360, 48));
            var stageLabelTmp = stageLabelGo.gameObject.AddComponent<TextMeshProUGUI>();
            stageLabelTmp.fontSize = 28; stageLabelTmp.fontStyle = FontStyles.Bold;
            stageLabelTmp.color = TextMuted; stageLabelTmp.alignment = TextAlignmentOptions.Midline;
            stageLabelTmp.raycastTarget = false;
            stageLabelGo.gameObject.AddComponent<LocalizedText>().SetStringId("lobby.stage_label");

            // Stage number — large, centered
            var stageNum = MakeChild(node, "Txt_StageNum");
            Center(stageNum, new Vector2(0, 120), new Vector2(420, 140));
            var numTmp = stageNum.gameObject.AddComponent<TextMeshProUGUI>();
            numTmp.text = "1"; numTmp.fontSize = 112; numTmp.fontStyle = FontStyles.Bold;
            numTmp.color = TextCol; numTmp.alignment = TextAlignmentOptions.Midline;

            // Star images at bottom of node — slot_star_on/slot_star_off assigned at runtime
            var starsRow = MakeChild(node, "Group_Stars");
            Center(starsRow, new Vector2(0, -160), new Vector2(336, 96));
            var starsHlg = starsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            starsHlg.spacing = 24; starsHlg.childAlignment = TextAnchor.MiddleCenter;
            starsHlg.childControlWidth = false; starsHlg.childControlHeight = false;
            starsHlg.childForceExpandWidth = false; starsHlg.childForceExpandHeight = false;
            for (int si = 0; si < 3; si++)
            {
                var starSlot = MakeChild(starsRow, $"Img_Star_{si}");
                starSlot.sizeDelta = new Vector2(96, 96);
                var starImg = starSlot.gameObject.AddComponent<Image>();
                starImg.color = new Color(1f, 1f, 1f, 0.25f);
                ApplySkin(starImg, "slot_star_off");
            }

            // LockIcon — shown at runtime when stage is locked
            var lockIcon = MakeChild(node, "LockIcon");
            Center(lockIcon, Vector2.zero, new Vector2(160, 160));
            var lockImg = lockIcon.gameObject.AddComponent<Image>();
            lockImg.color = new Color(1f, 1f, 1f, 0.85f);
            ApplySkin(lockImg, "slot_lock_icon");
            lockIcon.gameObject.SetActive(false);

            var prevBtn = MakeIconButton(carousel, "Btn_Prev", "btn_carousel_prev",
                new Vector2(96, 96), new Vector2(16, 0), AnchorPreset.MiddleLeft);
            var nextBtn = MakeIconButton(carousel, "Btn_Next", "btn_carousel_next",
                new Vector2(96, 96), new Vector2(-16, 0), AnchorPreset.MiddleRight);

            var playDisabled = MakeChild(carousel, "Txt_PlayDisabled");
            SetAnchor(playDisabled, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            playDisabled.sizeDelta = new Vector2(500, 40);
            playDisabled.anchoredPosition = new Vector2(0, 16);
            var pdTmp = playDisabled.gameObject.AddComponent<TextMeshProUGUI>();
            pdTmp.fontSize = 22; pdTmp.color = Danger;
            pdTmp.alignment = TextAlignmentOptions.Midline;
            playDisabled.gameObject.SetActive(false);

            // Group_Events — sibling of Carousel_Stages (child of Tab_Home); top-left overlay
            var evtGroup = MakeChild(tab, "Group_Events");
            SetAnchor(evtGroup, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            evtGroup.anchoredPosition = new Vector2(12, -44);
            evtGroup.sizeDelta = new Vector2(160, 0);
            var evtVlg = evtGroup.gameObject.AddComponent<VerticalLayoutGroup>();
            evtVlg.spacing = 8; evtVlg.childAlignment = TextAnchor.UpperCenter;
            evtVlg.childControlWidth = false; evtVlg.childControlHeight = false;
            evtVlg.childForceExpandWidth = false; evtVlg.childForceExpandHeight = false;
            var evtFitter = evtGroup.gameObject.AddComponent<ContentSizeFitter>();
            evtFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var badge = MakeChild(evtGroup, "Badge_Streak");
            badge.sizeDelta = new Vector2(160, 160);
            var badgeLE = badge.gameObject.AddComponent<LayoutElement>();
            badgeLE.preferredWidth = 160; badgeLE.preferredHeight = 160;
            var badgeBg = badge.gameObject.AddComponent<Image>();
            badgeBg.color = new Color(0.35f, 0.35f, 0.4f, 0.9f);
            ApplySkin(badgeBg, "slot_streak_badge");
            var badgeBtn = badge.gameObject.AddComponent<Button>();
            badgeBtn.targetGraphic = badgeBg;

            var badgeProgTxt = MakeChild(evtGroup, "Txt_Progress");
            badgeProgTxt.sizeDelta = new Vector2(160, 32);
            var bpLE = badgeProgTxt.gameObject.AddComponent<LayoutElement>();
            bpLE.preferredWidth = 160; bpLE.preferredHeight = 32;
            var bpTmp = badgeProgTxt.gameObject.AddComponent<TextMeshProUGUI>();
            bpTmp.text = ""; bpTmp.fontSize = 16;
            bpTmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            bpTmp.alignment = TextAlignmentOptions.Midline;

            var streakBadge = badge.gameObject.AddComponent<StreakChallengeBadge>();
            Assign(streakBadge, "progressText", bpTmp);

            // Card_Event (hidden by default)
            var cardEvent = MakeChild(tab, "Card_Event");
            SetAnchor(cardEvent, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            cardEvent.offsetMin = new Vector2(32, 32);
            cardEvent.offsetMax = new Vector2(-32, 252);
            var evtImg = cardEvent.gameObject.AddComponent<Image>();
            evtImg.color = BgSurface;
            ApplySkin(evtImg, "slot_event_banner");
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
            endsTmp.alignment = TextAlignmentOptions.Midline;
            cardEvent.gameObject.SetActive(false);

            return tab;
        }

        static RectTransform BuildShopTab(RectTransform parent)
        {
            var tab = MakeChild(parent, "Tab_Shop");
            Stretch(tab);
            tab.gameObject.SetActive(false);

            // Inventory strip — anchored at top, above the scroll viewport
            var strip = MakeChild(tab, "Row_InventoryStrip");
            SetAnchor(strip, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            strip.sizeDelta = new Vector2(0, 120);
            strip.anchoredPosition = Vector2.zero;
            var stripHlg = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
            stripHlg.spacing = 8; stripHlg.padding = new RectOffset(16, 16, 8, 8);
            stripHlg.childAlignment = TextAnchor.MiddleCenter;
            stripHlg.childControlWidth = false; stripHlg.childControlHeight = true;
            strip.gameObject.AddComponent<ShopInventoryStrip>();

            var scroll = tab.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;

            // Viewport sits below the inventory strip (top offset = 120 strip + 8 padding)
            var viewport = MakeChild(tab, "Viewport");
            Stretch(viewport, 0, 128, 0, 8);
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 1f);
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
            balLbl.sizeDelta = new Vector2(0, 36);
            var balLblTmp = balLbl.gameObject.AddComponent<TextMeshProUGUI>();
            balLblTmp.fontSize = 26; balLblTmp.color = TextMuted;
            balLbl.gameObject.AddComponent<LocalizedText>().SetStringId("shop.balance_label");
            balLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            var balTxt = MakeChild(balanceRow, "Txt_Balance");
            balTxt.sizeDelta = new Vector2(176, 36);
            var balTmp = balTxt.gameObject.AddComponent<TextMeshProUGUI>();
            balTmp.text = "0"; balTmp.fontSize = 30; balTmp.fontStyle = FontStyles.Bold;
            balTmp.color = TextCol; balTmp.alignment = TextAlignmentOptions.MidlineRight;
            balTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 176;

            var header = MakeChild(content, "Header_Stamina");
            header.sizeDelta = new Vector2(0, 52);
            var hTmp = header.gameObject.AddComponent<TextMeshProUGUI>();
            hTmp.fontSize = 26; hTmp.color = TextMuted; hTmp.fontStyle = FontStyles.Bold;
            header.gameObject.AddComponent<LocalizedText>().SetStringId("shop.section_stamina");
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;

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
            reTmp.alignment = TextAlignmentOptions.Midline;
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
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 1f);
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
            rtTmp.alignment = TextAlignmentOptions.MidlineLeft;
            rankTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 64;

            var youTxt = MakeChild(myRank, "Txt_You");
            youTxt.sizeDelta = new Vector2(0, 48);
            var ytTmp = youTxt.gameObject.AddComponent<TextMeshProUGUI>();
            ytTmp.fontSize = 28; ytTmp.fontStyle = FontStyles.Bold; ytTmp.color = AccentB;
            ytTmp.alignment = TextAlignmentOptions.MidlineLeft;
            youTxt.gameObject.AddComponent<LocalizedFont>();
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
            txt.fontSize = 28; txt.color = TextCol; txt.alignment = TextAlignmentOptions.Midline;
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
            go.gameObject.AddComponent<ProjectLink.Core.ButtonPressEffect>();
            var vlg = go.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.padding = new RectOffset(0, 0, 0, 24);
            go.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var icon = MakeChild(go, "Icon");
            SetAnchor(icon, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1));
            icon.sizeDelta = new Vector2(64, 64);
            icon.anchoredPosition = new Vector2(0, -18);
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.color = isDefault ? TextCol : TextMuted;
            ApplySkin(iconImg, iconSkinKey);
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 64;

            var txt = MakeChild(go, "Txt");
            SetAnchor(txt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            txt.sizeDelta = new Vector2(140, 32);
            txt.anchoredPosition = new Vector2(0, 16);
            var tmpTxt = txt.gameObject.AddComponent<TextMeshProUGUI>();
            tmpTxt.fontSize = 26; tmpTxt.color = isDefault ? TextCol : TextMuted;
            tmpTxt.alignment = TextAlignmentOptions.Midline;
            txt.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);

            return btn;
        }

        // ─── Game ─────────────────────────────────────────────────────────

        static void BuildGame(RectTransform safe, GameObject canvasRoot, RuntimeNavigationButtons router)
        {
            // HUD_Top
            var hud = MakeChild(safe, "HUD_Top");
            SetAnchor(hud, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            hud.sizeDelta = new Vector2(0, 196);
            var hudImg = hud.gameObject.AddComponent<Image>();
            hudImg.color = HudBg;
            var hudVlg = hud.gameObject.AddComponent<VerticalLayoutGroup>();
            hudVlg.spacing = 8; hudVlg.padding = new RectOffset(24, 24, 20, 16);
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
            ApplySkin(pauseImg, "btn_icon_pause");
            var pauseButton = pauseBtn.gameObject.AddComponent<Button>();
            pauseButton.targetGraphic = pauseImg;
            UnityEventTools.AddPersistentListener(pauseButton.onClick, router.OpenPausePopup);
            pauseBtn.gameObject.AddComponent<ProjectLink.Core.ButtonPressEffect>();
            var pauseLE = pauseBtn.gameObject.AddComponent<LayoutElement>();
            pauseLE.preferredWidth = 80; pauseLE.preferredHeight = 80; pauseLE.flexibleWidth = 0;

            var stageLbl = MakeChild(topBar, "Txt_Stage");
            stageLbl.sizeDelta = new Vector2(0, 64);
            var stTmp = stageLbl.gameObject.AddComponent<TextMeshProUGUI>();
            stTmp.text = "Stage 1"; stTmp.fontSize = 36; stTmp.fontStyle = FontStyles.Bold;
            stTmp.color = TextCol; stTmp.alignment = TextAlignmentOptions.Midline;
            stageLbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var timer = MakeChild(topBar, "Txt_Timer");
            timer.sizeDelta = new Vector2(176, 64);
            var timerTmp = timer.gameObject.AddComponent<TextMeshProUGUI>();
            timerTmp.text = ""; timerTmp.fontSize = 36; timerTmp.fontStyle = FontStyles.Bold;
            timerTmp.color = Warning; timerTmp.alignment = TextAlignmentOptions.MidlineRight;
            timer.gameObject.AddComponent<LayoutElement>().preferredWidth = 176;

            // Row_Objectives
            var objRow = MakeChild(hud, "Row_Objectives");
            objRow.sizeDelta = new Vector2(0, 56);
            var objHlg = objRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            objHlg.spacing = 16; objHlg.childAlignment = TextAnchor.MiddleLeft;
            objHlg.childControlWidth = true; objHlg.childControlHeight = true;
            objRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;

            var pipeTxt = MakeChild(objRow, "Txt_Pipe");
            pipeTxt.sizeDelta = new Vector2(0, 44);
            var pipeTmp = pipeTxt.gameObject.AddComponent<TextMeshProUGUI>();
            pipeTmp.text = "0 / 0"; pipeTmp.fontSize = 30; pipeTmp.fontStyle = FontStyles.Bold;
            pipeTmp.color = Positive; pipeTmp.alignment = TextAlignmentOptions.MidlineLeft;
            pipeTmp.raycastTarget = false;
            var pipeLE = pipeTxt.gameObject.AddComponent<LayoutElement>();
            pipeLE.flexibleWidth = 1;
            pipeTxt.gameObject.AddComponent<LocalizedFont>();

            var moveTxt = MakeChild(objRow, "Txt_Moves");
            moveTxt.sizeDelta = new Vector2(220, 44);
            var moveTmp = moveTxt.gameObject.AddComponent<TextMeshProUGUI>();
            moveTmp.text = ""; moveTmp.fontSize = 28; moveTmp.fontStyle = FontStyles.Bold;
            moveTmp.color = TextMuted; moveTmp.alignment = TextAlignmentOptions.MidlineRight;
            moveTxt.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;

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

            var itemBtns   = new Button[4];
            var itemCounts = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                var slot = MakeChild(toolbar, $"ItemSlot_{i + 1}");
                slot.sizeDelta = new Vector2(0, 140);
                var slotImg = slot.gameObject.AddComponent<Image>();
                slotImg.color = HexColor("#16213EE0");
                var slotVlg = slot.gameObject.AddComponent<VerticalLayoutGroup>();
                slotVlg.spacing = 4; slotVlg.padding = new RectOffset(8, 8, 8, 8);
                slotVlg.childAlignment = TextAnchor.MiddleCenter;
                slotVlg.childControlWidth = true; slotVlg.childControlHeight = true;
                slotVlg.childForceExpandWidth = true; slotVlg.childForceExpandHeight = false;
                var slotBtn = slot.gameObject.AddComponent<Button>();
                slotBtn.targetGraphic = slotImg;
                slot.gameObject.AddComponent<ProjectLink.Core.ButtonPressEffect>();
                slot.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                var iconGo = MakeChild(slot, "Img_Icon");
                iconGo.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
                var iconImg = iconGo.gameObject.AddComponent<Image>();
                iconImg.preserveAspect = true; iconImg.raycastTarget = false;
                ApplySkin(iconImg, $"btn_icon_item_{i + 1}");

                var countGo = MakeChild(slot, "Txt_Count");
                countGo.gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
                var countTmp = countGo.gameObject.AddComponent<TextMeshProUGUI>();
                countTmp.text = "0"; countTmp.fontSize = 22; countTmp.fontStyle = FontStyles.Bold;
                countTmp.color = TextCol; countTmp.alignment = TextAlignmentOptions.Midline;
                countTmp.raycastTarget = false;
                countGo.gameObject.AddComponent<LocalizedFont>();

                itemBtns[i]   = slotBtn;
                itemCounts[i] = countTmp;
            }

            // Layers
            AddPopupLayer(canvasRoot, 100);
            AddToastLayer(canvasRoot, 200);

            // Controller refs
            var ctrl = safe.gameObject.AddComponent<GameWireframeController>();
            Assign(ctrl, "levelLabelText", stTmp);
            Assign(ctrl, "pipeCounterText", pipeTmp);
            Assign(ctrl, "moveCounterText", moveTmp);
            Assign(ctrl, "item1Button",    itemBtns[0]);
            Assign(ctrl, "item2Button",    itemBtns[1]);
            Assign(ctrl, "item3Button",    itemBtns[2]);
            Assign(ctrl, "item4Button",    itemBtns[3]);
            Assign(ctrl, "item1CountText", itemCounts[0]);
            Assign(ctrl, "item2CountText", itemCounts[1]);
            Assign(ctrl, "item3CountText", itemCounts[2]);
            Assign(ctrl, "item4CountText", itemCounts[3]);
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
            Assign(popup, "closeIconButton",  FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "closeButton",      FindButtonInChildren(root, "Btn_Cancel"));
            Assign(popup, "saveButton",       FindButtonInChildren(root, "Btn_Save"));
            Assign(popup, "toggleOnSprite",   LoadSkin()?.Get("slot_toggle_on"));
            Assign(popup, "toggleOffSprite",  LoadSkin()?.Get("slot_toggle_off"));
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
                24, TextMuted, TextAlignmentOptions.Midline);
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
            starRow.sizeDelta = new Vector2(0, 96);
            var starHlgD = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            starHlgD.childAlignment = TextAnchor.MiddleCenter;
            starHlgD.spacing = 24;
            starHlgD.childControlWidth = false;
            starHlgD.childControlHeight = false;
            starHlgD.childForceExpandWidth = false;
            starHlgD.childForceExpandHeight = false;
            starRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            for (int si = 0; si < 3; si++)
            {
                var starSlot = MakeChild(starRow, $"Img_Star_{si}");
                starSlot.sizeDelta = new Vector2(96, 96);
                var starImg = starSlot.gameObject.AddComponent<Image>();
                starImg.color = new Color(1f, 1f, 1f, 0.18f);
                ApplySkin(starImg, "slot_star_off");
            }
            AddFooterButton(footer, "Btn_Play", "common.play", "btn_primary", isPrimary: true);
            var popup = root.GetComponent<StageDetailPopup>();
            Assign(popup, "btnClose",      FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "btnPlay",       FindButtonInChildren(root, "Btn_Play"));
            Assign(popup, "starRow",       starRow);
            Assign(popup, "starOnSprite",  LoadSkin()?.Get("slot_star_on"));
            Assign(popup, "starOffSprite", LoadSkin()?.Get("slot_star_off"));
            SavePopupPrefab(root, "StageDetailPopup");
        }

        static void BuildStreakChallengePopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<StreakChallengePopup>(
                "StreakChallengePopup", "streak.popup.title", dismissible: true);

            panel.sizeDelta = new Vector2(960, 0);
            var panelVlg = panel.GetComponent<VerticalLayoutGroup>();
            panelVlg.spacing = 14;
            panelVlg.padding = new RectOffset(36, 36, 26, 34);

            var header = FindRectInChildren(panel, "Header");
            if (header != null)
            {
                var infoBtn = MakeChild(header, "Btn_Info");
                SetAnchor(infoBtn, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f));
                infoBtn.sizeDelta = new Vector2(80, 80);
                infoBtn.anchoredPosition = new Vector2(20, 0);
                var infoImg = infoBtn.gameObject.AddComponent<Image>();
                infoImg.color = HexColor("#26A6FFEE");
                ApplySkin(infoImg, "btn_icon_info");
                var infoButton = infoBtn.gameObject.AddComponent<Button>();
                infoButton.targetGraphic = infoImg;
                infoBtn.gameObject.AddComponent<ProjectLink.Core.ButtonPressEffect>();

                var infoLabel = AddLocalizedLabel(infoBtn, "Txt_InfoGlyph", "", Vector2.zero,
                    Vector2.zero, 42f, TextAlignmentOptions.Midline, FontStyles.Bold);
                infoLabel.text = "i";
            }

            var banner = MakeChild(content, "Img_Banner");
            banner.sizeDelta = new Vector2(780, 230);
            banner.gameObject.AddComponent<LayoutElement>().preferredHeight = 230;
            var bannerImg = banner.gameObject.AddComponent<Image>();
            bannerImg.color = HexColor("#248DFF66");
            ApplySkin(bannerImg, "slot_streak_banner");
            if (bannerImg.sprite == null) ApplySkin(bannerImg, "slot_event_banner");

            var timerBadge = MakeChild(content, "Slot_TimerBadge");
            timerBadge.sizeDelta = new Vector2(330, 72);
            timerBadge.gameObject.AddComponent<LayoutElement>().preferredHeight = 72;
            var timerBg = timerBadge.gameObject.AddComponent<Image>();
            timerBg.color = HexColor("#FFE6BDEE");
            ApplySkin(timerBg, "slot_streak_time_badge");
            AddLocalizedLabel(timerBadge, "Txt_Timer", "", Vector2.zero, Vector2.zero,
                34f, TextAlignmentOptions.Midline, FontStyles.Bold);

            var levelTxt = MakeText(content, "Txt_Level", "streak.level_progress_fmt",
                40, TextCol, TextAlignmentOptions.Midline);
            levelTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;

            var prize = MakeChild(content, "Panel_Prize");
            prize.sizeDelta = new Vector2(760, 154);
            prize.gameObject.AddComponent<LayoutElement>().preferredHeight = 154;
            var prizeImg = prize.gameObject.AddComponent<Image>();
            prizeImg.color = HexColor("#7B2FBEF5");
            ApplySkin(prizeImg, "slot_streak_prize_panel");

            var prizeTitle = AddLocalizedLabel(prize, "Txt_PrizeTitle", "streak.grand_prize",
                new Vector2(0, 42), new Vector2(690, 44), 27f,
                TextAlignmentOptions.Midline, FontStyles.Bold);
            prizeTitle.color = TextCol;

            var prizeIcon = MakeChild(prize, "Img_PrizeIcon");
            Center(prizeIcon, new Vector2(-132, -28), new Vector2(72, 72));
            var prizeIconImg = prizeIcon.gameObject.AddComponent<Image>();
            prizeIconImg.color = Warning;
            ApplySkin(prizeIconImg, "slot_currency_soft");

            var prizeAmount = AddLocalizedLabel(prize, "Txt_PrizeAmount", "",
                new Vector2(92, -28), new Vector2(420, 78), 46f,
                TextAlignmentOptions.Midline, FontStyles.Bold);
            prizeAmount.color = TextCol;

            var levelList = MakeChild(content, "LevelPath");
            levelList.sizeDelta = new Vector2(0, 660);
            levelList.gameObject.AddComponent<LayoutElement>().preferredHeight = 660;

            var activateBtn = AddFooterButton(footer, "Btn_Claim", "streak.claim", "btn_streak_claim", isPrimary: true);
            if (activateBtn.GetComponent<Image>() != null)
                ApplySkin(activateBtn.GetComponent<Image>(), "btn_claim");

            var infoPopup = MakeChild(panel, "InfoPopup");
            SetAnchor(infoPopup, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            infoPopup.sizeDelta = new Vector2(760, 420);
            infoPopup.anchoredPosition = new Vector2(0, 120);
            var infoPopupImg = infoPopup.gameObject.AddComponent<Image>();
            infoPopupImg.color = HexColor("#0D1B36FA");
            ApplySkin(infoPopupImg, "slot_streak_info_panel");
            AddLocalizedLabel(infoPopup, "Txt_InfoTitle", "streak.info_title",
                new Vector2(0, 134), new Vector2(640, 64), 34f,
                TextAlignmentOptions.Midline, FontStyles.Bold);
            var infoBody = AddLocalizedLabel(infoPopup, "Txt_InfoBody", "streak.info_body",
                new Vector2(0, -24), new Vector2(620, 220), 26f,
                TextAlignmentOptions.TopLeft, FontStyles.Normal);
            infoBody.textWrappingMode = TextWrappingModes.Normal;
            infoPopup.gameObject.SetActive(false);

            var popup = root.GetComponent<StreakChallengePopup>();
            Assign(popup, "closeButton",    FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "infoButton",     FindButtonInChildren(root, "Btn_Info"));
            Assign(popup, "activateButton", activateBtn);
            Assign(popup, "infoPopup",      infoPopup.gameObject);
            Assign(popup, "bannerImage",    bannerImg);
            Assign(popup, "timerText",      FindTmpInChildren(root.GetComponent<RectTransform>(), "Txt_Timer"));
            Assign(popup, "levelText",      levelTxt.GetComponent<TextMeshProUGUI>());
            Assign(popup, "prizeTitleText", prizeTitle);
            Assign(popup, "prizeIcon",      prizeIconImg);
            Assign(popup, "prizeAmountText", prizeAmount);
            Assign(popup, "levelListRoot",  levelList);
            Assign(popup, "pathLineSprite", LoadSkin()?.Get("slot_streak_path_line"));
            Assign(popup, "pathNodeSprite", LoadSkin()?.Get("slot_streak_path_node"));
            Assign(popup, "pathNodeDoneSprite", LoadSkin()?.Get("slot_streak_path_node_done"));
            Assign(popup, "platformSprite", LoadSkin()?.Get("slot_streak_platform"));
            Assign(popup, "softCurrencySprite", LoadSkin()?.Get("slot_currency_soft"));
            Assign(popup, "itemRewardSprite", LoadSkin()?.Get("slot_streak_reward_item"));
            SavePopupPrefab(root, "StreakChallengePopup");
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
                32, TextCol, TextAlignmentOptions.Midline);
            nickTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var joinedTxt = MakeText(content, "Txt_Joined", "popup.account.joined_fmt",
                22, TextMuted, TextAlignmentOptions.Midline);
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
            var starHlgC = starRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            starHlgC.childAlignment = TextAnchor.MiddleCenter;
            starHlgC.spacing = 16;
            starHlgC.childControlWidth = false;
            starHlgC.childControlHeight = false;
            starHlgC.childForceExpandWidth = false;
            starHlgC.childForceExpandHeight = false;
            starRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;
            for (int si = 0; si < 3; si++)
            {
                var starSlot = MakeChild(starRow, $"Img_Star_{si}");
                starSlot.sizeDelta = new Vector2(82, 82);
                var starImg = starSlot.gameObject.AddComponent<Image>();
                starImg.color = new Color(1f, 1f, 1f, 0.2f);
                ApplySkin(starImg, "slot_star_off");
            }
            var stageTxt = MakeText(content, "StageText", "",
                38, TextCol, TextAlignmentOptions.Midline);
            stageTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var scoreTxt = MakeText(content, "ScoreText", "",
                30, TextMuted, TextAlignmentOptions.Midline);
            scoreTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var movesTxt = MakeText(content, "MovesText", "",
                30, TextMuted, TextAlignmentOptions.Midline);
            movesTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;
            var rewardTxt = MakeText(content, "RewardText", "",
                28, TextCol, TextAlignmentOptions.Midline);
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
            Assign(popup, "starRow",      starRow);
            Assign(popup, "starOnSprite",  LoadSkin()?.Get("slot_star_on"));
            Assign(popup, "starOffSprite", LoadSkin()?.Get("slot_star_off"));
            SavePopupPrefab(root, "ClearPopup");
        }

        static void BuildClearNextStageConfirmPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<ClearNextStageConfirmPopup>(
                "ClearNextStageConfirmPopup", "popup.clear.title", dismissible: true);
            AddPopupIcon(panel, Warning);
            var bodyTxt = MakeText(content, "Txt_Body", "", 26, TextMuted, TextAlignmentOptions.Midline);
            bodyTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            AddFooterButton(footer, "Btn_Cancel", "common.cancel", "btn_secondary");
            AddFooterButton(footer, "Btn_Confirm", "common.confirm", "btn_primary", isPrimary: true);
            var popup = root.GetComponent<ClearNextStageConfirmPopup>();
            Assign(popup, "closeIconButton", FindButtonInChildren(root, "Btn_Close"));
            Assign(popup, "cancelButton", FindButtonInChildren(root, "Btn_Cancel"));
            Assign(popup, "confirmButton", FindButtonInChildren(root, "Btn_Confirm"));
            Assign(popup, "bodyText", bodyTxt.GetComponent<TextMeshProUGUI>());
            SavePopupPrefab(root, "ClearNextStageConfirmPopup");
        }

        static void BuildPausePopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<PausePopup>(
                "PausePopup", "popup.pause.title", dismissible: true);

            // Overlay fully opaque so board is not visible while paused
            var overlayImg = root.transform.Find("Overlay")?.GetComponent<Image>();
            if (overlayImg != null) overlayImg.color = new Color(0f, 0f, 0f, 1f);

            // Footer: replace HLG with properly configured VLG
            Object.DestroyImmediate(footer.gameObject.GetComponent<HorizontalLayoutGroup>());
            var vlg = footer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var btnResume = AddFooterButton(footer, "Btn_Resume", "common.resume", "btn_primary", isPrimary: true, fullWidth: true);
            var btnRetry  = AddFooterButton(footer, "Btn_Retry",  "popup.pause.retry", "btn_secondary", fullWidth: true);
            var btnLobby  = AddFooterButton(footer, "Btn_Lobby",  "common.lobby", "btn_secondary", fullWidth: true);

            var popup = root.GetComponent<PausePopup>();
            Assign(popup, "btnResume", btnResume);
            Assign(popup, "btnRetry",  btnRetry);
            Assign(popup, "btnLobby",  btnLobby);

            SavePopupPrefab(root, "PausePopup");
        }

        static void BuildTimeoutPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<TimeoutPopup>(
                "TimeoutPopup", "popup.timeout.title", dismissible: false);

            var spine = MakeChild(content, "Slot_Spine");
            spine.sizeDelta = new Vector2(400, 280);
            spine.gameObject.AddComponent<Image>().color = SlotPlaceholder;
            spine.gameObject.AddComponent<LayoutElement>().preferredHeight = 280;

            // Extend section
            var extendGroup = MakeChild(content, "Group_Extend");
            extendGroup.sizeDelta = new Vector2(0, 100);
            var extendVlg = extendGroup.gameObject.AddComponent<VerticalLayoutGroup>();
            extendVlg.spacing = 4; extendVlg.childAlignment = TextAnchor.MiddleCenter;
            extendVlg.childForceExpandWidth = true; extendVlg.childForceExpandHeight = false;
            extendGroup.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;

            var extendDesc = MakeText(extendGroup, "Txt_ExtendDesc", "popup.timeout.extend_desc",
                22, TextMuted, TextAlignmentOptions.Midline);
            extendDesc.gameObject.AddComponent<LayoutElement>().preferredHeight = 36;

            var extendCostRow = MakeChild(extendGroup, "Row_ExtendCost");
            extendCostRow.sizeDelta = new Vector2(0, 48);
            var costHlg = extendCostRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            costHlg.spacing = 8; costHlg.childAlignment = TextAnchor.MiddleCenter;
            costHlg.childForceExpandWidth = false; costHlg.childForceExpandHeight = true;
            extendCostRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;

            var extendCost = MakeText(extendCostRow, "Txt_ExtendCost", "",
                28, Warning, TextAlignmentOptions.Midline);
            extendCost.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            var extendCostLe = extendCost.gameObject.AddComponent<LayoutElement>();
            extendCostLe.preferredWidth = 120; extendCostLe.preferredHeight = 40;

            var extendBtn = AddFooterButton(content, "Btn_Extend", "popup.timeout.extend_desc", "btn_primary", isPrimary: true);

            AddFooterButton(footer, "Btn_Retry", "common.retry", "btn_secondary");
            AddFooterButton(footer, "Btn_Lobby", "common.lobby", "btn_secondary");

            var rootRect = root.GetComponent<RectTransform>();
            var popup = root.GetComponent<TimeoutPopup>();
            Assign(popup, "btnRetry",     FindButtonInChildren(root, "Btn_Retry"));
            Assign(popup, "btnLobby",     FindButtonInChildren(root, "Btn_Lobby"));
            Assign(popup, "btnExtend",    extendBtn);
            Assign(popup, "txtExtendDesc", FindTmpInChildren(rootRect, "Txt_ExtendDesc"));
            Assign(popup, "txtExtendCost", FindTmpInChildren(rootRect, "Txt_ExtendCost"));
            SavePopupPrefab(root, "TimeoutPopup");
        }

        static void BuildConfirmPopupPrefab<T>(string prefabName, string titleKey, string bodyKey)
            where T : PopupBase
        {
            var (root, panel, content, footer) = CreatePopupShell<T>(
                prefabName, titleKey, dismissible: true);
            var bodyTxt = MakeText(content, "Txt_Body", bodyKey, 30, TextMuted, TextAlignmentOptions.Midline);
            bodyTxt.gameObject.AddComponent<LayoutElement>().preferredHeight = 128;
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
            ApplySkin(ovImg, "slot_popup_overlay", false);
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
            ApplySkin(panelImg, "slot_popup_bg", false);
            var panelVlg = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelVlg.spacing = 20; panelVlg.padding = new RectOffset(40, 40, 40, 44);
            panelVlg.childAlignment = TextAnchor.UpperCenter;
            panelVlg.childControlWidth = true; panelVlg.childControlHeight = true;
            panelVlg.childForceExpandWidth = true; panelVlg.childForceExpandHeight = false;
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // Header — absolute layout: Txt_Title centered, Btn_Close top-right
            var header = MakeChild(panel, "Header");
            header.sizeDelta = new Vector2(0, 96);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;

            var titleGo = MakeChild(header, "Txt_Title");
            Stretch(titleGo, dismissible ? 80 : 0, 0, dismissible ? 80 : 0, 0);
            var titleTmp = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            titleTmp.fontSize = 42; titleTmp.fontStyle = FontStyles.Bold; titleTmp.color = TextCol;
            titleTmp.alignment = TextAlignmentOptions.Midline;
            titleGo.gameObject.AddComponent<LocalizedText>().SetStringId(titleKey);

            if (dismissible)
            {
                var closeBtn = MakeChild(header, "Btn_Close");
                SetAnchor(closeBtn, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f));
                closeBtn.sizeDelta = new Vector2(80, 80);
                closeBtn.anchoredPosition = new Vector2(-20, 0);
                var closeImg = closeBtn.gameObject.AddComponent<Image>();
                closeImg.color = HexColor("#FFFFFF30");
                ApplySkin(closeImg, "btn_icon_close");
                var closeBtnComp = closeBtn.gameObject.AddComponent<Button>();
                closeBtnComp.targetGraphic = closeImg;
                if (closeBtn.gameObject.GetComponent<ProjectLink.Core.ButtonPressEffect>() == null)
                    closeBtn.gameObject.AddComponent<ProjectLink.Core.ButtonPressEffect>();
            }

            // Divider top
            var divTop = MakeChild(panel, "Divider_Top");
            divTop.sizeDelta = new Vector2(0, 3);
            divTop.gameObject.AddComponent<Image>().color = HexColor("#FFFFFF28");
            divTop.gameObject.AddComponent<LayoutElement>().preferredHeight = 3;

            // Content
            var content = MakeChild(panel, "Content");
            content.sizeDelta = new Vector2(0, 0);
            var contentVlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 20; contentVlg.childAlignment = TextAnchor.UpperCenter;
            contentVlg.childControlWidth = true; contentVlg.childControlHeight = true;
            contentVlg.childForceExpandWidth = true; contentVlg.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            // Divider bot
            var divBot = MakeChild(panel, "Divider_Bot");
            divBot.sizeDelta = new Vector2(0, 3);
            divBot.gameObject.AddComponent<Image>().color = HexColor("#FFFFFF28");
            divBot.gameObject.AddComponent<LayoutElement>().preferredHeight = 3;

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
            var go = MakeText(parent, "Txt_Body", stringKey, 30, TextMuted, TextAlignmentOptions.Midline);
            go.gameObject.AddComponent<LayoutElement>().preferredHeight = 168;
            return go.gameObject;
        }

        static Button AddFooterButton(RectTransform parent, string name, string labelKey,
            string skinKey, bool isPrimary = false, bool fullWidth = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 120);
            var img = go.GetComponent<Image>();
            img.color = isPrimary ? CtaPrimary : CtaSecondary;
            ApplySkin(img, skinKey);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<ProjectLink.Core.ButtonPressEffect>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 120;
            le.flexibleWidth = 1;
            AddLocalizedLabel(rect, "Txt_Label", labelKey, Vector2.zero,
                Vector2.zero, 32f, TextAlignmentOptions.Midline, FontStyles.Bold);
            return btn;
        }

        static void AddToggleRow(RectTransform parent, string name, string labelKey)
        {
            var row = MakeChild(parent, name);
            row.sizeDelta = new Vector2(0, 96);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;

            var lbl = MakeChild(row, "Txt_Label");
            lbl.sizeDelta = new Vector2(0, 48);
            var lblTmp = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            lblTmp.fontSize = 28; lblTmp.color = TextCol;
            lbl.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var toggle = MakeChild(row, "Toggle");
            toggle.sizeDelta = new Vector2(120, 56);
            var toggleBg = toggle.gameObject.AddComponent<Image>();
            toggleBg.color = new Color(0, 0, 0, 0);
            var toggleComp = toggle.gameObject.AddComponent<Toggle>();
            toggleComp.transition = UnityEngine.UI.Selectable.Transition.None;
            toggle.gameObject.AddComponent<LayoutElement>().preferredWidth = 120;

            var imgToggle = MakeChild(toggle, "Img_Toggle");
            Stretch(imgToggle);
            var imgToggleImg = imgToggle.gameObject.AddComponent<Image>();
            imgToggleImg.color = new Color(0.4f, 0.4f, 0.5f, 1f);
            imgToggleImg.preserveAspect = false;
            ApplySkin(imgToggleImg, "slot_toggle_off", false);
            toggleComp.targetGraphic = imgToggleImg;
        }

        static void AddDropdownRow(RectTransform parent, string name, string labelKey)
        {
            var row = MakeChild(parent, name);
            row.sizeDelta = new Vector2(0, 96);
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false; hlg.childControlHeight = true;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 96;

            var lbl = MakeChild(row, "Txt_Label");
            lbl.sizeDelta = new Vector2(0, 48);
            var lblTmp = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            lblTmp.fontSize = 28; lblTmp.color = TextCol;
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
            ApplySkin(iconImg, iconSkinKey);
            icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;

            var nameGo = MakeChild(row, "Txt_Name");
            nameGo.sizeDelta = new Vector2(0, 48);
            var nameTmp = nameGo.gameObject.AddComponent<TextMeshProUGUI>();
            nameTmp.text = labelText; nameTmp.fontSize = 28; nameTmp.color = TextCol;
            nameTmp.fontStyle = FontStyles.Bold;
            nameGo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var linkBtn = MakeChild(row, "Btn_LinkState");
            linkBtn.sizeDelta = new Vector2(200, 72);
            var linkImg = linkBtn.gameObject.AddComponent<Image>();
            linkImg.color = BgSurface;
            ApplySkin(linkImg, "btn_secondary");
            var linkBtnComp = linkBtn.gameObject.AddComponent<Button>();
            linkBtnComp.targetGraphic = linkImg;
            linkBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
        }

        static void BuildShopItemConfirmPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<ShopItemConfirmPopup>(
                "ShopItemConfirmPopup", "shop.confirm.title", dismissible: true);

            // Description (localized from item.description_key)
            var descGo = MakeChild(content, "Txt_Description");
            descGo.gameObject.AddComponent<LayoutElement>().preferredHeight = 68;
            var descTmp = descGo.gameObject.AddComponent<TextMeshProUGUI>();
            descTmp.fontSize = 26; descTmp.color = TextMuted;
            descTmp.alignment = TextAlignmentOptions.Midline;
            descTmp.raycastTarget = false;
            descTmp.enableWordWrapping = true;
            descGo.gameObject.AddComponent<LocalizedFont>();

            // Item name row
            var nameRow = MakeChild(content, "Row_ItemName");
            nameRow.sizeDelta = new Vector2(0, 56);
            nameRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var nameRowHlg = nameRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            nameRowHlg.spacing = 8; nameRowHlg.childControlWidth = true; nameRowHlg.childControlHeight = true;
            var nameTxt = MakeChild(nameRow, "Txt_ItemName");
            nameTxt.sizeDelta = new Vector2(0, 48);
            var nameTmp = nameTxt.gameObject.AddComponent<TextMeshProUGUI>();
            nameTmp.fontSize = 30; nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.color = TextCol; nameTmp.alignment = TextAlignmentOptions.Midline;
            nameTmp.raycastTarget = false;
            nameTxt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            nameTxt.gameObject.AddComponent<LocalizedFont>();

            // Balance row
            AddShopConfirmRow(content, "Row_Balance", "shop.confirm.balance_label", "Txt_Balance");
            // Cost row
            AddShopConfirmRow(content, "Row_Cost", "shop.confirm.cost_label", "Txt_Cost");
            // After row
            AddShopConfirmRow(content, "Row_After", "shop.confirm.after_label", "Txt_After");

            AddFooterButton(footer, "Btn_Cancel", "common.cancel",  "btn_secondary");
            AddFooterButton(footer, "Btn_Buy",    "common.confirm", "btn_primary", isPrimary: true);

            var popup = root.GetComponent<ShopItemConfirmPopup>();
            var rootRect = root.GetComponent<RectTransform>();
            Assign(popup, "txtDescription", FindTmpInChildren(rootRect, "Txt_Description"));
            Assign(popup, "txtItemName", FindTmpInChildren(rootRect, "Txt_ItemName"));
            Assign(popup, "txtBalance",  FindTmpInChildren(rootRect, "Txt_Balance"));
            Assign(popup, "txtCost",     FindTmpInChildren(rootRect, "Txt_Cost"));
            Assign(popup, "txtAfter",    FindTmpInChildren(rootRect, "Txt_After"));
            Assign(popup, "btnBuy",    FindButtonInChildren(root, "Btn_Buy"));
            Assign(popup, "btnCancel", FindButtonInChildren(root, "Btn_Cancel"));
            SavePopupPrefab(root, "ShopItemConfirmPopup");
        }

        static void AddShopConfirmRow(RectTransform parent, string rowName, string labelKey, string valueName)
        {
            var row = MakeChild(parent, rowName);
            row.sizeDelta = new Vector2(0, 56);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 56;
            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; hlg.padding = new RectOffset(0, 0, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            var lbl = MakeChild(row, $"Lbl_{labelKey.Replace(".", "_")}");
            lbl.sizeDelta = new Vector2(0, 44);
            var lblTmp = lbl.gameObject.AddComponent<TextMeshProUGUI>();
            lblTmp.fontSize = 26; lblTmp.color = TextMuted; lblTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lblTmp.raycastTarget = false;
            lbl.gameObject.AddComponent<LocalizedText>().SetStringId(labelKey);
            lbl.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

            var val = MakeChild(row, valueName);
            val.sizeDelta = new Vector2(240, 44);
            var valTmp = val.gameObject.AddComponent<TextMeshProUGUI>();
            valTmp.fontSize = 28; valTmp.fontStyle = FontStyles.Bold;
            valTmp.color = TextCol; valTmp.alignment = TextAlignmentOptions.MidlineRight;
            valTmp.raycastTarget = false;
            val.gameObject.AddComponent<LayoutElement>().preferredWidth = 220;
            val.gameObject.AddComponent<LocalizedFont>();
        }

        static void BuildShopItemResultPopup()
        {
            var (root, panel, content, footer) = CreatePopupShell<ShopItemResultPopup>(
                "ShopItemResultPopup", "shop.result.success_title", dismissible: true);

            AddPopupBodyText(content, null);
            // Rename body text for reliable ref assignment
            var bodyGo = FindTmpInChildren(root.GetComponent<RectTransform>(), "Txt_Body");

            AddFooterButton(footer, "Btn_Confirm", "common.confirm", "btn_primary", isPrimary: true);

            var popup = root.GetComponent<ShopItemResultPopup>();
            Assign(popup, "txtTitle",   FindTmpInChildren(root.GetComponent<RectTransform>(), "Txt_Title"));
            if (bodyGo != null) Assign(popup, "txtBody", bodyGo);
            Assign(popup, "btnConfirm", FindButtonInChildren(root, "Btn_Confirm"));
            SavePopupPrefab(root, "ShopItemResultPopup");
        }

        static void CreateShopProductCardPrefab()
        {
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder("Assets/Resources/Prefabs/UI");

            var root = new GameObject("ShopProductCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rootRect = root.GetComponent<RectTransform>();
            SetAnchor(rootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rootRect.sizeDelta = new Vector2(280f, 500f);
            root.GetComponent<Image>().color = new Color(0.06f, 0.16f, 0.28f, 0.9f);
            var rootBtn = root.GetComponent<Button>();
            rootBtn.targetGraphic = root.GetComponent<Image>();
            var rootLe = root.GetComponent<LayoutElement>();
            rootLe.preferredWidth = 280f; rootLe.preferredHeight = 500f;

            // Txt_Title — top
            var titleGo = MakeChild(rootRect, "Txt_Title");
            SetAnchor(titleGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1));
            titleGo.sizeDelta = new Vector2(0, 88f);
            titleGo.anchoredPosition = new Vector2(0, -36f);
            var titleTmp = titleGo.gameObject.AddComponent<TextMeshProUGUI>();
            titleTmp.fontSize = 46; titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.82f, 0.08f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Midline;
            titleTmp.raycastTarget = false;
            titleGo.gameObject.AddComponent<LocalizedFont>();

            // Img_ItemIcon — center
            var iconGo = MakeChild(rootRect, "Img_ItemIcon");
            SetAnchor(iconGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            iconGo.sizeDelta = new Vector2(190f, 190f);
            iconGo.anchoredPosition = new Vector2(0, 16f);
            var iconImg = iconGo.gameObject.AddComponent<Image>();
            iconImg.color = new Color(1f, 0.75f, 0.02f, 0.8f);
            iconImg.preserveAspect = true;
            ApplySkin(iconImg, "slot_item_icon");

            // Txt_Price — bottom
            var priceGo = MakeChild(rootRect, "Txt_Price");
            SetAnchor(priceGo, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0));
            priceGo.sizeDelta = new Vector2(-36f, 96f);
            priceGo.anchoredPosition = new Vector2(0, 26f);
            var priceTmp = priceGo.gameObject.AddComponent<TextMeshProUGUI>();
            priceTmp.fontSize = 36; priceTmp.fontStyle = FontStyles.Bold;
            priceTmp.color = Color.white;
            priceTmp.alignment = TextAlignmentOptions.Midline;
            priceTmp.raycastTarget = false;
            priceGo.gameObject.AddComponent<LocalizedFont>();

            var card = root.AddComponent<ShopProductCard>();
            Assign(card, "txtTitle",    FindTmpInChildren(rootRect, "Txt_Title"));
            var iconImgRef = FindRectInChildren(rootRect, "Img_ItemIcon");
            if (iconImgRef != null) Assign(card, "imgItemIcon", iconImgRef.GetComponent<Image>());
            Assign(card, "txtPrice",    FindTmpInChildren(rootRect, "Txt_Price"));
            Assign(card, "btnCard",     rootBtn);

            EnsureLocalizedFonts(root);
            AddAnimatorToIconImages(root);

            string cardPath = $"{PopupPrefabRoot}/ShopProductCard.prefab";
            string prevCard = System.IO.File.Exists(cardPath) ? System.IO.File.ReadAllText(cardPath) : null;
            PrefabUtility.SaveAsPrefabAsset(root, cardPath);
            Object.DestroyImmediate(root);
            RestoreIfUnchanged(cardPath, prevCard);
        }

        static void SavePopupPrefab(GameObject root, string prefabName)
        {
            NormalizeLayoutText(root);
            EnsureLocalizedFonts(root);
            AddAnimatorToIconImages(root);

            string path = $"{PopupPrefabRoot}/{prefabName}.prefab";
            string prev = System.IO.File.Exists(path) ? System.IO.File.ReadAllText(path) : null;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            RestoreIfUnchanged(path, prev);
        }

        // ─── Shared canvas / layer helpers ───────────────────────────────

        static (GameObject canvas, RectTransform safe) CreateSceneCanvas(string canvasName, bool includeBackground = true)
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

            if (includeBackground)
            {
                var bg = new GameObject("Panel_Background", typeof(RectTransform), typeof(Image));
                bg.transform.SetParent(root.transform, false);
                Stretch(bg.GetComponent<RectTransform>());
                bg.GetComponent<Image>().color = BgPrimary;
                bg.GetComponent<Image>().raycastTarget = false;
            }

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
            ApplySkin(img, name.ToLower().Replace("_", "").Replace("slot", "slot_"));
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
            img.color = skinKey == "btn_primary" ? CtaPrimary
                      : skinKey == "btn_secondary" ? CtaSecondary
                      : AccentA;
            ApplySkin(img, skinKey);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            go.AddComponent<ProjectLink.Core.ButtonPressEffect>();

            AddLocalizedLabel(rect, "Txt_Label", labelKey, Vector2.zero,
                Vector2.zero, fontSize, TextAlignmentOptions.Midline, FontStyles.Bold);

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
            ApplySkin(img, skinKey);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<ProjectLink.Core.ButtonPressEffect>();
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
            TextAlignmentOptions alignment = TextAlignmentOptions.Midline,
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

        static void EnsureLocalizedFonts(GameObject root)
        {
            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.GetComponent<LocalizedText>() != null) continue;
                if (tmp.GetComponent<LocalizedFont>() != null) continue;
                tmp.gameObject.AddComponent<LocalizedFont>();
            }
        }

        static void NormalizeLayoutText(GameObject root)
        {
            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.transform.parent == null || tmp.transform.parent.GetComponent<LayoutGroup>() == null)
                    continue;

                var rect = tmp.rectTransform;
                if (Mathf.Approximately(rect.sizeDelta.x, 0f))
                    rect.sizeDelta = new Vector2(EstimateTextWidth(tmp), rect.sizeDelta.y);

                var element = tmp.GetComponent<LayoutElement>();
                if (element == null)
                    element = tmp.gameObject.AddComponent<LayoutElement>();

                if (element.preferredWidth <= 0f)
                    element.preferredWidth = EstimateTextWidth(tmp);

                if (element.flexibleWidth <= 0f)
                    element.flexibleWidth = 1f;
            }
        }

        static float EstimateTextWidth(TextMeshProUGUI tmp)
        {
            string text = string.IsNullOrEmpty(tmp.text) ? "Text" : tmp.text;
            float fontSize = tmp.fontSize > 0f ? tmp.fontSize : 28f;
            return Mathf.Clamp(text.Length * fontSize * 0.6f, 80f, 420f);
        }

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

        static UISpriteSkin LoadSkin() =>
            _skin ??= AssetDatabase.LoadAssetAtPath<UISpriteSkin>(SkinAssetPath);

        static void ApplySkin(Image image, string key, bool preserveAspect = true)
        {
            var sprite = LoadSkin()?.Get(key);
            if (sprite == null) return;
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = preserveAspect;
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

        static void AssignSpriteArray(Object target, string propertyName, Sprite[] sprites)
        {
            if (target == null || sprites == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propertyName);
            if (prop == null || !prop.isArray) return;
            prop.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.objectReferenceValue = sprites[i];
            }
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

        // ─── FileID-stable save helpers ──────────────────────────────────
        // Unity assigns new fileIDs every time objects are destroyed and recreated.
        // These helpers detect whether a rebuild produced only fileID churn (no real
        // structural change) and restore the original file in that case, so git diff
        // only shows genuinely modified scenes/prefabs.

        static bool ContentMatchesIgnoringFileIds(string newContent, string oldContent)
            => NormalizeYamlFileIds(newContent) == NormalizeYamlFileIds(oldContent);

        static string NormalizeYamlFileIds(string yaml)
        {
            var idMap = new System.Collections.Generic.Dictionary<string, string>();
            int counter = 0;

            string MapId(string id)
            {
                if (!idMap.TryGetValue(id, out var n))
                    idMap[id] = n = (++counter).ToString();
                return n;
            }

            // YAML anchors:  --- !u!NNN &1234567890
            yaml = System.Text.RegularExpressions.Regex.Replace(
                yaml, @"&(\d+)", m => $"&{MapId(m.Groups[1].Value)}");
            // fileID references: {fileID: 1234567890, ...}
            yaml = System.Text.RegularExpressions.Regex.Replace(
                yaml, @"fileID: (-?\d+)", m => $"fileID: {MapId(m.Groups[1].Value)}");

            return yaml;
        }

        static void RestoreIfUnchanged(string path, string previousContent)
        {
            if (previousContent == null || !System.IO.File.Exists(path)) return;
            string current = System.IO.File.ReadAllText(path);
            if (!ContentMatchesIgnoringFileIds(current, previousContent)) return;

            System.IO.File.WriteAllText(path, previousContent);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        // ─────────────────────────────────────────────────────────────────

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
