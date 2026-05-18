using System.Collections;
using System.Collections.Generic;
using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class SettingPopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button closeIconButton;
        [SerializeField] Button saveButton;
        [SerializeField] TextMeshProUGUI accountStatusText;
        [SerializeField] Toggle bgmToggle;
        [SerializeField] Toggle sfxToggle;
        [SerializeField] Toggle hapticsToggle;
        [SerializeField] Toggle notifToggle;
        [SerializeField] Sprite toggleOnSprite;
        [SerializeField] Sprite toggleOffSprite;

        bool _initialized;
        readonly Dictionary<Toggle, Coroutine> _toggleAnims = new();

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindOverlayClose();
            BindClose(closeButton);
            BindClose(closeIconButton);
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSave);

            RefreshAccount();
            LoadSettings();
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("CloseButton") ?? FindButton("Btn_Cancel");
            closeIconButton ??= FindButton("CloseIconButton") ?? FindButton("Btn_Close");
            saveButton ??= FindButton("SaveButton") ?? FindButton("Btn_Save");
            accountStatusText ??= FindText("AccountStatusText");
            bgmToggle ??= FindToggleInRow("Row_Bgm");
            sfxToggle ??= FindToggleInRow("Row_Sfx");
            hapticsToggle ??= FindToggleInRow("Row_Haptics");
            notifToggle ??= FindToggleInRow("Row_Notif");

            var dropdown = GetComponentInChildren<TMP_Dropdown>(true);
            if (dropdown != null && dropdown.GetComponent<LanguageSelector>() == null)
                dropdown.gameObject.AddComponent<LanguageSelector>();
        }

        void LoadSettings()
        {
            var dm = DataManager.Instance;
            if (dm == null) { BindToggleListeners(); return; }
            SetToggle(bgmToggle,     dm.SoundVolume > 0f);
            SetToggle(sfxToggle,     dm.SfxVolume > 0f);
            SetToggle(hapticsToggle, dm.HapticEnabled);
            SetToggle(notifToggle,   dm.GetFlag("notifications_enabled", true));
            BindToggleListeners();
        }

        void BindToggleListeners()
        {
            BindToggle(bgmToggle,     v => DataManager.Instance.SoundVolume   = v ? 1f : 0f);
            BindToggle(sfxToggle,     v => DataManager.Instance.SfxVolume     = v ? 1f : 0f);
            BindToggle(hapticsToggle, v => DataManager.Instance.HapticEnabled = v);
            BindToggle(notifToggle,   v => DataManager.Instance.SetFlag("notifications_enabled", v));
        }

        void BindToggle(Toggle toggle, System.Action<bool> onChanged)
        {
            if (toggle == null) return;
            toggle.onValueChanged.AddListener(v =>
            {
                AnimateToggleVisual(toggle, v);
                onChanged?.Invoke(v);
            });
        }

        void AnimateToggleVisual(Toggle toggle, bool isOn)
        {
            if (toggle == null) return;
            if (_toggleAnims.TryGetValue(toggle, out var existing) && existing != null)
                StopCoroutine(existing);
            _toggleAnims[toggle] = StartCoroutine(ToggleAnim(toggle, isOn));
        }

        IEnumerator ToggleAnim(Toggle toggle, bool isOn)
        {
            var rect = toggle.GetComponent<RectTransform>();
            var img  = toggle.transform.Find("Img_Toggle")?.GetComponent<Image>();
            if (rect == null) yield break;

            float t = 0f;
            while (t < 0.07f)
            {
                t += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, 0.82f, t / 0.07f);
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            if (img != null)
            {
                var sprite = isOn ? toggleOnSprite : toggleOffSprite;
                if (sprite != null) { img.sprite = sprite; img.color = Color.white; }
                else img.color = isOn ? new Color(0.2f, 0.8f, 0.4f, 1f) : new Color(0.4f, 0.4f, 0.5f, 1f);
            }

            t = 0f;
            while (t < 0.18f)
            {
                t += Time.unscaledDeltaTime;
                float f = t / 0.18f;
                float s = f < 0.55f
                    ? Mathf.Lerp(0.82f, 1.10f, f / 0.55f)
                    : Mathf.Lerp(1.10f, 1.00f, (f - 0.55f) / 0.45f);
                rect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rect.localScale = Vector3.one;
        }

        void OnSave()
        {
            CloseTop();
        }

        void RefreshAccount()
        {
            var provider = ProjectLink.Core.NetworkManager.Instance?.AuthProvider;
            var label    = string.IsNullOrEmpty(provider) || provider == "guest" || provider == "refresh"
                ? LocalizationManager.Get("popup.account.guest")
                : provider;
            SetText(accountStatusText, label);
        }

        void BindClose(Button button)
        {
            if (button != null)
                button.onClick.AddListener(CloseTop);
        }

        void SetToggle(Toggle toggle, bool value)
        {
            if (toggle == null) return;
            toggle.SetIsOnWithoutNotify(value);
            var img = toggle.transform.Find("Img_Toggle")?.GetComponent<Image>();
            if (img == null) return;
            var sprite = value ? toggleOnSprite : toggleOffSprite;
            if (sprite != null) { img.sprite = sprite; img.color = Color.white; }
            else img.color = value ? new Color(0.2f, 0.8f, 0.4f, 1f) : new Color(0.4f, 0.4f, 0.5f, 1f);
        }

        Toggle FindToggleInRow(string rowName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name != rowName) continue;
                var toggle = rect.GetComponentInChildren<Toggle>(true);
                if (toggle != null) return toggle;
            }
            return null;
        }

        Button FindButton(string buttonName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == buttonName) return button;
            return null;
        }

        TextMeshProUGUI FindText(string labelName)
        {
            foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.name == labelName) return label;
            return null;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }
    }
}
