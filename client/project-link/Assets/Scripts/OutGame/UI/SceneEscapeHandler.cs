using ProjectLink.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectLink.OutGame.UI
{
    public sealed class SceneEscapeHandler : MonoBehaviour
    {
        public enum EscapeAction
        {
            None,
            ReturnToTitle,
            ExitGame
        }

        [SerializeField] EscapeAction action;
        [SerializeField] RuntimeNavigationButtons navigation;

        void Awake()
        {
            if (navigation == null)
                navigation = GetComponent<RuntimeNavigationButtons>();
        }

        void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (PopupManager.Instance != null && PopupManager.Instance.HasPopup) return;

            switch (action)
            {
                case EscapeAction.ReturnToTitle:
                    navigation?.OpenReturnTitlePopup();
                    break;
                case EscapeAction.ExitGame:
                    navigation?.OpenExitGamePopup();
                    break;
            }
        }
    }
}

