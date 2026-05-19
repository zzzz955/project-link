namespace ProjectLink.Core
{
    public enum AppEnvironment { Dev, Prod }

    public static class AppConfig
    {
        public const string DevGameServerUrl  = "http://localhost:20101";
        public const string ProdGameServerUrl = "https://colorbridge.mooo.com";
    }
}
