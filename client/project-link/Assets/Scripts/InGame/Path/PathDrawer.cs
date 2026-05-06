using System.Collections.Generic;
using ProjectLink.Core;
using Cell = ProjectLink.InGame.Board.Cell;

namespace ProjectLink.InGame.Path
{
    public class PathDrawer
    {
        readonly ProjectLink.InGame.Board.Board _board;
        readonly GameStateMachine               _stateMachine;
        readonly Dictionary<int, List<PathModel>> _paths = new();

        PathModel _activePath;

        public PathModel ActivePath => _activePath;

        public PathDrawer(ProjectLink.InGame.Board.Board board, GameStateMachine stateMachine)
        {
            _board        = board;
            _stateMachine = stateMachine;
        }

        public bool TryStartPath(Cell cell)
        {
            if (_stateMachine.Current != GameState.Idle) return false;

            if (cell.IsNode)
            {
                int groupId = cell.NodeGroupId;
                RemovePathEndingAt(groupId, cell);
                var model = new PathModel(groupId);
                model.AddCell(cell);
                GetOrCreateGroup(groupId).Add(model);
                _activePath = model;
                _stateMachine.TryTransition(GameState.Drawing);
                return true;
            }

            // Resume from tail of incomplete path
            if (cell.HasPath && !cell.IsNode)
            {
                int groupId = cell.PathOwner;
                var path = FindIncompleteWithTail(groupId, cell);
                if (path != null)
                {
                    _activePath = path;
                    _stateMachine.TryTransition(GameState.Drawing);
                    return true;
                }
            }

            return false;
        }

        public void ProcessCell(Cell cell)
        {
            if (_activePath == null || _stateMachine.Current != GameState.Drawing) return;
            if (_activePath.IsComplete) return;  // Issue 4: don't extend past a completing node

            if (_activePath.Contains(cell.X, cell.Y))
            {
                for (int i = _activePath.Cells.Count - 1; i >= 0; i--)
                {
                    var c = _activePath.Cells[i];
                    if (c.X == cell.X && c.Y == cell.Y) break;
                    if (!c.IsNode) _board.ReleasePath(c.X, c.Y);
                }
                _activePath.TruncateTo(cell.X, cell.Y);
                return;
            }

            // Diagonal move: insert one L-shaped intermediate cell
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
                if (_activePath.IsComplete) return;
            }

            if (!PathValidator.IsAdjacent(_activePath.Cells[^1], cell)) return;
            if (!PathValidator.CanMoveTo(cell, _activePath.ColorId)) return;

            AppendCell(cell);

            // Rules 4 & 5: auto-release drag when path completes
            if (_activePath != null && _activePath.IsComplete)
            {
                AutoEndPath();
                return;
            }

            // Rule 4: merge if an adjacent same-group tail is now touching
            if (_activePath != null)
                TryMergeAdjacentTail(cell);
        }

        void AppendCell(Cell cell)
        {
            // Overwrite any other path occupying this cell (different group or same-group-different-path)
            if (cell.HasPath)
            {
                var ownerPath = FindPathOwning(cell.PathOwner, cell);
                if (ownerPath != null && !ReferenceEquals(ownerPath, _activePath))
                    HandleOverwriteOf(ownerPath, cell);
            }

            // Rule 5: reaching same-group node → remove any PathModels already using that node
            if (cell.IsNode && cell.NodeGroupId == _activePath.ColorId)
                RemovePathEndingAt(cell.NodeGroupId, cell);

            if (!cell.IsNode) _board.ClaimPath(cell.X, cell.Y, _activePath.ColorId);
            _activePath.AddCell(cell);
        }

        // Rule 3: overwrite preserves the orphaned far-side portion if it ends at a node.
        void HandleOverwriteOf(PathModel ownerPath, Cell cell)
        {
            int groupId = ownerPath.ColorId;
            int idx = ownerPath.IndexOf(cell.X, cell.Y);
            if (idx < 0) return;

            // Collect the orphaned portion (everything AFTER the overwrite cell)
            var orphaned = new List<Cell>();
            for (int i = idx + 1; i < ownerPath.Cells.Count; i++)
                orphaned.Add(ownerPath.Cells[i]);

            // Release only the overwrite cell itself from the board
            if (!cell.IsNode) _board.ReleasePath(cell.X, cell.Y);

            // Trim the owner path (does not touch board ownership)
            ownerPath.TruncateBefore(cell.X, cell.Y);
            if (_paths.TryGetValue(groupId, out var grp) && ownerPath.Cells.Count == 0)
                grp.Remove(ownerPath);

            // Preserve orphaned portion if its far end is a same-group node → reverse so node is cells[0]
            if (orphaned.Count > 0 && orphaned[^1].IsNode && orphaned[^1].NodeGroupId == groupId)
            {
                orphaned.Reverse();
                var orphan = new PathModel(groupId);
                foreach (var c in orphaned) orphan.AddCell(c);
                GetOrCreateGroup(groupId).Add(orphan);
            }
            else
            {
                // No node endpoint on the far side — discard (release board ownership)
                foreach (var c in orphaned)
                    if (!c.IsNode) _board.ReleasePath(c.X, c.Y);
            }
        }

