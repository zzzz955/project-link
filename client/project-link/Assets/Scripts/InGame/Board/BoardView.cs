using UnityEngine;
using ProjectLink.Utils;

namespace ProjectLink.InGame.Board
{
    public class BoardView : MonoBehaviour
    {
        Board _board;
        CellView[,] _cellViews;
        float _cellSize;

        public void Init(Board board, float cellSize = 1f)
        {
            _board    = board;
            _cellSize = cellSize;

            if (_cellViews != null)
            {
                foreach (var cv in _cellViews)
                    if (cv != null) Destroy(cv.gameObject);
            }

            _cellViews = new CellView[board.Width, board.Height];

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var go  = new GameObject($"Cell_{x}_{y}");
                    go.transform.SetParent(transform, false);
                    go.transform.localPosition = GridUtils.CellToWorld(x, y, board.Width, board.Height, cellSize);

                    var cv = go.AddComponent<CellView>();
                    cv.Init(board.GetCell(x, y), cellSize);
                    _cellViews[x, y] = cv;
                }
            }
        }

        public void Refresh()
        {
            foreach (var cv in _cellViews)
                cv?.Refresh();
        }

        public CellView GetCellView(int x, int y) => _cellViews[x, y];

        public void SetHighlights(System.Func<Cell, bool> predicate, Color color)
        {
            for (int x = 0; x < _board.Width; x++)
            for (int y = 0; y < _board.Height; y++)
                _cellViews[x, y]?.SetHighlight(predicate(_board.GetCell(x, y)), color);
        }

        public void ClearHighlights()
        {
            foreach (var cv in _cellViews)
                cv?.ClearHighlight();
        }
    }
}
