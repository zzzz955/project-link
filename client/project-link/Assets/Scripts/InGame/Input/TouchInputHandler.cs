using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLink.InGame.Input
{
    public class TouchInputHandler : MonoBehaviour
    {
        public event Action<Vector2> OnDragStart;
        public event Action<Vector2> OnDragMove;
        public event Action<Vector2> OnDragEnd;
        public event Action<Vector2> OnLongPressStart;
        public event Action          OnLongPressCanceled;

        [SerializeField] float _longPressThreshold = 0.7f;
        [SerializeField] float _longPressMoveLimit  = 0.15f;

        bool    _isPressing;
        bool    _longPressFired;
        bool    _isDragStarted;
        float   _pressTime;
        Vector2 _pressStartWorld;

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame)
            {
                _isPressing      = true;
                _longPressFired  = false;
                _isDragStarted   = false;
                _pressTime       = 0f;
                _pressStartWorld = ToWorld(pointer.position.ReadValue());
                // OnDragStart is deferred until movement is confirmed so that
                // a longpress on a completed path is not pre-empted by TryStartPath.
            }

            if (_isPressing && pointer.press.isPressed)
            {
                var worldPos = ToWorld(pointer.position.ReadValue());
                float moved  = Vector2.Distance(worldPos, _pressStartWorld);

                _pressTime += Time.deltaTime;

                if (_longPressFired)
                {
                    // longpress active — movement is ignored
                }
                else if (_isDragStarted)
                {
                    OnDragMove?.Invoke(worldPos);
                }
                else if (moved > _longPressMoveLimit)
                {
                    _isDragStarted = true;
                    OnDragStart?.Invoke(_pressStartWorld);
                    OnDragMove?.Invoke(worldPos);
                }
                else if (_pressTime >= _longPressThreshold)
                {
                    _longPressFired = true;
                    OnLongPressStart?.Invoke(_pressStartWorld);
                }
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                _isPressing = false;
                var worldPos = ToWorld(pointer.position.ReadValue());

                if (_longPressFired)
                    OnLongPressCanceled?.Invoke();
                else
                    OnDragEnd?.Invoke(worldPos);
            }
        }

        static Vector2 ToWorld(Vector2 screenPos) =>
            UnityEngine.Camera.main.ScreenToWorldPoint(screenPos);
    }
}
