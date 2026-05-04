using UnityEngine;

namespace ProjectLink.InGame.UI
{
    public class CircularGauge : MonoBehaviour
    {
        [SerializeField] float _radius   = 0.35f;
        [SerializeField] int   _segments = 40;

        LineRenderer _line;

        void Awake()
        {
            _line = gameObject.AddComponent<LineRenderer>();
            _line.sortingLayerName   = "Node";
            _line.startWidth         = 0.08f;
            _line.endWidth           = 0.08f;
            _line.numCornerVertices  = 4;
            _line.numCapVertices     = 4;
            _line.useWorldSpace      = true;
            _line.material           = new Material(Shader.Find("Sprites/Default"));
            gameObject.SetActive(false);
        }

        public void Show(Vector3 worldPos, Color color)
        {
            gameObject.SetActive(true);
            transform.position    = worldPos;
            _line.startColor      = color;
            _line.endColor        = color;
        }

        public void SetProgress(float t)
        {
            int count = Mathf.Max(2, Mathf.RoundToInt(t * _segments));
            _line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float angle = 90f - (i / (float)(_segments - 1)) * 360f * t;
                _line.SetPosition(i, ArcPoint(transform.position, _radius, angle));
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        static Vector3 ArcPoint(Vector3 center, float radius, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return center + new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
        }
    }
}
