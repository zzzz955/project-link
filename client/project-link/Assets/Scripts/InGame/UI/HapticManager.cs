using UnityEngine;

namespace ProjectLink.InGame.UI
{
    public static class HapticManager
    {
        public static bool Enabled { get; set; } = true;

        // Called when a drag attempts a blocked cell direction
        public static void PlayBlocked() => TryVibrate();

        // Called when a color pair is successfully connected
        public static void PlayConnected() => TryVibrate();

        // Called when a path is deleted via erase mode
        public static void PlayErased() => TryVibrate();

        static void TryVibrate()
        {
            if (!Enabled) return;
#if UNITY_ANDROID || UNITY_IOS
            try { Handheld.Vibrate(); }
            catch { }
#endif
        }
    }
}
