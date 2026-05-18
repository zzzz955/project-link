using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ProjectLink.Core
{
    public enum PopupId
    {
        ReturnTitle,
        ExitGame,
        Settings,
        BuyItem,
        Energy,
        StreakChallenge,
        Account,
        Reward,
        StageClear,
        SessionExpired,
        Pause,
        ForceUpdate,
        Maintenance,
        StageDetail,
        ClearNextStageConfirm,
        DailyReward,
        Timeout,
        StreakRewardConfirm,
        ShopItemConfirm,
        ShopItemResult
    }

    public readonly struct PopupRequest
    {
        public PopupRequest(PopupId id, object payload)
        {
            Id = id;
            Payload = payload;
        }

        public PopupId Id { get; }
        public object Payload { get; }
    }

    public abstract class PopupBase : MonoBehaviour
    {
        public virtual void OnBackPressed() => PopupManager.Instance.CloseTop();

        protected void BindOverlayClose()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == "Overlay")
                {
                    button.onClick.AddListener(() => PopupManager.Instance?.CloseTop());
                    return;
                }
            }
        }
    }

    public class PopupManager : MonoBehaviour
    {
        public static event System.Action<PopupRequest> Requested;
        public static PopupManager Instance { get; private set; }

        private readonly Stack<PopupBase> _stack = new();
        private int _lastPushFrame = -1;

        public bool HasPopup => _stack.Count > 0;
        public int Count => _stack.Count;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            if (Instance == this)
                Requested += HandleRequest;
        }

        void OnDisable()
        {
            if (Instance == this)
                Requested -= HandleRequest;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && HasPopup && Time.frameCount > _lastPushFrame)
                CloseTop();
        }

        public static void Request(PopupId id, object payload = null)
        {
            Requested?.Invoke(new PopupRequest(id, payload));
        }

        public void Push(PopupBase popup)
        {
            if (_stack.TryPeek(out var prev))
                prev.gameObject.SetActive(false);

            _stack.Push(popup);
            _lastPushFrame = Time.frameCount;
        }

        public void CloseTop()
        {
            if (!HasPopup) return;

            Destroy(_stack.Pop().gameObject);

            if (_stack.TryPeek(out var top))
                top.gameObject.SetActive(true);
        }

        public void CloseAll()
        {
            while (HasPopup)
                Destroy(_stack.Pop().gameObject);
        }

        public T Open<T>() where T : PopupBase
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(UIManager.Instance.GetLayer(UILayer.Popup), false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var popup = go.AddComponent<T>();
            Push(popup);
            return popup;
        }

        void HandleRequest(PopupRequest request)
        {
            switch (request.Id)
            {
                case PopupId.ReturnTitle:
                    OpenPrefab<ProjectLink.OutGame.UI.ReturnTitlePopup>("Prefabs/UI/ReturnTitlePopup")?.Init();
                    break;
                case PopupId.ExitGame:
                    OpenPrefab<ProjectLink.OutGame.UI.ExitGamePopup>("Prefabs/UI/ExitGamePopup")?.Init(request.Payload as ProjectLink.OutGame.UI.RuntimeNavigationButtons);
                    break;
                case PopupId.Settings:
                    OpenPrefab<ProjectLink.OutGame.UI.SettingPopup>("Prefabs/UI/SettingPopup")?.Init();
                    break;
                case PopupId.BuyItem:
                    OpenPrefab<ProjectLink.OutGame.UI.BuyItemPopup>("Prefabs/UI/BuyItemPopup")?.Init();
                    break;
                case PopupId.Energy:
                    OpenPrefab<ProjectLink.OutGame.UI.EnergyPopup>("Prefabs/UI/EnergyPopup")?.Init();
                    break;
                case PopupId.StreakChallenge:
                    OpenPrefab<ProjectLink.OutGame.UI.StreakChallengePopup>("Prefabs/UI/StreakChallengePopup")?.Init();
                    break;
                case PopupId.Account:
                    OpenPrefab<ProjectLink.OutGame.UI.AccountPopup>("Prefabs/UI/AccountPopup")?.Init();
                    break;
                case PopupId.Reward:
                    OpenPrefab<ProjectLink.OutGame.UI.RewardPopup>("Prefabs/UI/RewardPopup")?.Init();
                    break;
                case PopupId.StageClear:
                    var clearModel = request.Payload as ProjectLink.InGame.UI.StageClearPopupModel;
                    var clearPopup = OpenPrefab<ProjectLink.InGame.UI.ClearPopup>("Prefabs/UI/ClearPopup");
                    if (clearPopup != null)
                        clearPopup.Init(clearModel);
                    else
                        Open<ProjectLink.InGame.UI.ClearPopup>().Init(clearModel);
                    break;
                case PopupId.SessionExpired:
                    Open<ProjectLink.OutGame.UI.SessionExpiredPopup>().Init();
                    break;
                case PopupId.Pause:
                    var resumeCallback = request.Payload as System.Action;
                    var pausePopup = OpenPrefab<ProjectLink.InGame.UI.PausePopup>("Prefabs/UI/PausePopup");
                    if (pausePopup != null)
                        pausePopup.Init(resumeCallback);
                    else
                        Open<ProjectLink.InGame.UI.PausePopup>().Init(resumeCallback);
                    break;
                case PopupId.ForceUpdate:
                    OpenPrefab<ProjectLink.OutGame.UI.ForceUpdatePopup>("Prefabs/UI/ForceUpdatePopup")?.Init();
                    break;
                case PopupId.Maintenance:
                    OpenPrefab<ProjectLink.OutGame.UI.MaintenancePopup>("Prefabs/UI/MaintenancePopup")?.Init(request.Payload as string);
                    break;
                case PopupId.StageDetail:
                    var stageId = request.Payload is int id ? id : GameContext.SelectedStageId;
                    OpenPrefab<ProjectLink.OutGame.UI.StageDetailPopup>("Prefabs/UI/StageDetailPopup")?.Init(stageId);
                    break;
                case PopupId.ClearNextStageConfirm:
                    var confirmModel = request.Payload as ProjectLink.InGame.UI.ClearNextStageConfirmModel;
                    var confirmPopup = OpenPrefab<ProjectLink.InGame.UI.ClearNextStageConfirmPopup>("Prefabs/UI/ClearNextStageConfirmPopup", false);
                    if (confirmPopup != null)
                        confirmPopup.Init(confirmModel);
                    else
                        Open<ProjectLink.InGame.UI.ClearNextStageConfirmPopup>().Init(confirmModel);
                    break;
                case PopupId.DailyReward:
                    OpenPrefab<ProjectLink.OutGame.UI.DailyRewardPopup>("Prefabs/UI/DailyReward")?.Init();
                    break;
                case PopupId.Timeout:
                    var timeoutStageId = request.Payload is int tid ? tid : GameContext.SelectedStageId;
                    var timeoutPopup = OpenPrefab<ProjectLink.InGame.UI.TimeoutPopup>("Prefabs/UI/TimeoutPopup");
                    if (timeoutPopup != null)
                        timeoutPopup.Init(timeoutStageId);
                    else
                        Open<ProjectLink.InGame.UI.TimeoutPopup>().Init(timeoutStageId);
                    break;
                case PopupId.StreakRewardConfirm:
                    var streakModel = request.Payload as ProjectLink.OutGame.UI.StreakRewardConfirmModel;
                    var streakConfirmPopup = OpenPrefab<ProjectLink.OutGame.UI.StreakRewardConfirmPopup>("Prefabs/UI/StreakRewardConfirmPopup", false);
                    if (streakConfirmPopup != null)
                        streakConfirmPopup.Init(streakModel);
                    else
                        Open<ProjectLink.OutGame.UI.StreakRewardConfirmPopup>().Init(streakModel);
                    break;
                case PopupId.ShopItemConfirm:
                    var shopConfirmModel = request.Payload as ProjectLink.OutGame.UI.ShopItemConfirmModel;
                    OpenPrefab<ProjectLink.OutGame.UI.ShopItemConfirmPopup>("Prefabs/UI/ShopItemConfirmPopup")?.Init(shopConfirmModel);
                    break;
                case PopupId.ShopItemResult:
                    var shopResultModel = request.Payload as ProjectLink.OutGame.UI.ShopItemResultModel;
                    OpenPrefab<ProjectLink.OutGame.UI.ShopItemResultPopup>("Prefabs/UI/ShopItemResultPopup")?.Init(shopResultModel);
                    break;
            }
        }

        T OpenPrefab<T>(string resourcePath, bool logError = true) where T : PopupBase
        {
            var prefab = Resources.Load<T>(resourcePath);
            if (prefab == null)
            {
                if (logError)
                    Debug.LogError($"Popup prefab not found: Resources/{resourcePath}");
                return null;
            }

            var popup = Instantiate(prefab, UIManager.Instance.GetLayer(UILayer.Popup), false);
            var rect = popup.GetComponent<RectTransform>();
            if (rect == null)
                rect = popup.gameObject.AddComponent<RectTransform>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Push(popup);
            return popup;
        }
    }
}
