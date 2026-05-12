using ProjectLink.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class ReturnTitlePopup : PopupBase
    {
        [SerializeField] Button closeIconButton;
        [SerializeField] Button cancelButton;
        [SerializeField] Button confirmButton;

        bool _initialized;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindOverlayClose();
            BindClose(closeIconButton);
            BindClose(cancelButton);
            BindConfirm(confirmButton);
        }

        void ResolveMissingReferences()
        {
            closeIconButton ??= FindButton("CloseIconButton");
            cancelButton ??= FindButton("CancelButton");
            confirmButton ??= FindButton("ConfirmButton");
        }

        void BindClose(Button button)
        {
            if (button != null)
                button.onClick.AddListener(CloseTop);
        }

        void BindConfirm(Button button)
        {
            if (button != null)
                button.onClick.AddListener(ReturnToTitle);
        }

        Button FindButton(string buttonName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                    return button;
            }

            return null;
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }

        static void ReturnToTitle()
        {
            PopupManager.Instance?.CloseAll();
            if (SceneLoader.Instance != null)
                SceneLoader.Instance.LoadScene("Title");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
        }
    }
}
