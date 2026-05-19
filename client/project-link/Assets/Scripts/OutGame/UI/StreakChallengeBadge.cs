#nullable enable
using System;
using System.Collections;
using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class StreakChallengeBadge : MonoBehaviour
    {
        [SerializeField] Image? badgeImage;
        [SerializeField] TextMeshProUGUI? labelText;
        [SerializeField] TextMeshProUGUI? progressText;
        // Spine placeholder: assign SkeletonAnimation component here when asset is ready.

        static readonly Color ColorActive    = new Color(1f,    0.72f, 0.1f,  1f);
        static readonly Color ColorReward    = new Color(0.2f,  0.85f, 0.35f, 1f);
        static readonly Color ColorCompleted = new Color(0.55f, 0.75f, 0.55f, 0.8f);
        static readonly Color ColorInactive  = new Color(0.35f, 0.35f, 0.4f,  0.65f);

        bool _shouldAnimate;
        Coroutine? _bounceLoop;
        Coroutine? _timerLoop;
        DateTimeOffset _expiresAt;

        void Awake()
        {
            ResolveRefs();
            var btn = GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => PopupManager.Request(PopupId.StreakChallenge));
        }

        void OnEnable()
        {
            if (_bounceLoop != null) StopCoroutine(_bounceLoop);
            _bounceLoop = StartCoroutine(BounceLoop());
        }

        void OnDisable()
        {
            if (_bounceLoop != null) { StopCoroutine(_bounceLoop); _bounceLoop = null; }
            if (_timerLoop  != null) { StopCoroutine(_timerLoop);  _timerLoop  = null; }
            transform.localScale = Vector3.one;
        }

        /// <param name="eventStatus">INACTIVE | ACTIVE | COMPLETED | EXPIRED</param>
        /// <param name="hasPendingReward">reward waiting to be claimed</param>
        /// <param name="remainingTimeIso">ISO 8601 duration from server (e.g. "PT23H45M")</param>
        public void Apply(string? eventStatus, bool hasPendingReward, string remainingTimeIso)
        {
            string status = eventStatus ?? "INACTIVE";
            _shouldAnimate = status == "ACTIVE" || hasPendingReward;

            Color  bg;
            string label;
            bool   interactive;

            if (hasPendingReward)
            {
                bg = ColorReward;    label = "GO"; interactive = true;
            }
            else if (status == "ACTIVE")
            {
                bg = ColorActive;    label = "SC"; interactive = true;
            }
            else if (status == "COMPLETED")
            {
                bg = ColorCompleted; label = "OK"; interactive = false;
            }
            else
            {
                bg = ColorInactive;  label = "SC"; interactive = true;
            }

            if (badgeImage != null) badgeImage.color = bg;
            if (labelText  != null) labelText.text   = label;

            var button = GetComponent<Button>();
            if (button != null) button.interactable = interactive;

            // Timer
            StopTimer();
            if (status == "ACTIVE" && !string.IsNullOrEmpty(remainingTimeIso))
            {
                try
                {
                    var ts = System.Xml.XmlConvert.ToTimeSpan(remainingTimeIso);
                    _expiresAt = DateTimeOffset.UtcNow.Add(ts);
                    UpdateTimerText();
                    if (gameObject.activeInHierarchy)
                        _timerLoop = StartCoroutine(TimerLoop());
                }
                catch { SetProgressText(""); }
            }
            else
            {
                SetProgressText("");
            }
        }

        void StopTimer()
        {
            if (_timerLoop != null) { StopCoroutine(_timerLoop); _timerLoop = null; }
        }

        IEnumerator TimerLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(60f);
                UpdateTimerText();
            }
        }

        void UpdateTimerText()
        {
            var remaining = _expiresAt - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) { SetProgressText("00:00"); StopTimer(); return; }
            int h = (int)remaining.TotalHours;
            int m = remaining.Minutes;
            SetProgressText($"{h:D2}:{m:D2}");
        }

        void SetProgressText(string value)
        {
            if (progressText != null) progressText.text = value;
        }

        IEnumerator BounceLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(5f);
                if (!_shouldAnimate) continue;
                yield return DoBounce();
            }
        }

        IEnumerator DoBounce()
        {
            var rt     = GetComponent<RectTransform>();
            var origin = rt.anchoredPosition;
            const float rise   = 0.15f;
            const float fall   = 0.10f;
            const float height = 16f;
            const float pulse  = 0.22f;

            float t = 0f;
            while (t < rise)
            {
                t += Time.unscaledDeltaTime;
                rt.anchoredPosition = origin + Vector2.up * (Mathf.Sin(Mathf.Clamp01(t / rise) * Mathf.PI * 0.5f) * height);
                yield return null;
            }

            t = 0f;
            while (t < fall)
            {
                t += Time.unscaledDeltaTime;
                rt.anchoredPosition = origin + Vector2.up * (Mathf.Cos(Mathf.Clamp01(t / fall) * Mathf.PI * 0.5f) * height);
                yield return null;
            }
            rt.anchoredPosition = origin;

            t = 0f;
            while (t < pulse)
            {
                t += Time.unscaledDeltaTime;
                float s = 1f + 0.15f * Mathf.Sin(Mathf.Clamp01(t / pulse) * Mathf.PI);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }

        void ResolveRefs()
        {
            if (badgeImage == null)
                foreach (var img in GetComponentsInChildren<Image>(true))
                    if (img.gameObject != gameObject) { badgeImage = img; break; }

            foreach (var tmp in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.name == "Txt_Icon")     labelText    ??= tmp;
                if (tmp.name == "Txt_Progress") progressText ??= tmp;
            }
        }
    }
}
