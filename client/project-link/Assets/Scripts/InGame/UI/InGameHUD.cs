using System;
using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.InGame.UI
{
    public class InGameHUD : MonoBehaviour
    {
        public Action OnPausePressed;

        TextMeshProUGUI _pipeCounterText;
        TextMeshProUGUI _moveCounterText;
        TextMeshProUGUI _timerText;
        Func<int> _getConnectedCount;
        int _totalColors;

        static readonly Color _timerNormal = Color.white;
        static readonly Color _timerUrgent = new(1f, 0.25f, 0.3f, 1f);

        public void Init(int stageId, int totalColors, Func<int> getConnectedCount, int timeLimitSeconds = 0)
        {
            _totalColors = totalColors;
            _getConnectedCount = getConnectedCount;

            // Bind to DDL-generated elements first; fall back to runtime creation
            BindToDdlElements(stageId, timeLimitSeconds > 0);
            Refresh();
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
            _timerText.text = $"{s / 60:D2}:{s % 60:D2}";
            _timerText.color = s <= 10 ? _timerUrgent : _timerNormal;
        }

        public void SetMoveDisplay(int movesUsed, int moveLimit)
        {
            if (_moveCounterText == null) return;
            _moveCounterText.text = moveLimit > 0
                ? string.Format(LocalizationManager.Get("hud.moves_fmt"), movesUsed, moveLimit)
                : string.Format(LocalizationManager.Get("hud.moves_no_limit_fmt"), movesUsed);
        }

        void BindToDdlElements(int stageId, bool showTimer)
        {
            var hudLayer = UIManager.Instance?.GetLayer(UILayer.HUD);
            if (hudLayer == null) return;

            // Search scene-wide first (HUD_Top lives on GameCanvas safe area, not under Canvas_HUD)
            var ctrl = UnityEngine.Object.FindFirstObjectByType<GameWireframeController>(FindObjectsInactive.Include);
            if (ctrl == null)
                ctrl = hudLayer.GetComponentInChildren<GameWireframeController>(true);
            if (ctrl != null)
            {
                ctrl.SetStageLabel(stageId);
                _moveCounterText = ctrl.MoveCounterText;
                _timerText = ctrl.TimerText;
                _pipeCounterText = ctrl.ObjectiveText;

                // Bind pause button from DDL
                var pauseBtn = FindButtonInHud(hudLayer, "Btn_Pause");
                if (pauseBtn != null)
                    pauseBtn.onClick.AddListener(() => OnPausePressed?.Invoke());
                return;
            }

            // Fallback: create runtime HUD root if DDL controller not found
            CreateRuntimeHud(hudLayer, stageId, showTimer);
        }

        void CreateRuntimeHud(Transform parent, int stageId, bool showTimer)
        {
            DestroyStaleHud(parent);
            var root = CreateRoot(parent);
            BuildHudFallback(root, stageId, showTimer);
        }

        void BuildHudFallback(RectTransform root, int stageId, bool showTimer)
        {
            AddImageSlot(root, "LevelHeaderSlot", new Vector2(0f, -20f), new Vector2(360f, 160f), true);
            AddImageSlot(root, "BottomToolBarSlot", new Vector2(0f, 56f), new Vector2(980f, 170f), false);

            var pause = AddHotspot(root, "PauseButton", new Vector2(425f, -78f), new Vector2(120f, 120f), true);
            pause.onClick.AddListener(() => OnPausePressed?.Invoke());

            AddHotspot(root, "HintButton",    new Vector2(-405f, 56f), new Vector2(150f, 150f), false).onClick.AddListener(OpenBuyItemPopup);
            AddHotspot(root, "UndoButton",    new Vector2(-225f, 56f), new Vector2(150f, 150f), false).onClick.AddListener(OpenBuyItemPopup);
            AddHotspot(root, "ShuffleButton", new Vector2(120f,  56f), new Vector2(140f, 140f), false).onClick.AddListener(OpenBuyItemPopup);
            AddHotspot(root, "HammerButton",  new Vector2(295f,  56f), new Vector2(140f, 140f), false).onClick.AddListener(OpenBuyItemPopup);
            AddHotspot(root, "PaintButton",   new Vector2(470f,  56f), new Vector2(140f, 140f), false).onClick.AddListener(OpenBuyItemPopup);

            _pipeCounterText = AddDynamicLabel(root, "0 / 0",  60, new Vector2(42f,   -105f), new Vector2(150f, 80f),  true);
            _moveCounterText = AddDynamicLabel(root, "",        34, new Vector2(0f,    -42f),  new Vector2(260f, 56f),  true);

            if (showTimer)
                _timerText = AddDynamicLabel(root, "--:--", 36, new Vector2(-345f, -82f), new Vector2(150f, 58f), true);
        }

        void OnDestroy()
        {
            // Runtime root is parented to UILayer.HUD; clean up if we created one
            var hudLayer = UIManager.Instance?.GetLayer(UILayer.HUD);
            if (hudLayer == null) return;
            var stale = hudLayer.Find("WireframeInGameHUD");
            if (stale != null) Destroy(stale.gameObject);
        }

        static void DestroyStaleHud(Transform parent)
        {
            var stale = parent.Find("ModernInGameHUD");
            if (stale != null) Destroy(stale.gameObject);
            stale = parent.Find("WireframeInGameHUD");
            if (stale != null) Destroy(stale.gameObject);
        }

        static RectTransform CreateRoot(Transform parent)
        {
            var rootGo = new GameObject("WireframeInGameHUD");
            rootGo.transform.SetParent(parent, false);
            var rect = rootGo.AddComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        static Button FindButtonInHud(Transform hudRoot, string name)
        {
            foreach (var btn in hudRoot.GetComponentsInChildren<Button>(true))
                if (btn.name == name) return btn;
            return null;
        }

        static RectTransform AddImageSlot(RectTransform parent, string name, Vector2 position, Vector2 size, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = rect.anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.enabled = false;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return rect;
        }

        static Button AddHotspot(RectTransform parent, string name, Vector2 position, Vector2 size, bool top)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = rect.anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }

        static TextMeshProUGUI AddDynamicLabel(RectTransform parent, string text, float size, Vector2 position, Vector2 rectSize, bool top)
        {
            var go = new GameObject("DynamicLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = rect.anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = rectSize;

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return label;
        }

        static void OpenBuyItemPopup()
        {
            if (PopupManager.Instance != null)
                PopupManager.Request(PopupId.BuyItem);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
