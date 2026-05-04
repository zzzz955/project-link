using System.Collections.Generic;
using UnityEngine;
using ProjectLink.Data;
using ProjectLink.InGame.Board;
using ProjectLink.InGame.Input;
using ProjectLink.InGame.Path;
using ProjectLink.InGame.UI;
using ProjectLink.Utils;

namespace ProjectLink.Core
{
    public class InGameController : MonoBehaviour
    {
        [SerializeField] int   _stageId  = 1;
        [SerializeField] float _cellSize = 1f;

        Board             _board;
        GameStateMachine  _stateMachine;
        PathDrawer        _drawer;
        BoardView         _boardView;
        TouchInputHandler _touchInput;

        readonly Dictionary<int, PathView> _pathViews = new();
        int _activeColorId;

        void Start()
        {
            var stageData = StageLoader.Load(_stageId);
            if (stageData == null) return;

            _board        = new Board(stageData);
            _stateMachine = new GameStateMachine();
            _drawer       = new PathDrawer(_board, _stateMachine);

            var boardGo = new GameObject("BoardView");
            boardGo.transform.SetParent(transform);
            _boardView = boardGo.AddComponent<BoardView>();
            _boardView.Init(_board, _cellSize);

            var gaugeGo = new GameObject("CircularGauge");
            gaugeGo.transform.SetParent(transform);
            var gauge = gaugeGo.AddComponent<CircularGauge>();

            _touchInput = GetComponent<TouchInputHandler>();
            if (_touchInput == null) _touchInput = gameObject.AddComponent<TouchInputHandler>();

            var eraseGo = new GameObject("EraseController");
            eraseGo.transform.SetParent(transform);
            var erase = eraseGo.AddComponent<EraseController>();
            erase.Init(_touchInput, _stateMachine, _drawer, _board, gauge, _boardView, _cellSize);

            _touchInput.OnDragStart        += HandleDragStart;
            _touchInput.OnDragMove         += HandleDragMove;
            _touchInput.OnDragEnd          += HandleDragEnd;
            _stateMachine.OnStateChanged   += HandleStateChanged;
        }

        void OnDestroy()
        {
            if (_touchInput == null) return;
            _touchInput.OnDragStart -= HandleDragStart;
            _touchInput.OnDragMove  -= HandleDragMove;
            _touchInput.OnDragEnd   -= HandleDragEnd;
        }

        void HandleDragStart(Vector2 worldPos)
        {
            var cell = InputSnapper.Snap(worldPos, _board, _cellSize);
            if (!_drawer.TryStartPath(cell)) return;

            _activeColorId = cell.ColorId;
            EnsurePathView(_activeColorId);
            _boardView.Refresh();
            _pathViews[_activeColorId].Refresh();
        }

        void HandleDragMove(Vector2 worldPos)
        {
            if (_stateMachine.Current != GameState.Drawing) return;
            var cell = InputSnapper.Snap(worldPos, _board, _cellSize);
            _drawer.ProcessCell(cell);
            _boardView.Refresh();
            _pathViews[_activeColorId].Refresh();
        }

        void HandleDragEnd(Vector2 worldPos)
        {
            _drawer.EndPath();
            _boardView.Refresh();
            foreach (var pv in _pathViews.Values) pv.Refresh();
        }

        void HandleStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Completed)
                HapticManager.PlayConnected();

            // Erase completed: sync views (EraseController already refreshed BoardView internally)
            if (from == GameState.Erasing && to == GameState.Idle)
                foreach (var pv in _pathViews.Values) pv.Refresh();
        }

        void EnsurePathView(int colorId)
        {
            if (_pathViews.ContainsKey(colorId)) return;
            var go = new GameObject($"PathView_{colorId}");
            go.transform.SetParent(transform);
            var pv = go.AddComponent<PathView>();
            pv.Init(_drawer.GetPath(colorId), _board.Width, _board.Height, _cellSize);
            _pathViews[colorId] = pv;
        }
    }
}