        // Rules 4 & 5: finalize path when it completes mid-draw (no finger-lift needed)
        void AutoEndPath()
        {
            if (_activePath == null) return;
            bool allCleared = PathValidator.IsCleared(_board, _paths);
            _stateMachine.TryTransition(allCleared ? GameState.Completed : GameState.Idle);
            _activePath = null;
        }

        PathModel FindPathOwning(int groupId, Cell cell)
        {
            if (!_paths.TryGetValue(groupId, out var group)) return null;
            foreach (var p in group)
                if (p.Contains(cell.X, cell.Y)) return p;
            return null;
        }

        // Rule 4: when the new cell is orthogonally adjacent to another same-group path's tail, merge.
        void TryMergeAdjacentTail(Cell newCell)
        {
            int groupId = _activePath.ColorId;
            if (!_paths.TryGetValue(groupId, out var group)) return;

            foreach (var adj in _board.GetAdjacentCells(newCell.X, newCell.Y))
            {
                if (!adj.HasPath || adj.PathOwner != groupId) continue;
                if (_activePath.Contains(adj.X, adj.Y)) continue;

                PathModel other = null;
                foreach (var p in group)
                {
                    if (p.Cells.Count == 0 || ReferenceEquals(p, _activePath)) continue;
                    var tail = p.Cells[^1];
                    if (tail.X == adj.X && tail.Y == adj.Y) { other = p; break; }
                }
                if (other == null || other.IsComplete) continue;

                // Merge: adj (other's tail) → reverse remaining cells of other onto active path
                var otherCells = new List<Cell>(other.Cells);
                group.Remove(other);

                _activePath.AddCell(adj);
                for (int i = otherCells.Count - 2; i >= 0; i--)
                    _activePath.AddCell(otherCells[i]);

                // Rule 4: auto-release if merge completed the path
                if (_activePath.IsComplete)
                    AutoEndPath();

                break;
            }
        }

        public void EndPath()
        {
            if (_activePath == null) return;

            if (_activePath.IsComplete)
            {
                bool allCleared = PathValidator.IsCleared(_board, _paths);
                _stateMachine.TryTransition(allCleared ? GameState.Completed : GameState.Idle);
            }
            else
            {
                // Persist incomplete path on board; do NOT clear
                _stateMachine.TryTransition(GameState.Idle);
            }

            _activePath = null;
        }

        public IReadOnlyList<PathModel> GetPaths(int groupId) =>
            _paths.TryGetValue(groupId, out var list) ? list : System.Array.Empty<PathModel>();

        // Compat: returns first path for group
        public PathModel GetPath(int colorId) =>
            _paths.TryGetValue(colorId, out var list) && list.Count > 0 ? list[0] : null;

        public IEnumerable<(int groupId, PathModel path)> AllPaths()
        {
            foreach (var kv in _paths)
                foreach (var p in kv.Value)
                    yield return (kv.Key, p);
        }

        // Removes any PathModel for groupId that has the given node as cells[0] or cells[^1]
        void RemovePathEndingAt(int groupId, Cell node)
        {
            if (!_paths.TryGetValue(groupId, out var group)) return;
            for (int i = group.Count - 1; i >= 0; i--)
            {
                var p = group[i];
                if (p.Cells.Count == 0) { group.RemoveAt(i); continue; }
                bool startsHere = p.Cells[0].X == node.X && p.Cells[0].Y == node.Y;
                bool endsHere   = p.Cells[^1].X == node.X && p.Cells[^1].Y == node.Y;
                if (!startsHere && !endsHere) continue;
                foreach (var c in p.Cells)
                    if (!c.IsNode) _board.ReleasePath(c.X, c.Y);
                group.RemoveAt(i);
            }
        }

        PathModel FindIncompleteWithTail(int groupId, Cell tailCell)
        {
            if (!_paths.TryGetValue(groupId, out var group)) return null;
            foreach (var p in group)
            {
                if (p.IsComplete || p.Cells.Count == 0) continue;
                var last = p.Cells[^1];
                if (last.X == tailCell.X && last.Y == tailCell.Y) return p;
            }
            return null;
        }

        List<PathModel> GetOrCreateGroup(int groupId)
        {
            if (!_paths.ContainsKey(groupId)) _paths[groupId] = new List<PathModel>();
            return _paths[groupId];
        }
    }
}
