using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectLink.Core
{
    public sealed class ButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        const float PressScaleMultiplier = 0.88f;
        const float AnimDur = 0.07f;

        Coroutine _anim;
        Vector3 _restoreScale;
        bool _isPressed;

        public void OnPointerDown(PointerEventData _)
        {
            _isPressed = true;
            _restoreScale = transform.localScale;
            Animate(_restoreScale * PressScaleMultiplier);
        }

        public void OnPointerUp(PointerEventData _) => RestoreIfPressed();
        public void OnPointerExit(PointerEventData _) => RestoreIfPressed();

        void RestoreIfPressed()
        {
            if (!_isPressed) return;
            _isPressed = false;
            Animate(_restoreScale);
        }

        void Animate(Vector3 target)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(ScaleTo(target));
        }

        IEnumerator ScaleTo(Vector3 target)
        {
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / AnimDur);
                transform.localScale = Vector3.Lerp(start, target, t);
                yield return null;
            }
        }
    }
}
