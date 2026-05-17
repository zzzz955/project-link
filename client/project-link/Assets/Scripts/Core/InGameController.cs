using System.Collections.Generic;
using UnityEngine;
using ProjectLink.Data;
using ProjectLink.InGame;
using ProjectLink.InGame.Board;
using ProjectLink.InGame.Camera;
using ProjectLink.InGame.Input;
using ProjectLink.InGame.Path;
using ProjectLink.InGame.UI;
using ProjectLink.Services;
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
        StageTimer        _timer;
        IUiDataService    _uiData;
        int               _moveLimit;
        int               _movesUsed;
        bool              _stageEndSubmitted;
        bool              _pathMoved;

        readonly Dictionary<PathModel, PathView> _pathViewMap = new();
        int     _activeGroupId;
        Vector2 _lastDragPos;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            _stageId = GameContext.SelectedStageId;
            _uiData = UiServiceLocator.UiData;

            var stageData = StageLoader.Load(_stageId);
            if (stageData == null) return;

            ColorPalette.Init(stageData.NodeColors);

            _board        = new Board(stageData);
            _stateMachine = new GameStateMachine();
            _drawer       = new PathDrawer(_board, _stateMachine);

            FitCameraToBoard();

            // Add camera controller for zoom/pan support
            if (Camera.main != null)
            {
                var camCtrl = Camera.main.gameObject.GetComponent<BoardCameraController>();
                if (camCtrl == null) camCtrl = Camera.main.gameObject.AddComponent<BoardCameraController>();
                camCtrl.Init(_board, _cellSize);
            }

            var boardGo = new GameObject("BoardView");
            boardGo.transform.SetParent(transform);
            _boardView = boardGo.AddComponent<BoardView>();
            _boardView.Init(_board, _cellSize);

            _touchInput = GetComponent<TouchInputHandler>();
            if (_touchInput == null) _touchInput = gameObject.AddComponent<TouchInputHandler>();

            var hudGo = new GameObject("InGameHUD");
            hudGo.transform.SetParent(transform);
            _hud = hudGo.AddComponent<InGameHUD>();
            _hud.Init(_stageId, _board.GroupIds.Count, GetConnectedCount, stageData.TimeLimit);
            _hud.OnPausePressed = OpenPausePopup;
            _hud.SetMoveDisplay(_movesUsed, _moveLimit);

            _timer = new StageTimer();
            _timer.OnTimeUp += HandleTimeUp;
            if (stageData.TimeLimit > 0)
                _timer.Start(stageData.TimeLimit);

            _touchInput.OnDragStart      += HandleDragStart;
            _touchInput.OnDragMove       += HandleDragMove;
            _touchInput.OnDragEnd        += HandleDragEnd;
            _stateMachine.OnStateChanged += HandleStateChanged;

            SetInputEnabled(false);
            if (!string.IsNullOrEmpty(GameContext.StageSessionToken))
                ApplyStageSession();
            else
                _uiData.StartStage(_stageId, HandleStageStarted);
        }

        void Update()
        {
            _timer?.Tick();
            if (_timer != null && _timer.HasLimit && !_timer.IsExpired)
                _hud?.SetTimerDisplay(_timer.Remaining);

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

        public void OpenPausePopup()
        {
            if (PopupManager.Instance == null || PopupManager.Instance.HasPopup) return;

            SetInputEnabled(false);
            _timer?.Pause();
            PopupManager.Instance.Open<PausePopup>().Init(() =>
            {
                SetInputEnabled(true);
                _timer?.Resume();
            });
        }

        public void AbandonStageAndLoad(string sceneName)
        {
            if (_stageEndSubmitted || string.IsNullOrEmpty(GameContext.StageSessionToken) || _uiData == null)
            {
                GameContext.ClearStageSession();
                SceneLoader.Instance.LoadScene(sceneName);
                return;
            }

            _stageEndSubmitted = true;
            SetInputEnabled(false);
            _timer?.Pause();

            var token = GameContext.StageSessionToken;
            var elapsedMs = GameContext.StageStartedAtMs > 0
                ? System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - GameContext.StageStartedAtMs
                : 0L;

            GameContext.ClearStageSession();
            _uiData.EndStage(_stageId, token, "fail", elapsedMs, _movesUsed, _ =>
            {
                SceneLoader.Instance.LoadScene(sceneName);
            });
        }

        int GetConnectedCount()
        {
            if (_drawer == null || _board == null) return 0;
            int count = 0;
            foreach (int groupId in _board.GroupIds)
                if (PathValidator.IsGroupConnected(_board.GetGroupNodes(groupId), _drawer.GetPaths(groupId)))
                    count++;
            return count;
        }

        void HandleDragStart(Vector2 worldPos)
        {
            var cell = InputSnapper.Snap(worldPos, _board, _cellSize);
            if (!_drawer.TryStartPath(cell)) return;

            _pathMoved     = false;
            _activeGroupId = _drawer.ActivePath.ColorId;
            _lastDragPos   = worldPos;
            CleanupStalePathViews();   // Issue 2: remove views for paths cleared by TryStartPath
            EnsurePathViews(_activeGroupId);
            _boardView.Refresh();
            RefreshGroupViews(_activeGroupId);
        }

        void HandleDragMove(Vector2 worldPos)
        {
            if (_stateMachine.Current != GameState.Drawing) return;
            _pathMoved = true;

            Vector2 delta    = worldPos - _lastDragPos;
            float   stepSize = _cellSize * 0.5f;
            int     steps    = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / stepSize));

            for (int i = 1; i <= steps; i++)
            {
                var cell = InputSnapper.Snap(_lastDragPos + delta * (i / (float)steps), _board, _cellSize);
                _drawer.ProcessCell(cell);
            }

            _lastDragPos = worldPos;
            CleanupStalePathViews();   // Issue 1: remove views for overwritten/merged paths
            EnsurePathViews(_activeGroupId);
            _boardView.Refresh();
            foreach (var pv in _pathViewMap.Values) pv.Refresh();
        }

        void HandleDragEnd(Vector2 worldPos)
        {
            bool counted = _pathMoved;
            _pathMoved = false;
            _drawer.EndPath();
            if (counted) _movesUsed++;
            CleanupStalePathViews();
            _boardView.Refresh();
            foreach (var pv in _pathViewMap.Values) pv.Refresh();
            _hud?.Refresh();
            _hud?.SetMoveDisplay(_movesUsed, _moveLimit);
        }

        // Destroys PathView GameObjects for PathModels no longer tracked by PathDrawer.
        void CleanupStalePathViews()
        {
            var active = new HashSet<PathModel>();
            foreach (var (_, path) in _drawer.AllPaths()) active.Add(path);

            var stale = new List<PathModel>();
            foreach (var kv in _pathViewMap)
                if (!active.Contains(kv.Key)) stale.Add(kv.Key);

            foreach (var pm in stale)
            {
                if (_pathViewMap.TryGetValue(pm, out var pv) && pv != null)
                    Destroy(pv.gameObject);
                _pathViewMap.Remove(pm);
            }
        }

        void HandleStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Completed)
            {
                HapticManager.PlayConnected();
                SubmitStageEnd("success");
            }
        }

        void HandleStageStarted(ServiceResult<ProjectLink.Contracts.Stage.StageStartResponse> result)
        {
            if (!result.IsSuccess)
            {
                Debug.LogWarning($"Stage start failed: {result.ErrorCode} {result.ErrorMessage}");
                if (result.ErrorCode != "INSUFFICIENT_STAMINA")
                    UiEventBus.Publish(new UiErrorRaised("stage_start", result.ErrorCode, result.ErrorMessage));

                GameContext.ClearStageSession();
                if (SceneLoader.Instance != null)
                    SceneLoader.Instance.LoadScene("Lobby", () =>
                    {
                        if (result.ErrorCode == "INSUFFICIENT_STAMINA")
                            PopupManager.Request(PopupId.Energy);
                    });
                return;
            }

            var response = result.Value;
            GameContext.SetStageSession(response.SessionToken, response.MoveLimit, response.TimeLimitSeconds);
            ApplyStageSession();
        }

        void ApplyStageSession()
        {
            _moveLimit = GameContext.MoveLimit;
            _hud?.SetMoveDisplay(_movesUsed, _moveLimit);
            SetInputEnabled(true);

            if (GameContext.TimeLimitSeconds > 0 && GameContext.TimeLimitSeconds != _timer.Remaining)
            {
                _timer.Start(GameContext.TimeLimitSeconds);
                _hud?.SetTimerDisplay(GameContext.TimeLimitSeconds);
            }
        }

        void SubmitStageEnd(string result)
        {
            if (_stageEndSubmitted) return;
            _stageEndSubmitted = true;
            SetInputEnabled(false);
            _timer?.Pause();

            var elapsedMs = GameContext.StageStartedAtMs > 0
                ? System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - GameContext.StageStartedAtMs
                : 0L;

            _uiData.EndStage(_stageId, GameContext.StageSessionToken, result, elapsedMs, _movesUsed, stageResult =>
            {
                if (!stageResult.IsSuccess)
                {
                    Debug.LogError($"Stage end failed: {stageResult.ErrorCode} {stageResult.ErrorMessage}");
                    GameContext.ClearStageSession();
                    OpenClearPopup(new StageClearPopupModel(_stageId, 3, 0, 0, _movesUsed, _moveLimit, elapsedMs, 0, false, _stageId + 1, true, null));
                    return;
                }

                var value = stageResult.Value;
                GameContext.ClearStageSession();
                DataManager.Instance.ClearStage(_stageId, value.Stars);
                var nextStageId = value.NextStageId ?? _stageId + 1;
                var nextStageUnlocked = value.NextStageUnlocked;
                var streakDirective = value.StreakChallenge?.NavigationDirective ?? "NONE";
                if (streakDirective == "RETURN_TO_LOBBY")
                    nextStageUnlocked = false;
                if (streakDirective == "OPEN_EVENT_POPUP")
                    GameContext.ShouldOpenStreakPopupOnLobby = true;

                var model = new StageClearPopupModel(
                    _stageId,
                    value.Stars,
                    value.SoftReward,
                    value.SoftBalanceAfter,
                    value.MovesUsed,
                    value.MoveLimit,
                    value.AdjustedElapsedMs,
                    value.Score,
                    value.IsBestRecord,
                    nextStageId,
                    nextStageUnlocked,
                    value.RankPercentile);
                model.StreakDirective = streakDirective;
                OpenClearPopup(model);
            });
        }

        void OpenClearPopup(StageClearPopupModel model)
        {
            SetInputEnabled(false);
            _timer?.Pause();
            PopupManager.Request(PopupId.StageClear, model);
        }

        public void ExtendTime(int seconds)
        {
            PopupManager.Instance.CloseAll();
            _timer.Start(seconds);
            SetInputEnabled(true);
        }

        void HandleTimeUp()
        {
            if (_stateMachine.Current == GameState.Completed) return;

            SetInputEnabled(false);

            if (_stateMachine.Current == GameState.Drawing)
                _drawer.EndPath();

            if (_stateMachine.Current == GameState.Completed) return;

            _boardView.Refresh();
            foreach (var pv in _pathViewMap.Values) pv.Refresh();
            _hud?.SetTimerDisplay(0f);

            PopupManager.Request(PopupId.Timeout, _stageId);
        }

        void FitCameraToBoard()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float boardW  = _board.Width  * _cellSize;
            float boardH  = _board.Height * _cellSize;
            float padding = _cellSize;

            float sizeByHeight = (boardH + padding) * 0.5f;
            float sizeByWidth  = (boardW + padding) * 0.5f / cam.aspect;

            cam.orthographic     = true;
            cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
            cam.transform.position = new Vector3(0f, 0f, -10f);
        }

        void EnsurePathViews(int groupId)
        {
            foreach (var path in _drawer.GetPaths(groupId))
            {
                if (_pathViewMap.ContainsKey(path)) continue;
                var go = new GameObject($"PathView_{groupId}");
                go.transform.SetParent(transform);
                var pv = go.AddComponent<PathView>();
                pv.Init(path, _board.Width, _board.Height, _cellSize);
                _pathViewMap[path] = pv;
            }
        }

        void RefreshGroupViews(int groupId)
        {
            foreach (var path in _drawer.GetPaths(groupId))
                if (_pathViewMap.TryGetValue(path, out var pv)) pv.Refresh();
        }
    }
}
