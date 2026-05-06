using System;
using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public abstract class ConfirmPopupBase : PopupBase
    {
        protected void Build(string title, string message, string confirmLabel, Color accent, Action onConfirm)
        {
            var overlay = gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.62f);

            var panel = AddPanel(transform, "Panel", new Vector2(0f, 0f), new Vector2(720f, 520f), new Color(0.055f, 0.07f, 0.1f, 0.98f));
            AddLocalizedLabel(panel, title, 52, FontStyles.Bold, Color.white, new Vector2(0f, 148f), new Vector2(620f, 82f));
            AddLocalizedLabel(panel, message, 30, FontStyles.Normal, new Color(0.72f, 0.78f, 0.86f, 1f), new Vector2(0f, 54f), new Vector2(590f, 100f));

            var confirm = AddLocalizedButton(panel, "ConfirmButton", confirmLabel, new Vector2(0f, -82f), new Vector2(560f, 90f), accent, new Color(0.06f, 0.08f, 0.12f, 1f));
            var cancel = AddLocalizedButton(panel, "CancelButton", "popup_cancel", new Vector2(0f, -194f), new Vector2(560f, 78f), new Color(1f, 1f, 1f, 0.1f), Color.white);

            confirm.onClick.AddListener(() => onConfirm?.Invoke());
            cancel.onClick.AddListener(() => PopupManager.Instance.CloseTop());
        }

        protected RectTransform AddPanel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return rect;
        }

        protected TextMeshProUGUI AddLabel(Transform parent, string text, float fontSize, FontStyles style, Color color, Vector2 position, Vector2 size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        protected Button AddButton(Transform parent, string name, string text, Vector2 position, Vector2 size, Color background, Color foreground)
        {
            var rect = AddPanel(parent, name, position, size, background);
            var button = rect.gameObject.AddComponent<Button>();
            AddLabel(rect, text, 30, FontStyles.Bold, foreground, Vector2.zero, size);
            return button;
        }

        protected TextMeshProUGUI AddLocalizedLabel(Transform parent, string stringId, float fontSize, FontStyles style, Color color, Vector2 position, Vector2 size)
        {
            var label = AddLabel(parent, stringId, fontSize, style, color, position, size);
            label.gameObject.AddComponent<LocalizedText>().SetStringId(stringId);
            return label;
        }

        protected Button AddLocalizedButton(Transform parent, string name, string stringId, Vector2 position, Vector2 size, Color background, Color foreground)
        {
            var rect = AddPanel(parent, name, position, size, background);
            var button = rect.gameObject.AddComponent<Button>();
            AddLocalizedLabel(rect, stringId, 30, FontStyles.Bold, foreground, Vector2.zero, size);
            return button;
        }
    }
}
