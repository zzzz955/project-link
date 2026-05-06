using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace ProjectLink.InGame.Camera
{
    public class BoardCameraController : MonoBehaviour
    {
        UnityEngine.Camera _cam;
        bool  _initialized;
        float _minZoom;
        float _maxZoom;
        float _boardHalfW;
        float _boardHalfH;

        // Previous-frame touch positions for delta computation
        Vector2 _prevTouch0;
        Vector2 _prevTouch1;
        bool    _wasTwoFingers;

        public void Init(ProjectLink.InGame.Board.Board board, float cellSize)
        {
            _cam = GetComponent<UnityEngine.Camera>();
            if (_cam == null) _cam = UnityEngine.Camera.main;

            float boardW = board.Width  * cellSize;
            float boardH = board.Height * cellSize;
            _boardHalfW  = boardW * 0.5f;
            _boardHalfH  = boardH * 0.5f;

            _minZoom = _cam.orthographicSize;
            _maxZoom = _minZoom * 3f;
            _initialized = true;
        }

        void OnEnable()  => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            if (!_initialized) return;

            var touches = Touch.activeTouches;
            if (touches.Count == 2)
            {
                Vector2 t0 = touches[0].screenPosition;
                Vector2 t1 = touches[1].screenPosition;

                if (_wasTwoFingers)
                {
                    // Pinch zoom
                    float prevDist = Vector2.Distance(_prevTouch0, _prevTouch1);
                    float currDist = Vector2.Distance(t0, t1);
                    if (prevDist > 0.001f)
                    {
                        float ratio = currDist / prevDist;
                        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize / ratio, _minZoom, _maxZoom);
                    }

                    // 2-finger pan
                    Vector2 prevMid = (_prevTouch0 + _prevTouch1) * 0.5f;
                    Vector2 currMid = (t0 + t1) * 0.5f;
                    Vector3 prevWorld = _cam.ScreenToWorldPoint(new Vector3(prevMid.x, prevMid.y, 0f));
                    Vector3 currWorld = _cam.ScreenToWorldPoint(new Vector3(currMid.x, currMid.y, 0f));
                    Vector3 delta = prevWorld - currWorld;

                    Vector3 pos = _cam.transform.position + delta;
                    pos.x = Mathf.Clamp(pos.x, -_boardHalfW, _boardHalfW);
                    pos.y = Mathf.Clamp(pos.y, -_boardHalfH, _boardHalfH);
                    pos.z = _cam.transform.position.z;
                    _cam.transform.position = pos;
                }

                _prevTouch0    = t0;
                _prevTouch1    = t1;
                _wasTwoFingers = true;
            }
            else
            {
                _wasTwoFingers = false;
            }
        }
    }
}
