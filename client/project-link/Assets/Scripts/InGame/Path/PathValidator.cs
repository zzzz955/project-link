using System.Collections.Generic;
using ProjectLink.InGame.Board;

namespace ProjectLink.InGame.Path
{
    public static class PathValidator
    {
        public static bool IsAdjacent(Cell a, Cell b) =>
            (a.X == b.X && System.Math.Abs(a.Y - b.Y) == 1) ||
            (a.Y == b.Y && System.Math.Abs(a.X - b.X) == 1);

        // Allow move to any drawable cell that is not a node from another group
        public static bool CanMoveTo(Cell target, int activeGroupId) =>
            target.IsDrawable && !(target.IsNode && target.NodeGroupId != activeGroupId);

        // True when every node in every group is an endpoint of a complete path
        public static bool IsCleared(ProjectLink.InGame.Board.Board board, Dictionary<int, List<PathModel>> paths)
        {
            foreach (int groupId in board.GroupIds)
            {
                paths.TryGetValue(groupId, out var groupPaths);
                if (!IsGroupConnected(board.GetGroupNodes(groupId), groupPaths)) return false;
            }
            return true;
        }

        // True when every node in the group is an endpoint of at least one complete path
        public static bool IsGroupConnected(IReadOnlyList<Cell> nodes, IReadOnlyList<PathModel> paths)
        {
            if (nodes == null || nodes.Count == 0) return true;
            if (paths == null || paths.Count == 0) return false;
            foreach (var node in nodes)
            {
                bool covered = false;
                foreach (var path in paths)
                {
                    if (!path.IsComplete || path.Cells.Count < 2) continue;
                    if ((path.Cells[0].X == node.X && path.Cells[0].Y == node.Y) ||
                        (path.Cells[^1].X == node.X && path.Cells[^1].Y == node.Y))
                    { covered = true; break; }
                }
                if (!covered) return false;
            }
            return true;
        }

        // Old compat signature (kept for any remaining references)
        public static bool IsCleared(
            IReadOnlyCollection<int> colorIds,
            IReadOnlyDictionary<int, PathModel> paths)
        {
            foreach (int id in colorIds)
                if (!paths.TryGetValue(id, out var p) || !p.IsComplete) return false;
            return true;
        }
    }
}
