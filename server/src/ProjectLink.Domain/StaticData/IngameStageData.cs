namespace ProjectLink.Domain.StaticData;

public class IngameStageData
{
    public int    StageId       { get; set; }
    public int    Width         { get; set; }
    public int    Height        { get; set; }
    public int    TimeLimit     { get; set; }
    public int    MoveLimit     { get; set; }   // 0 = unlimited
    public int    Difficulty    { get; set; }
    public string BoardEncoding { get; set; } = "";
    public string NodeMap       { get; set; } = "";
    public string CellMap       { get; set; } = "";
    public int    SoftReward    { get; set; }
    public string StageMeta     { get; set; } = "";
    public uint   GeneratorSeed { get; set; }
}
