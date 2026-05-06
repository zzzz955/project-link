using System.Collections.Generic;
using ProjectLink.InGame.Board;

namespace ProjectLink.InGame.Path
{
    public class PathModel
    {
        public int ColorId { get; }
        public IReadOnlyList<Cell> Cells => _cells;
        public bool IsComplete { get; private set; }

        readonly List<Cell>          _cells   = new();
        readonly HashSet<(int, int)> _cellSet = new();

        public PathModel(int colorId) { ColorId = colorId; }

        public void AddCell(Cell cell)
        {
            _cells.Add(cell);
            _cellSet.Add((cell.X, cell.Y));
            UpdateIsComplete();
        }

        public bool TruncateTo(int x, int y)
        {
            int idx = _cells.FindIndex(c => c.X == x && c.Y == y);
            if (idx < 0) return false;
            _cells.RemoveRange(idx + 1, _cells.Count - idx - 1);
            _cellSet.Clear();
            foreach (var c in _cells) _cellSet.Add((c.X, c.Y));
            UpdateIsComplete();
            return true;
        }

        public bool Contains(int x, int y) => _cellSet.Contains((x, y));

        public int IndexOf(int x, int y) => _cells.FindIndex(c => c.X == x && c.Y == y);

        // Removes cell at (x,y) and everything after; returns false if not found
        public bool TruncateBefore(int x, int y)
        {
            int idx = _cells.FindIndex(c => c.X == x && c.Y == y);
            if (idx < 0) return false;
            _cells.RemoveRange(idx, _cells.Count - idx);
            _cellSet.Clear();
            foreach (var c in _cells) _cellSet.Add((c.X, c.Y));
            UpdateIsComplete();
            return true;
        }

        public void Clear()
        {
            _cells.Clear();
            _cellSet.Clear();
            IsComplete = false;
        }

        void UpdateIsComplete()
        {
            IsComplete = _cells.Count >= 2
                && _cells[0].IsNode  && _cells[0].NodeGroupId == ColorId
                && _cells[^1].IsNode && _cells[^1].NodeGroupId == ColorId
                && !ReferenceEquals(_cells[^1], _cells[0]);
        }
    }
}
