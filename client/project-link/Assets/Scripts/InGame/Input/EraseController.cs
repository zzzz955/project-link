using System.Collections;
using UnityEngine;
using ProjectLink.Core;
using ProjectLink.InGame.Path;
using ProjectLink.InGame.UI;
using ProjectLink.Utils;

namespace ProjectLink.InGame.Input
{
    public class EraseController : MonoBehaviour
    {
        [SerializeField] float _fillDuration = 0.7f;

        TouchInputHandler                   _input;
        GameStateMachine                    _fsm;
        PathDrawer                          _drawer;
        ProjectLink.InGame.Board.Board      _board;
        CircularGauge                       _gauge;
        ProjectLink.InGame.Board.BoardView  _boardView;
        float                               _cellSize;

        Coroutine _fillCoroutine;

        public void Init(
            TouchInputHandler input,
            GameStateMachine fsm,
            PathDrawer drawer,
            ProjectLink.InGame.Board.Board board,
            CircularGauge gauge,
            ProjectLink.InGame.Board.BoardView boardView,
            float cellSize)
        {
            _input     = input;
            _fsm       = fsm;
            _drawer    = drawer;
            _board     = board;
            _gauge     = gauge;
            _boardView = boardView;
            _cellSize  = cellSize;

            _input.OnLongPressStart    += OnLongPressStarted;
            _input.OnLongPressCanceled += Cancel;
        }

        void OnDestroy()
        {
            if (_input == null) return;
            _input.OnLongPressStart    -= OnLongPressStarted;
            _input.OnLongPressCanceled -= Cancel;
        }

        void OnLongPressStarted(Vector2 worldPos)
        {
            var cell = InputSnapper.Snap(worldPos, _board, _cellSize);
            if (!cell.IsNode) return;

            var path = _drawer.GetPath(cell.ColorId);
            if (path == null || !path.IsComplete) return;
            if (!_fsm.TryTransition(GameState.Erasing)) return;

            var gaugePos = new Vector3(
                GridUtils.CellToWorld(cell.X, cell.Y, _board.Width, _board.Height, _cellSize).x,
                GridUtils.CellToWorld(cell.X, cell.Y, _board.Width, _board.Height, _cellSize).y,
                0f);

            _gauge.Show(gaugePos, ColorPalette.Get(cell.ColorId));
            _fillCoroutine = StartCoroutine(FillGauge(cell.ColorId));
        }

        void Cancel()
        {
            if (_fillCoroutine != null) StopCoroutine(_fillCoroutine);
            _fillCoroutine = null;
            _gauge.Hide();
            if (_fsm.Current == GameState.Erasing) _fsm.TryTransition(GameState.Idle);
        }

        IEnumerator FillGauge(int colorId)
        {
            float elapsed = 0f;
            while (elapsed < _fillDuration)
            {
                elapsed += Time.deltaTime;
                _gauge.SetProgress(elapsed / _fillDuration);
                yield return null;
            }

            _gauge.SetProgress(1f);
            _board.ClearPathCells(colorId);
            _drawer.GetPath(colorId)?.Clear();
            _boardView.Refresh();
            _gauge.Hide();
            _fsm.TryTransition(GameState.Idle);
            _fillCoroutine = null;
        }
    }
}
