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
        float _restoreScale = 1f;

        public void OnPointerDown(PointerEventData _)
        {
            _restoreScale = transform.localScale.x;
            Animate(_restoreScale * PressScaleMultiplier);
        }

        public void OnPointerUp(PointerEventData _)   => Animate(_restoreScale);
        public void OnPointerExit(PointerEventData _) => Animate(_restoreScale);

        void Animate(float target)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(ScaleTo(target));
        }

        IEnumerator ScaleTo(float target)
        {
            float start = transform.localScale.x;
            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(1f, t + Time.unscaledDeltaTime / AnimDur);
                float s = Mathf.Lerp(start, target, t);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }
    }
}
