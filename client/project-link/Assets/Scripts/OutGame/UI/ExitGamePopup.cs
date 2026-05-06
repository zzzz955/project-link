using UnityEngine;

namespace ProjectLink.OutGame.UI
{
    public sealed class ExitGamePopup : ConfirmPopupBase
    {
        public void Init(RuntimeNavigationButtons navigation)
        {
            Build("popup_exit_title", "popup_exit_message", "popup_exit_confirm", new Color(1f, 0.42f, 0.38f, 1f), () => navigation?.QuitApplication());
        }
    }
}
