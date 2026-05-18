namespace ProjectLink.Services
{
    public static class UiDataRoutes
    {
        public const string BootstrapConfig = "/api/bootstrap/config";
        public const string AccountMe = "/api/account/me";
        public const string LobbyState = "/api/lobby";
        public const string Progress = "/api/progress";
        public const string Stamina = "/api/stamina";
        public const string StaminaAdReward = "/api/stamina/ad-reward";
        public const string StaminaRefill = "/api/stamina/refill";
        public const string Inventory = "/api/inventory";
        public const string ShopCatalog = "/api/shop/catalog";
        public const string ShopPurchase = "/api/shop/purchase";
        public const string RankingGlobalScore = "/api/ranking/global/score";
        public const string RankingGlobalStages = "/api/ranking/global/stages";
        public const string StreakChallenge = "/api/streak-challenge";
        public const string StreakChallengeActivate = "/api/streak-challenge/activate";
        public static string StreakChallengeStartLevel(int level) => $"/api/streak-challenge/level/{level}/start";
        public static string StreakChallengeClaimReward(int level) => $"/api/streak-challenge/level/{level}/claim-reward";
        public const string SeasonEvents = "/api/events/season";
        public const string PlayerSettings = "/api/settings";
        public const string RewardsClaim = "/api/rewards/claim";

        public static string StageStart(int stageId)  => $"/api/stage/{stageId}/start";
        public static string StageEnd(int stageId)    => $"/api/stage/{stageId}/end";
        public static string StageExtend(int stageId) => $"/api/stage/{stageId}/extend";
        public static string RankingStage(int stageId) => $"/api/ranking/stage/{stageId}";

        public const string UseIngameItem = "/api/items/use-ingame";

        public static string Ranking(string category)
        {
            if (!string.IsNullOrEmpty(category) && category.StartsWith("stage:") && int.TryParse(category[6..], out var stageId))
                return RankingStage(stageId);

            return category switch
            {
                "global_stages" => RankingGlobalStages,
                "stages" => RankingGlobalStages,
                "stage" => RankingGlobalStages,
                _ => RankingGlobalScore,
            };
        }
    }
}
