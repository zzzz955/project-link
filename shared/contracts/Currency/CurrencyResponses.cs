#nullable enable

namespace ProjectLink.Contracts.Currency
{

public class CurrencyResponse
{
    public long SoftAmount { get; set; }
}

public class CurrencyAdRewardResponse
{
    public long SoftAmountAfter { get; set; }
    public int Added { get; set; }
}
}
