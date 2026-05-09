using ProjectLink.Core;
using UnityEngine;

namespace ProjectLink.Services
{
    public static class UiServiceLocator
    {
        static IStaticCatalogService _catalog;

        public static IStaticCatalogService Catalog => _catalog ??= new StaticCatalogService();

        public static IUiDataService UiData
        {
            get
            {
                foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                {
                    if (behaviour is IUiDataService service)
                        return service;
                }

                EnsureNetworkManager();
                var go = new GameObject("HttpUiDataService");
                Object.DontDestroyOnLoad(go);
                return go.AddComponent<HttpUiDataService>();
            }
        }

        static void EnsureNetworkManager()
        {
            if (NetworkManager.Instance != null)
                return;

            var go = new GameObject("NetworkManager");
            go.AddComponent<NetworkManager>();
        }
    }
}
