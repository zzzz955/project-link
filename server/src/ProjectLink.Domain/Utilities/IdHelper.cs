namespace ProjectLink.Domain.Utilities;

public static class IdHelper
{
    // 16-digit range safe for JSON number precision (≤ 2^53)
    private const long Min = 1_000_000_000_000_000L;
    private const long Max = 9_007_199_254_740_992L;

    public static string NewId() => Random.Shared.NextInt64(Min, Max).ToString();
}
