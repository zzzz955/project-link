using ProjectLink.Core;
using ProjectLink.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectLink.OutGame.UI
{
    public sealed class RuntimeNavigationButtons : MonoBehaviour
    {
        const string TitleSceneName = "Title";
        const string LobbySceneName = "Lobby";
        const string GameSceneName = "Game";

        [SerializeField]
        [Min(1)]
        int defaultStageId = 1;

        public void LoadTitle()
        {
            GameContext.SuppressNextTitleSilentLogin();
            LoadScene(TitleSceneName);
        }

        public void OpenReturnTitlePopup()
        {
            if (PopupManager.Instance == null)
            {
                LoadTitle();
                return;
            }

            PopupManager.Request(PopupId.ReturnTitle);
        }

        public void OpenExitGamePopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.ExitGame, this);
        }

        public void OpenSettingsPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.Settings);
        }

        public void OpenAccountPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.Account);
        }

        public void OpenStreakChallengePopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.StreakChallenge);
        }

        public void OpenRewardPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.Reward);
        }

        public void OpenDailyRewardPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.DailyReward);
        }

        public void OpenPausePopup()
        {
            if (Core.InGameController.Instance != null)
            {
                Core.InGameController.Instance.OpenPausePopup();
                return;
            }
            if (PopupManager.Instance == null) return;
            PopupManager.Request(PopupId.Pause);
        }

        public void OpenBuyItemPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.BuyItem);
        }

        public void OpenEnergyPopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Request(PopupId.Energy);
        }

        public void LoadLobby()
        {
            LoadScene(LobbySceneName);
        }

        public void LoadGame()
        {
            OpenStageDetail(GameContext.SelectedStageId > 0 ? GameContext.SelectedStageId : defaultStageId);
        }

        public void LoadGameWithStage(int stageId)
        {
            OpenStageDetail(stageId);
        }

        public static void OpenStageDetail(int stageId)
        {
            stageId = Mathf.Max(1, stageId);
            GameContext.SelectedStageId = stageId;

            if (PopupManager.Instance != null)
            {
                PopupManager.Request(PopupId.StageDetail, stageId);
                return;
            }

            EnterStage(stageId);
        }

        public static void EnterStage(int stageId)
        {
            stageId = Mathf.Max(1, stageId);
            GameContext.SelectedStageId = stageId;

            if (!string.IsNullOrEmpty(GameContext.StageSessionToken))
            {
                LoadScene(GameSceneName);
                return;
            }

            var uiData = UiServiceLocator.UiData;
            uiData.StartStage(stageId, result =>
            {
                if (!result.IsSuccess)
                {
                    if (result.ErrorCode == "INSUFFICIENT_STAMINA")
                        LoadLobbyWithEnergyPopup();
                    else
                        UiEventBus.Publish(new UiErrorRaised("stage_start", result.ErrorCode, result.ErrorMessage));
                    return;
                }

                var response = result.Value;
                GameContext.SetStageSession(response.SessionToken, response.MoveLimit, response.TimeLimitSeconds);
                LoadScene(GameSceneName);
            });
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void LoadScene(string sceneName)
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        static void LoadLobbyWithEnergyPopup()
        {
            GameContext.ClearStageSession();

            if (SceneManager.GetActiveScene().name == LobbySceneName)
            {
                PopupManager.Request(PopupId.Energy);
                return;
            }

            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene(LobbySceneName, () => PopupManager.Request(PopupId.Energy));
                return;
            }

            SceneManager.LoadScene(LobbySceneName);
            PopupManager.Request(PopupId.Energy);
        }
    }

}
