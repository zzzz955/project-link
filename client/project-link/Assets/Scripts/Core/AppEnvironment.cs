namespace ProjectLink.Core
{
    public enum AppEnvironment { Dev, Prod }

    // TODO prod: replace placeholder URLs when production deployment is ready.
    // Navigation: docs/refs/platform-infra.md — port policy, network topology
    //             docs/refs/platform-auth.md  — platform auth public surface
    public static class AppConfig
    {
        public const string DevGameServerUrl  = "http://localhost:20101";
        public const string ProdGameServerUrl = "https://PLACEHOLDER_PROD_GAME_URL";

        public const string DevPlatformAuthUrl  = "http://localhost:20001";
        public const string ProdPlatformAuthUrl = "https://PLACEHOLDER_PROD_AUTH_URL";
    }
}
