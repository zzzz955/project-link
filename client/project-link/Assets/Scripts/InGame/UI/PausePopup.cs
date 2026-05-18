using System;
using ProjectLink.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.InGame.UI
{
    public class PausePopup : PopupBase
    {
        [SerializeField] Button btnResume;
        [SerializeField] Button btnRetry;
        [SerializeField] Button btnLobby;

        Action _onResume;

        public void Init() => Init(null);

        public void Init(Action onResume)
        {
            _onResume = onResume;

            if (btnResume != null) btnResume.onClick.AddListener(DoResume);
            if (btnRetry  != null) btnRetry .onClick.AddListener(OnRetry);
            if (btnLobby  != null) btnLobby .onClick.AddListener(OnLobby);

            FindButtonInChildren("Btn_Close")?.onClick.AddListener(DoResume);
            FindButtonInChildren("Overlay")?.onClick.AddListener(DoResume);
        }

        public override void OnBackPressed() => DoResume();

        void DoResume()
        {
            PopupManager.Instance.CloseTop();
            _onResume?.Invoke();
        }

        void OnRetry()
        {
            PopupManager.Instance.CloseAll();
            if (InGameController.Instance != null)
                InGameController.Instance.AbandonStageAndLoad("Game");
            else
                SceneLoader.Instance.LoadScene("Game");
        }

        void OnLobby()
        {
            PopupManager.Instance.CloseAll();
            if (InGameController.Instance != null)
                InGameController.Instance.AbandonStageAndLoad("Lobby");
            else
                SceneLoader.Instance.LoadScene("Lobby");
        }

        Button FindButtonInChildren(string childName)
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
                if (btn.name == childName) return btn;
            return null;
        }
    }
}
