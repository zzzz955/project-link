using System.Collections;
using ProjectLink.Services;
using TMPro;
using ProjectLink.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace ProjectLink.OutGame.UI
{
    public sealed class TitleWireframeController : MonoBehaviour
    {
        [SerializeField] Button btnTapToStart;
        [SerializeField] Button btnGoogle;
        [SerializeField] Button btnApple;
        [SerializeField] TextMeshProUGUI txtVersion;
        [SerializeField] string devGoogleIdToken = "";

        TitleViewModel _viewModel;
        bool _blockingPopupShown;
        bool _lobbyLoadRequested;

        void Start()
        {
            UserDataCache.Instance?.Clear();

            BindButton(btnTapToStart, OnTapToStart);
            BindButton(btnGoogle, OnGoogle);
            BindButton(btnApple, OnApple);

            _viewModel = new TitleViewModel(UiServiceLocator.UiData, NetworkManager.Instance);
            _viewModel.Changed += Render;
            _viewModel.Load();
        }

        void OnDestroy()
        {
            if (_viewModel != null)
                _viewModel.Changed -= Render;
        }

        void OnTapToStart()
        {
            _viewModel?.TapToStart();
        }

        void OnGoogle()
        {
            _viewModel?.LoginGoogle(devGoogleIdToken);
        }

        void OnApple()
        {
            _viewModel?.LoginApple();
        }

        void Render()
        {
            if (_viewModel == null) return;

            SetText(txtVersion, _viewModel.Version);
            bool interactable = !_viewModel.IsLoading && _viewModel.TitleControlsVisible;
            SetButtonsInteractable(interactable);

            if (_viewModel.RequiresForceUpdate)
            {
                if (!_blockingPopupShown)
                {
                    _blockingPopupShown = true;
                    PopupManager.Request(PopupId.ForceUpdate);
                }
                return;
            }

            if (_viewModel.IsMaintenance && _viewModel.IsAuthenticated)
            {
                if (!_blockingPopupShown)
                {
                    _blockingPopupShown = true;
                    PopupManager.Request(PopupId.Maintenance, _viewModel.MaintenanceMessage);
                }
                return;
            }

            if (_viewModel.EnterLobbyRequested && !_lobbyLoadRequested)
            {
                _lobbyLoadRequested = true;
                StartCoroutine(LoadLobbyWhenReady());
            }
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (btnTapToStart != null) btnTapToStart.interactable = interactable;
            if (btnGoogle != null) btnGoogle.interactable = interactable;
            if (btnApple != null) btnApple.interactable = interactable;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);
        }

        IEnumerator LoadLobbyWhenReady()
        {
            while (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading)
                yield return null;

            LoadLobby();
        }

        void LoadLobby()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.LoadScene("Lobby");
                return;
            }

            SceneManager.LoadScene("Lobby");
        }
    }
}
