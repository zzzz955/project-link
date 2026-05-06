using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLink.Core
{
    public abstract class PopupBase : MonoBehaviour
    {
        public virtual void OnBackPressed() => PopupManager.Instance.CloseTop();
    }

    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        private readonly Stack<PopupBase> _stack = new();
        private int _lastPushFrame = -1;

        public bool HasPopup => _stack.Count > 0;
        public int Count => _stack.Count;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && HasPopup && Time.frameCount > _lastPushFrame)
                CloseTop();
        }

        public void Push(PopupBase popup)
        {
            if (_stack.TryPeek(out var prev))
                prev.gameObject.SetActive(false);

            _stack.Push(popup);
            _lastPushFrame = Time.frameCount;
        }

        public void CloseTop()
        {
            if (!HasPopup) return;

            Destroy(_stack.Pop().gameObject);

            if (_stack.TryPeek(out var top))
                top.gameObject.SetActive(true);
        }

        public void CloseAll()
        {
            while (HasPopup)
                Destroy(_stack.Pop().gameObject);
        }

        public T Open<T>() where T : PopupBase
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(UIManager.Instance.GetLayer(UILayer.Popup), false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var popup = go.AddComponent<T>();
            Push(popup);
            return popup;
        }
    }
}
