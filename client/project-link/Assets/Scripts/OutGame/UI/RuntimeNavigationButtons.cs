using ProjectLink.Core;
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
            LoadScene(TitleSceneName);
        }

        public void OpenReturnTitlePopup()
        {
            if (PopupManager.Instance == null)
            {
                LoadTitle();
                return;
            }

            PopupManager.Instance.Open<ReturnTitlePopup>().Init();
        }

        public void OpenExitGamePopup()
        {
            if (PopupManager.Instance == null)
                return;

            PopupManager.Instance.Open<ExitGamePopup>().Init(this);
        }

        public void LoadLobby()
        {
            LoadScene(LobbySceneName);
        }

        public void LoadGame()
        {
            LoadGameWithStage(defaultStageId);
        }

        public void LoadGameWithStage(int stageId)
        {
            GameContext.SelectedStageId = Mathf.Max(1, stageId);
            LoadScene(GameSceneName);
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
    }
}
