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

            // Diagonal move: insert one L-shaped intermediate cell before target
            var last = _activePath.Cells[^1];
            int dx = cell.X - last.X;
            int dy = cell.Y - last.Y;
            if (System.Math.Abs(dx) == 1 && System.Math.Abs(dy) == 1)
            {
                var hStep = _board.GetCell(last.X + dx, last.Y);
                var vStep = _board.GetCell(last.X, last.Y + dy);

                Cell mid = null;
                if (!_activePath.Contains(hStep.X, hStep.Y) && PathValidator.CanMoveTo(hStep, _activePath.ColorId))
                    mid = hStep;
                else if (!_activePath.Contains(vStep.X, vStep.Y) && PathValidator.CanMoveTo(vStep, _activePath.ColorId))
                    mid = vStep;

                if (mid == null) return;
                AppendCell(mid);
            }

            if (!PathValidator.IsAdjacent(_activePath.Cells[^1], cell)) return;
            if (!PathValidator.CanMoveTo(cell, _activePath.ColorId)) return;

            AppendCell(cell);
        }

        void AppendCell(Cell cell)
        {
            if (cell.IsEmpty) _board.SetPath(cell.X, cell.Y, _activePath.ColorId);
            _activePath.AddCell(cell);
        }

        public void EndPath()
        {
            if (_activePath == null) return;

            if (_activePath.IsComplete)
            {
                bool allConnected = PathValidator.IsCleared(_board.ColorIds, _paths);
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

    }
}
