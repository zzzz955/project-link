using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public static class ModernUI
    {
        public static readonly Color Navy = new(0.015f, 0.095f, 0.22f, 1f);
        public static readonly Color NavyPanel = new(0.02f, 0.16f, 0.36f, 0.96f);
        public static readonly Color NavySoft = new(0.035f, 0.22f, 0.46f, 0.92f);
        public static readonly Color Blue = new(0.02f, 0.46f, 0.98f, 1f);
        public static readonly Color Cyan = new(0.02f, 0.78f, 1f, 1f);
        public static readonly Color Green = new(0.22f, 0.88f, 0.16f, 1f);
        public static readonly Color Pink = new(1f, 0.18f, 0.42f, 1f);
        public static readonly Color Purple = new(0.48f, 0.12f, 0.92f, 1f);
        public static readonly Color Yellow = new(1f, 0.78f, 0.02f, 1f);
        public static readonly Color White = new(0.96f, 0.98f, 1f, 1f);
        public static readonly Color Muted = new(0.66f, 0.76f, 0.9f, 1f);

        public static RectTransform AddFullBleed(RectTransform parent, Color color)
        {
            var rect = AddPanel(parent, "Background", Vector2.zero, Vector2.zero, color, false);
            Stretch(rect);
            rect.SetAsFirstSibling();
            return rect;
        }

        public static RectTransform AddResourceImage(RectTransform parent, string name, string resourcePath, Vector2 position, Vector2 size, Color color, bool preserveAspect = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Center(rect, position, size);

            var image = go.GetComponent<Image>();
            image.sprite = Resources.Load<Sprite>(resourcePath);
            image.color = color;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            return rect;
        }

        public static RectTransform AddPanel(RectTransform parent, string name, Vector2 position, Vector2 size, Color color, bool shadow = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Center(rect, position, size);

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            if (shadow)
            {
                var s = go.AddComponent<Shadow>();
                s.effectColor = new Color(0f, 0f, 0f, 0.42f);
                s.effectDistance = new Vector2(0f, -8f);
            }

            return rect;
        }

        public static RectTransform AddCircle(RectTransform parent, string name, Vector2 position, float diameter, Color color, bool shadow = true)
        {
            var rect = AddPanel(parent, name, position, new Vector2(diameter, diameter), color, shadow);
            AddLabel(rect, ".", diameter * 0.82f, FontStyles.Bold, color, new Vector2(0f, diameter * 0.18f), new Vector2(diameter, diameter), TextAlignmentOptions.Center);
            return rect;
        }

        public static Button AddButton(RectTransform parent, string name, Vector2 position, Vector2 size, Color background)
        {
            var rect = AddPanel(parent, name, position, size, background);
            var image = rect.GetComponent<Image>();
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = background;
            colors.highlightedColor = Color.Lerp(background, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(background, Color.black, 0.16f);
            colors.selectedColor = background;
            button.colors = colors;
            return button;
        }

        public static Button AddLocalizedButton(RectTransform parent, string name, string stringId, Vector2 position, Vector2 size, Color background, Color foreground, float fontSize = 42f)
        {
            var button = AddButton(parent, name, position, size, background);
            AddLocalizedLabel(button.GetComponent<RectTransform>(), stringId, fontSize, FontStyles.Bold, foreground, Vector2.zero, size);
            return button;
        }

        public static TextMeshProUGUI AddLabel(RectTransform parent, string text, float size, FontStyles style, Color color, Vector2 position, Vector2 rectSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            Center(rect, position, rectSize);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Max(12f, size * 0.6f);
            label.fontSizeMax = size;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -3f);
            return label;
        }

        public static TextMeshProUGUI AddLocalizedLabel(RectTransform parent, string stringId, float size, FontStyles style, Color color, Vector2 position, Vector2 rectSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = AddLabel(parent, stringId, size, style, color, position, rectSize, alignment);
            label.gameObject.AddComponent<LocalizedText>().SetStringId(stringId);
            return label;
        }

        public static void AddGrid(RectTransform parent, Color lineColor, Color dotColor, float spacing, float width = 1080f, float height = 1920f)
        {
            for (float x = -width * 0.5f; x <= width * 0.5f; x += spacing)
                AddPanel(parent, $"GridV_{x:0}", new Vector2(x, 0f), new Vector2(3f, height), lineColor, false);

            for (float y = -height * 0.5f; y <= height * 0.5f; y += spacing)
                AddPanel(parent, $"GridH_{y:0}", new Vector2(0f, y), new Vector2(width, 3f), lineColor, false);

            for (float x = -width * 0.5f; x <= width * 0.5f; x += spacing)
            {
                for (float y = -height * 0.5f; y <= height * 0.5f; y += spacing)
                    AddCircle(parent, $"GridDot_{x:0}_{y:0}", new Vector2(x, y), 34f, dotColor, false);
            }
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void SetTop(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }

        public static void SetBottom(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
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
