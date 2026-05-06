using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectLink.Core;

namespace ProjectLink.InGame.UI
{
    public class InGameHUD : MonoBehaviour
    {
        public Action OnPausePressed;

        TextMeshProUGUI _pipeCounterText;
        TextMeshProUGUI _stageLabelText;
        TextMeshProUGUI _timerText;
        Func<int>       _getConnectedCount;
        int             _totalColors;
        RectTransform   _root;

        static readonly Color _timerNormal  = new(1f, 0.85f, 0.3f, 1f);
        static readonly Color _timerUrgent  = new(1f, 0.35f, 0.3f, 1f);

        public void Init(int stageId, int totalColors, Func<int> getConnectedCount, int timeLimitSeconds = 0)
        {
            _totalColors       = totalColors;
            _getConnectedCount = getConnectedCount;

            var parent = UIManager.Instance.GetLayer(UILayer.HUD);
            DestroyStaleHud(parent);
            _root = CreateRoot(parent);
            BuildTopHud(_root, stageId, totalColors, timeLimitSeconds > 0);
            BuildObjectiveStrip(_root, totalColors);
            BuildToolDock(_root);
        }

        public void Refresh()
        {
            if (_pipeCounterText == null) return;
            _pipeCounterText.text = $"{_getConnectedCount()} / {_totalColors}";
        }

        public void SetTimerDisplay(float remaining)
        {
            if (_timerText == null) return;
            int s = Mathf.Max(0, Mathf.FloorToInt(remaining));
            _timerText.text  = s >= 60 ? $"{s / 60}:{s % 60:D2}" : s.ToString();
            _timerText.color = s <= 10 ? _timerUrgent : _timerNormal;
        }

        void OnDestroy()
        {
            if (_root != null)
                Destroy(_root.gameObject);
        }

        static void DestroyStaleHud(Transform parent)
        {
            var stale = parent.Find("ModernInGameHUD");
            if (stale != null)
                Destroy(stale.gameObject);
        }

        RectTransform CreateRoot(Transform parent)
        {
            var rootGo = new GameObject("ModernInGameHUD");
            rootGo.transform.SetParent(parent, false);
            var rect = rootGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        void BuildTopHud(RectTransform root, int stageId, int totalColors, bool showTimer)
        {
            var bar = AddPanel(root, "TopHUD", new Vector2(0f, -72f), new Vector2(960f, 112f), new Color(0.035f, 0.045f, 0.07f, 0.9f));
            bar.anchorMin = new Vector2(0.5f, 1f);
            bar.anchorMax = new Vector2(0.5f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);

            _pipeCounterText = AddLabel(bar, $"0 / {totalColors}", 34, FontStyles.Bold, new Color(0.2f, 0.95f, 0.8f, 1f), new Vector2(-360f, 0f), new Vector2(190f, 72f), TextAlignmentOptions.Center);
            _stageLabelText  = AddLabel(bar, $"STAGE {stageId}", 36, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(260f, 72f), TextAlignmentOptions.Center);

            if (showTimer)
                _timerText = AddLabel(bar, "--", 32, FontStyles.Bold, _timerNormal, new Vector2(260f, 0f), new Vector2(120f, 72f), TextAlignmentOptions.Center);

            var pause = AddPanel(bar, "PauseButton", new Vector2(386f, 0f), new Vector2(82f, 82f), new Color(1f, 1f, 1f, 0.12f));
            var pauseButton = pause.gameObject.AddComponent<Button>();
            AddLabel(pause, "II", 30, FontStyles.Bold, Color.white, Vector2.zero, new Vector2(82f, 82f), TextAlignmentOptions.Center);
            pauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());
        }

        void BuildObjectiveStrip(RectTransform root, int totalColors)
        {
            var strip = AddPanel(root, "ObjectiveStrip", new Vector2(0f, -198f), new Vector2(620f, 56f), new Color(1f, 1f, 1f, 0.07f));
            strip.anchorMin = new Vector2(0.5f, 1f);
            strip.anchorMax = new Vector2(0.5f, 1f);
            strip.pivot = new Vector2(0.5f, 1f);

            AddLabel(strip, "CONNECT ALL", 20, FontStyles.UpperCase, new Color(0.75f, 0.8f, 0.88f, 1f), new Vector2(-190f, 0f), new Vector2(180f, 40f), TextAlignmentOptions.Left);

            Color[] colors =
            {
                new(0.2f, 0.95f, 0.8f, 1f),
                new(1f, 0.45f, 0.42f, 1f),
                new(1f, 0.78f, 0.28f, 1f),
                new(0.35f, 0.58f, 1f, 1f),
                new(0.8f, 0.48f, 1f, 1f)
            };

            int count = Mathf.Clamp(totalColors, 1, 5);
            for (int i = 0; i < count; i++)
                AddPanel(strip, $"ColorPip_{i}", new Vector2(70f + i * 58f, 0f), new Vector2(34f, 34f), colors[i % colors.Length]);
        }

        void BuildToolDock(RectTransform root)
        {
            var dock = AddPanel(root, "ToolDock", new Vector2(0f, 54f), new Vector2(780f, 118f), new Color(0.035f, 0.045f, 0.07f, 0.92f));
            dock.anchorMin = new Vector2(0.5f, 0f);
            dock.anchorMax = new Vector2(0.5f, 0f);
            dock.pivot = new Vector2(0.5f, 0f);

            AddTool(dock, "UNDO", new Vector2(-248f, 0f), new Color(0.35f, 0.58f, 1f, 1f));
            AddTool(dock, "ERASE", Vector2.zero, new Color(1f, 0.45f, 0.42f, 1f));
            AddTool(dock, "HINT", new Vector2(248f, 0f), new Color(1f, 0.78f, 0.28f, 1f));
        }

        void AddTool(RectTransform parent, string label, Vector2 position, Color accent)
        {
            var tool = AddPanel(parent, label + "Tool", position, new Vector2(190f, 78f), new Color(1f, 1f, 1f, 0.08f));
            AddPanel(tool, "Accent", new Vector2(-68f, 0f), new Vector2(14f, 42f), accent);
            AddLabel(tool, label, 24, FontStyles.Bold, Color.white, new Vector2(18f, 0f), new Vector2(120f, 42f), TextAlignmentOptions.Center);
        }

        TextMeshProUGUI AddLabel(RectTransform parent, string text, float fontSize, FontStyles style, Color color, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        RectTransform AddPanel(RectTransform parent, string name, Vector2 position, Vector2 size, Color color)
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
    }
}
