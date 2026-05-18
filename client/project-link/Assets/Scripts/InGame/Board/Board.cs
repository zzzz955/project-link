using System;
using System.Collections.Generic;
using ProjectLink.Data;

namespace ProjectLink.InGame.Board
{
    public class Board
    {
        public int Width  { get; }
        public int Height { get; }

        public IReadOnlyCollection<int> GroupIds => _groupIds;
        public IReadOnlyCollection<int> ColorIds => _groupIds;

        readonly Cell[,]                    _cells;
        readonly HashSet<int>               _groupIds   = new();
        readonly Dictionary<int, List<Cell>> _groupNodes = new();

        public Board(StageData stageData)
        {
            Width  = stageData.Width;
            Height = stageData.Height;
            _cells = new Cell[Width, Height];

            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                _cells[x, y] = new Cell(x, y);

                int cellType = stageData.CellMap[x, y];
                if      (cellType == 1) _cells[x, y].SetObstacle();
                else if (cellType >= 2) _cells[x, y].SetGimmick();

                int groupId = stageData.NodeMap[x, y];
                if (groupId > 0)
                {
                    _cells[x, y].SetNode(groupId);
                    _groupIds.Add(groupId);
                    if (!_groupNodes.ContainsKey(groupId)) _groupNodes[groupId] = new List<Cell>();
                    _groupNodes[groupId].Add(_cells[x, y]);
                }
            }
        }

        public Cell GetCell(int x, int y) => _cells[x, y];
        public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public IEnumerable<Cell> GetAdjacentCells(int x, int y)
        {
            if (IsInBounds(x, y - 1)) yield return _cells[x, y - 1];
            if (IsInBounds(x, y + 1)) yield return _cells[x, y + 1];
            if (IsInBounds(x - 1, y)) yield return _cells[x - 1, y];
            if (IsInBounds(x + 1, y)) yield return _cells[x + 1, y];
        }

        public IReadOnlyList<Cell> GetGroupNodes(int groupId) =>
            _groupNodes.TryGetValue(groupId, out var list) ? list : Array.Empty<Cell>();

        public void ClaimPath(int x, int y, int groupId) => _cells[x, y].ClaimPath(groupId);
        public void ReleasePath(int x, int y)             => _cells[x, y].ReleasePath();

        public void ClearGroupPaths(int groupId)
        {
            for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (_cells[x, y].HasPath && _cells[x, y].PathOwner == groupId)
                    _cells[x, y].ReleasePath();
        }

        public void RemoveObstacle(int x, int y)
        {
            if (!IsInBounds(x, y)) return;
            var cell = _cells[x, y];
            if (cell.IsObstacle) cell.SetEmpty();
        }

        public void RemoveNodePair(int groupId)
        {
            ClearGroupPaths(groupId);
            if (_groupNodes.TryGetValue(groupId, out var nodes))
            {
                foreach (var node in nodes) node.SetEmpty();
                _groupNodes.Remove(groupId);
            }
            _groupIds.Remove(groupId);
        }

        // Compat aliases
        public void SetPath(int x, int y, int colorId)   => ClaimPath(x, y, colorId);
        public void ClearPathCells(int colorId)           => ClearGroupPaths(colorId);
    }
}
