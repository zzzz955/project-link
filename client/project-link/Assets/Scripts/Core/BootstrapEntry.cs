using UnityEngine;

namespace ProjectLink.Core
{
    public class BootstrapEntry : MonoBehaviour
    {
        void Start()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            CreateManager<DataManager>("DataManager");
            CreateManager<LocalizationManager>("LocalizationManager");
            CreateManager<SoundManager>("SoundManager");
            CreateManager<HapticManager>("HapticManager");
            CreateManager<UIManager>("UIManager");
            CreateManager<PopupManager>("PopupManager");
            CreateManager<ToastPresenter>("ToastPresenter");
            CreateManager<PoolManager>("PoolManager");
            CreateManager<UserDataCache>("UserDataCache");
            CreateManager<SceneLoader>("SceneLoader");
        }

        private static void CreateManager<T>(string goName) where T : MonoBehaviour
        {
            var go = new GameObject(goName);
            go.AddComponent<T>();
        }
    }
}
