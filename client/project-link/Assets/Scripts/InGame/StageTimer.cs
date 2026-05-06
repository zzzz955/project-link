using System;
using UnityEngine;

namespace ProjectLink.InGame
{
    // Anti-tamper countdown timer.
    // Uses Time.realtimeSinceStartup (immune to Time.timeScale manipulation)
    // cross-checked against DateTime.UtcNow. On divergence, takes the larger
    // (penalizing) value so slowing realtimeSinceStartup via memory edit can't
    // extend the deadline.
    public class StageTimer
    {
        const float TAMPER_THRESHOLD = 2f;

        public event Action OnTimeUp;

        float _timeLimit;
        float _startRealtime;
        long  _startUtcTicks;

        float _pauseOffsetRealtime;
        long  _pauseOffsetTicks;
        float _pauseEnteredRealtime;
        long  _pauseEnteredUtcTicks;

        bool _active;
        bool _isPaused;

        public bool  HasLimit  => _timeLimit > 0f;
        public bool  IsExpired { get; private set; }
        public float Remaining => HasLimit ? Mathf.Max(0f, _timeLimit - Elapsed) : float.MaxValue;

        float Elapsed
        {
            get
            {
                float fromRealtime = (Time.realtimeSinceStartup - _startRealtime) - _pauseOffsetRealtime;
                float fromUtc      = (float)((DateTime.UtcNow.Ticks - _startUtcTicks - _pauseOffsetTicks) /
                                             (double)TimeSpan.TicksPerSecond);

                if (Mathf.Abs(fromRealtime - fromUtc) > TAMPER_THRESHOLD)
                    return Mathf.Max(fromRealtime, fromUtc);

                return fromRealtime;
            }
        }

        public void Start(float timeLimitSeconds)
        {
            _timeLimit           = timeLimitSeconds;
            _startRealtime       = Time.realtimeSinceStartup;
            _startUtcTicks       = DateTime.UtcNow.Ticks;
            _pauseOffsetRealtime = 0f;
            _pauseOffsetTicks    = 0L;
            IsExpired            = false;
            _active              = HasLimit;
            _isPaused            = false;
        }

        public void Tick()
        {
            if (!_active || IsExpired || _isPaused) return;

            if (Elapsed >= _timeLimit)
            {
                IsExpired = true;
                _active   = false;
                OnTimeUp?.Invoke();
            }
        }

        public void Pause()
        {
            if (!_active || _isPaused) return;
            _isPaused             = true;
            _pauseEnteredRealtime = Time.realtimeSinceStartup;
            _pauseEnteredUtcTicks = DateTime.UtcNow.Ticks;
        }

        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused            = false;
            _pauseOffsetRealtime += Time.realtimeSinceStartup - _pauseEnteredRealtime;
            _pauseOffsetTicks    += DateTime.UtcNow.Ticks - _pauseEnteredUtcTicks;
        }
    }
}
