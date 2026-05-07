namespace ProjectLink.Contracts.Lobby;

public class LobbyStateResponse
{
    public LobbyProfile         Profile         { get; set; } = new();
    public LobbyStamina         Stamina         { get; set; } = new();
    public LobbyCurrency        Currency        { get; set; } = new();
    public LobbyProgressSummary ProgressSummary { get; set; } = new();
    public LobbyDailyChallenge  DailyChallenge  { get; set; } = new();
    public LobbySeasonEvent?    SeasonEvent     { get; set; }
}

public class LobbyProfile
{
    public string DisplayName { get; set; } = "";
    public int    AvatarId    { get; set; }
}

public class LobbyStamina
{
    public int     Current        { get; set; }
    public int     Max            { get; set; }
    public string? NextRechargeAt { get; set; }
}

public class LobbyCurrency
{
    public long SoftAmount { get; set; }
}

public class LobbyProgressSummary
{
    public int HighestStageId      { get; set; }
    public int TotalStarsEarned    { get; set; }
    public int NextUnlockedStageId { get; set; }
}

public class LobbyDailyChallenge
{
    public bool   CompletedToday  { get; set; }
    public bool   CanComplete     { get; set; }
    public int    PlayCountToday  { get; set; }
    public int    PlayCountTarget { get; set; }
    public int    StreakDays      { get; set; }
    public string ResetAt         { get; set; } = "";
}

public class LobbySeasonEvent
{
    public int    EventId  { get; set; }
    public string Name     { get; set; } = "";
    public string EndAt    { get; set; } = "";
    public bool   IsActive { get; set; }
}
