namespace ProjectLink.InGame.Board
{
    public enum CellType { Empty, Node, Obstacle, Gimmick }

    public class Cell
    {
        public int X { get; }
        public int Y { get; }
        public CellType Type { get; private set; }
        public int NodeGroupId { get; private set; }
        public int PathOwner { get; private set; }

        public bool IsNode     => Type == CellType.Node;
        public bool IsObstacle => Type == CellType.Obstacle;
        public bool IsGimmick  => Type == CellType.Gimmick;
        public bool IsDrawable => Type == CellType.Empty || Type == CellType.Node;
        public bool HasPath    => PathOwner > 0;

        // Compat: ColorId = NodeGroupId when node, else PathOwner
        public bool IsEmpty => Type == CellType.Empty;
        public bool IsPath  => HasPath;
        public int  ColorId => IsNode ? NodeGroupId : PathOwner;

        public Cell(int x, int y) { X = x; Y = y; }

        public void SetNode(int groupId)   { Type = CellType.Node; NodeGroupId = groupId; }
        public void SetObstacle()          { Type = CellType.Obstacle; }
        public void SetGimmick()           { Type = CellType.Gimmick; }
        public void SetEmpty()             { Type = CellType.Empty; NodeGroupId = 0; PathOwner = 0; }
        public void ClaimPath(int groupId) { PathOwner = groupId; }
        public void ReleasePath()          { PathOwner = 0; }
        public void Clear()                => ReleasePath();
    }
}
