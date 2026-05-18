namespace ProjectLink.Core
{
    public static class GameContext
    {
        public static int SelectedStageId { get; set; } = 1;
        public static string StageSessionToken { get; set; } = "";
        public static long StageStartedAtMs { get; set; }
        public static int MoveLimit { get; set; }
        public static int TimeLimitSeconds { get; set; }
        public static System.Collections.Generic.Dictionary<int, int> ItemCounts { get; private set; } = new();
        public static bool IsStreakChallengeActive      { get; set; }
        public static bool ShouldOpenStreakPopupOnLobby { get; set; }
        public static bool SuppressTitleSilentLoginOnce { get; private set; }

        public static void SetStageSession(string sessionToken, int moveLimit, int timeLimitSeconds,
            System.Collections.Generic.Dictionary<int, int> itemCounts = null)
        {
            StageSessionToken = sessionToken ?? "";
            MoveLimit = moveLimit;
            TimeLimitSeconds = timeLimitSeconds;
            StageStartedAtMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            ItemCounts = itemCounts != null
                ? new System.Collections.Generic.Dictionary<int, int>(itemCounts)
                : new System.Collections.Generic.Dictionary<int, int>();
        }

        public static void ClearStageSession()
        {
            StageSessionToken = "";
            StageStartedAtMs = 0;
            MoveLimit = 0;
            TimeLimitSeconds = 0;
            ItemCounts = new System.Collections.Generic.Dictionary<int, int>();
        }

        public static void SuppressNextTitleSilentLogin()
        {
            SuppressTitleSilentLoginOnce = true;
        }

        public static bool ConsumeTitleSilentLoginSuppression()
        {
            if (!SuppressTitleSilentLoginOnce)
                return false;

            SuppressTitleSilentLoginOnce = false;
            return true;
        }
    }
}
