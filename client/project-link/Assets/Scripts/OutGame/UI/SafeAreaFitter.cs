using UnityEngine;

namespace ProjectLink.OutGame.UI
{
    [ExecuteAlways]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rectTransform;
        Rect _lastSafeArea;
        Vector2Int _lastScreenSize;

        void OnEnable()
        {
            _rectTransform = GetComponent<RectTransform>();
            Apply();
        }

        void Update() => Apply();

        void Apply()
        {
            if (_rectTransform == null) return;

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (safeArea == _lastSafeArea && screenSize == _lastScreenSize) return;

            _lastSafeArea = safeArea;
            _lastScreenSize = screenSize;

            if (Screen.width <= 0 || Screen.height <= 0)
            {
                _rectTransform.anchorMin = Vector2.zero;
                _rectTransform.anchorMax = Vector2.one;
                return;
            }

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}

