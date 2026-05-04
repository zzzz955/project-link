using System.Collections.Generic;
using ProjectLink.Core;
using Cell = ProjectLink.InGame.Board.Cell;

namespace ProjectLink.InGame.Path
{
    public class PathDrawer
    {
        readonly ProjectLink.InGame.Board.Board _board;
        readonly GameStateMachine               _stateMachine;
        readonly Dictionary<int, PathModel>     _paths = new();

        PathModel _activePath;

        public PathDrawer(ProjectLink.InGame.Board.Board board, GameStateMachine stateMachine)
        {
            _board        = board;
            _stateMachine = stateMachine;
        }

        public bool TryStartPath(Cell startCell)
        {
            if (!startCell.IsNode) return false;
            if (_stateMachine.Current != GameState.Idle) return false;

            int colorId = startCell.ColorId;

            if (_paths.TryGetValue(colorId, out var existing))
            {
                _board.ClearPathCells(colorId);
                existing.Clear();
            }
            else
            {
                _paths[colorId] = new PathModel(colorId);
            }

            _activePath = _paths[colorId];
            _activePath.AddCell(startCell);

            _stateMachine.TryTransition(GameState.Drawing);
            return true;
        }

        public void ProcessCell(Cell cell)
        {
            if (_activePath == null || _stateMachine.Current != GameState.Drawing) return;

            if (_activePath.Contains(cell.X, cell.Y))
            {
                for (int i = _activePath.Cells.Count - 1; i >= 0; i--)
                {
                    var c = _activePath.Cells[i];
                    if (c.X == cell.X && c.Y == cell.Y) break;
                    if (c.IsPath) c.Clear();
                }
                _activePath.TruncateTo(cell.X, cell.Y);
                return;
            }

            if (!IsAdjacent(_activePath.Cells[^1], cell)) return;
            if (!CanMoveTo(cell)) return;

            if (cell.IsEmpty) _board.SetPath(cell.X, cell.Y, _activePath.ColorId);
            _activePath.AddCell(cell);
        }

        public void EndPath()
        {
            if (_activePath == null) return;

            if (_activePath.IsComplete)
            {
                bool allConnected = true;
                foreach (int colorId in _board.ColorIds)
                {
                    if (!_paths.TryGetValue(colorId, out var path) || !path.IsComplete)
                    {
                        allConnected = false;
                        break;
                    }
                }
                _stateMachine.TryTransition(allConnected ? GameState.Completed : GameState.Idle);
            }
            else
            {
                _board.ClearPathCells(_activePath.ColorId);
                _activePath.Clear();
                _stateMachine.TryTransition(GameState.Idle);
            }

            _activePath = null;
        }

        public PathModel GetPath(int colorId) =>
            _paths.TryGetValue(colorId, out var path) ? path : null;

        bool CanMoveTo(Cell cell) =>
            cell.IsEmpty || (cell.IsNode && cell.ColorId == _activePath.ColorId);

        static bool IsAdjacent(Cell a, Cell b) =>
            (a.X == b.X && System.Math.Abs(a.Y - b.Y) == 1) ||
            (a.Y == b.Y && System.Math.Abs(a.X - b.X) == 1);
    }
}
