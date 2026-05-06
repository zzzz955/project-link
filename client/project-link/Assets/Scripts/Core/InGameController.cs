using System.Collections.Generic;
using UnityEngine;
using ProjectLink.Data;
using ProjectLink.InGame.Board;
using ProjectLink.InGame.Input;
using ProjectLink.InGame.Path;
using ProjectLink.InGame.UI;
using ProjectLink.Utils;
using UnityEngine.InputSystem;

namespace ProjectLink.Core
{
    public class InGameController : MonoBehaviour
    {
        public static InGameController Instance { get; private set; }

        int               _stageId;
        [SerializeField] float _cellSize = 1f;

        Board             _board;
        GameStateMachine  _stateMachine;
        PathDrawer        _drawer;
        BoardView         _boardView;
        TouchInputHandler _touchInput;
        InGameHUD         _hud;

        readonly Dictionary<int, PathView> _pathViews = new();
        int     _activeColorId;
        Vector2 _lastDragPos;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            _stageId = GameContext.SelectedStageId;

            var stageData = StageLoader.Load(_stageId);
            if (stageData == null) return;

            _board        = new Board(stageData);
            _stateMachine = new GameStateMachine();
            _drawer       = new PathDrawer(_board, _stateMachine);

            FitCameraToBoard();

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

            var hudGo = new GameObject("InGameHUD");
            hudGo.transform.SetParent(transform);
            _hud = hudGo.AddComponent<InGameHUD>();
            _hud.Init(_stageId, _board.ColorIds.Count, GetConnectedCount);
            _hud.OnPausePressed = OpenPausePopup;

            _touchInput.OnDragStart        += HandleDragStart;
            _touchInput.OnDragMove         += HandleDragMove;
            _touchInput.OnDragEnd          += HandleDragEnd;
            _stateMachine.OnStateChanged   += HandleStateChanged;
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (PopupManager.Instance != null && PopupManager.Instance.HasPopup) return;

            OpenPausePopup();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_touchInput == null) return;
            _touchInput.OnDragStart -= HandleDragStart;
            _touchInput.OnDragMove  -= HandleDragMove;
            _touchInput.OnDragEnd   -= HandleDragEnd;
        }

        public void SetInputEnabled(bool enabled)
        {
            if (_touchInput != null) _touchInput.enabled = enabled;
        }

        void OpenPausePopup()
        {
            if (PopupManager.Instance == null || PopupManager.Instance.HasPopup) return;

            SetInputEnabled(false);
            PopupManager.Instance.Open<PausePopup>().Init(() => SetInputEnabled(true));
        }

        int GetConnectedCount()
        {
            if (_drawer == null || _board == null) return 0;
            int count = 0;
            foreach (int id in _board.ColorIds)
            {
                var path = _drawer.GetPath(id);
                if (path != null && path.IsComplete) count++;
            }
            return count;
        }

        void HandleDragStart(Vector2 worldPos)
        {
            var cell = InputSnapper.Snap(worldPos, _board, _cellSize);
            if (!_drawer.TryStartPath(cell)) return;

            _activeColorId = cell.ColorId;
            _lastDragPos   = worldPos;
            EnsurePathView(_activeColorId);
            _boardView.Refresh();
            _pathViews[_activeColorId].Refresh();
        }

        void HandleDragMove(Vector2 worldPos)
        {
            if (_stateMachine.Current != GameState.Drawing) return;

            Vector2 delta    = worldPos - _lastDragPos;
            float   stepSize = _cellSize * 0.5f;
            int     steps    = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / stepSize));

            for (int i = 1; i <= steps; i++)
            {
                var cell = InputSnapper.Snap(_lastDragPos + delta * (i / (float)steps), _board, _cellSize);
                _drawer.ProcessCell(cell);
            }

            _lastDragPos = worldPos;
            _boardView.Refresh();
            _pathViews[_activeColorId].Refresh();
        }

        void HandleDragEnd(Vector2 worldPos)
        {
            _drawer.EndPath();
            _boardView.Refresh();
            foreach (var pv in _pathViews.Values) pv.Refresh();
            _hud?.Refresh();
        }

        void HandleStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Completed)
            {
                HapticManager.PlayConnected();
                DataManager.Instance.ClearStage(_stageId, 3);
                var popup = PopupManager.Instance.Open<ClearPopup>();
                popup.Init(_stageId, 3);
            }

            // Erase completed: sync views (EraseController already refreshed BoardView internally)
            if (from == GameState.Erasing && to == GameState.Idle)
                foreach (var pv in _pathViews.Values) pv.Refresh();
        }

        void FitCameraToBoard()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float boardW = _board.Width  * _cellSize;
            float boardH = _board.Height * _cellSize;
            float padding = _cellSize;

            float sizeByHeight = (boardH + padding) * 0.5f;
            float sizeByWidth  = (boardW + padding) * 0.5f / cam.aspect;

            cam.orthographic     = true;
            cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
            cam.transform.position = new Vector3(0f, 0f, -10f);
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
