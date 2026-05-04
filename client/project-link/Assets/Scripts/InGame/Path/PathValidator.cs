using System.Collections.Generic;
using ProjectLink.InGame.Board;

namespace ProjectLink.InGame.Path
{
    public static class PathValidator
    {
        public static bool IsAdjacent(Cell a, Cell b) =>
            (a.X == b.X && System.Math.Abs(a.Y - b.Y) == 1) ||
            (a.Y == b.Y && System.Math.Abs(a.X - b.X) == 1);

        public static bool CanMoveTo(Cell target, int activeColorId) =>
            target.IsEmpty || (target.IsNode && target.ColorId == activeColorId);

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
