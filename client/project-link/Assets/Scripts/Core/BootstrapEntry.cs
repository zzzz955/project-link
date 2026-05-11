using UnityEngine;

namespace ProjectLink.Core
{
    public class BootstrapEntry : MonoBehaviour
    {
        void Start()
        {
            CreateManager<DataManager>("DataManager");
            CreateManager<LocalizationManager>("LocalizationManager");
            CreateManager<SoundManager>("SoundManager");
            CreateManager<HapticManager>("HapticManager");
            CreateManager<UIManager>("UIManager");
            CreateManager<PopupManager>("PopupManager");
            CreateManager<NetworkManager>("NetworkManager");
            NetworkManager.Instance.AuthService = new MockAuthService();
            CreateManager<PoolManager>("PoolManager");
            CreateManager<SceneLoader>("SceneLoader");

            SceneLoader.Instance.LoadScene("Title");
        }

        private static void CreateManager<T>(string goName) where T : MonoBehaviour
        {
            var go = new GameObject(goName);
            go.AddComponent<T>();
        }
    }
}
