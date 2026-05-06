using ProjectLink.Core;
using UnityEngine;

namespace ProjectLink.OutGame.UI
{
    public sealed class ReturnTitlePopup : ConfirmPopupBase
    {
        public void Init()
        {
            Build("popup_return_title", "popup_return_message", "popup_return_confirm", new Color(0.19f, 0.91f, 0.78f, 1f), () =>
            {
                PopupManager.Instance.CloseAll();
                if (SceneLoader.Instance != null)
                    SceneLoader.Instance.LoadScene("Title");
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Title");
            });
        }
    }
}
