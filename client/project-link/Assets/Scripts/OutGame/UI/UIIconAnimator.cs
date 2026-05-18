using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    [RequireComponent(typeof(Image))]
    public sealed class UIIconAnimator : MonoBehaviour
    {
        [SerializeField] float intervalSeconds  = 10f;
        [SerializeField] float bounceScale      = 1.08f;
        [SerializeField] float bounceDuration   = 0.8f;
        [SerializeField] float shimmerDuration  = 0.65f;
        [SerializeField] float shimmerWidth     = 0.35f;

        static readonly int ShimmerPosId   = Shader.PropertyToID("_ShimmerPos");
        static readonly int ShimmerWidthId = Shader.PropertyToID("_ShimmerWidth");

        Image    _icon;
        Image    _shine;
        Material _shineMat;
        Coroutine _loop;

        void Awake()
        {
            _icon  = GetComponent<Image>();
            _shine = EnsureShine();
        }

        void OnEnable()  => _loop = StartCoroutine(AnimLoop());

        void OnDisable()
        {
            if (_loop != null) { StopCoroutine(_loop); _loop = null; }
            transform.localScale = Vector3.one;
            if (_shineMat != null) _shineMat.SetFloat(ShimmerPosId, -2f);
        }

        void OnDestroy()
        {
            if (_shineMat == null) return;
            if (Application.isPlaying) Destroy(_shineMat);
            else DestroyImmediate(_shineMat);
        }

        Image EnsureShine()
        {
            var tf = transform.Find("Img_Shine");
            Image img;
            if (tf != null)
            {
                img = tf.GetComponent<Image>();
            }
            else
            {
                var go = new GameObject("Img_Shine", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                img = go.GetComponent<Image>();
                img.sprite         = _icon.sprite;
                img.color          = Color.white;
                img.raycastTarget  = false;
                img.preserveAspect = true;
            }

            var shader = Shader.Find("ProjectLink/UIShimmer");
            _shineMat = new Material(shader != null ? shader : Shader.Find("UI/Default"));
            _shineMat.SetFloat(ShimmerPosId,   -2f);
            _shineMat.SetFloat(ShimmerWidthId, shimmerWidth);
            img.material = _shineMat;
            return img;
        }

        IEnumerator AnimLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(intervalSeconds);
                yield return BounceAndShimmer();
            }
        }

        // Bounce and shimmer run concurrently in a single loop driven by elapsed time.
        IEnumerator BounceAndShimmer()
        {
            float elapsed     = 0f;
            float totalDur    = Mathf.Max(bounceDuration, shimmerDuration);

            while (elapsed < totalDur)
            {
                elapsed += Time.unscaledDeltaTime;

                // ── Bounce ───────────────────────────────────────────────────
                // Single smooth arc: 1.0 → bounceScale → 1.0
                float bt = Mathf.Clamp01(elapsed / bounceDuration);
                float arc = bt < 0.5f
                    ? EaseOut(bt * 2f)
                    : EaseOut((1f - bt) * 2f);
                transform.localScale = Vector3.one * (1f + (bounceScale - 1f) * arc);

                // ── Shimmer ──────────────────────────────────────────────────
                // Diagonal band sweeps from top-left (-0.4) to bottom-right (2.4)
                float st     = Mathf.Clamp01(elapsed / shimmerDuration);
                float shimPos = Mathf.Lerp(-0.4f, 2.4f, EaseInOut(st));
                _shineMat.SetFloat(ShimmerPosId, shimPos);

                yield return null;
            }

            transform.localScale = Vector3.one;
            _shineMat.SetFloat(ShimmerPosId, -2f);
        }

        static float EaseOut(float t)   => 1f - (1f - t) * (1f - t);
        static float EaseInOut(float t) => t < 0.5f
            ? 2f * t * t
            : 1f - 2f * (1f - t) * (1f - t);
    }
}
