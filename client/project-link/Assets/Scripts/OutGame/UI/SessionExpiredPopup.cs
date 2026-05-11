using ProjectLink.Core;
using UnityEngine;

namespace ProjectLink.OutGame.UI
{
    public sealed class SessionExpiredPopup : ConfirmPopupBase
    {
        public void Init()
        {
            Build(
                "popup_session_expired_title",
                "popup_session_expired_message",
                "popup_return_confirm",
                new Color(0.18f, 0.52f, 0.98f, 1f),
                ReturnToTitle);
        }

        static void ReturnToTitle()
        {
            PopupManager.Instance?.CloseAll();
            SceneLoader.Instance?.LoadScene("Title");
        }
    }
}
