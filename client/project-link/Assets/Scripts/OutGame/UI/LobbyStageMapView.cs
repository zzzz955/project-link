using System.Collections.Generic;
using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class LobbyStageMapView : MonoBehaviour
    {
        const string PoolKey = "LobbyStageNode";

        [SerializeField] RectTransform nodeHost;
        [SerializeField] GameObject stageNodePrefab;
        [SerializeField] TextMeshProUGUI pageLabel;
        [SerializeField] Button previousPageButton;
        [SerializeField] Button nextPageButton;
        [SerializeField] int totalStageCount = 1000;
        [SerializeField] int stagesPerPage = 20;
        [SerializeField] int playableStageCount = 2;

        readonly List<GameObject> _activeNodes = new();
        int _currentPage;

        void Start()
        {
            if (previousPageButton != null)
                previousPageButton.onClick.AddListener(PreviousPage);

            if (nextPageButton != null)
                nextPageButton.onClick.AddListener(NextPage);

            Build();
        }

        void OnDestroy()
        {
            if (PoolManager.Instance == null || !PoolManager.Instance.HasPool(PoolKey)) return;

            for (int i = 0; i < _activeNodes.Count; i++)
            {
                if (_activeNodes[i] != null)
                    PoolManager.Instance.Return(PoolKey, _activeNodes[i]);
            }

            _activeNodes.Clear();
        }

        public void Build()
        {
            if (nodeHost == null || stageNodePrefab == null) return;

            stageNodePrefab.SetActive(false);

            if (PoolManager.Instance == null)
            {
                RenderPageWithoutPool();
                return;
            }

            if (!PoolManager.Instance.HasPool(PoolKey))
                PoolManager.Instance.Register(PoolKey, stageNodePrefab, stagesPerPage);

            RenderPage();
        }

        public void NextPage()
        {
            int maxPage = GetMaxPage();
            if (_currentPage >= maxPage) return;

            _currentPage++;
            RenderPage();
        }

        public void PreviousPage()
        {
            if (_currentPage <= 0) return;

            _currentPage--;
            RenderPage();
        }

        void RenderPage()
        {
            ReturnActiveNodes();

            int startStageId = _currentPage * stagesPerPage + 1;
            int endStageId = Mathf.Min(totalStageCount, startStageId + stagesPerPage - 1);

            for (int stageId = startStageId; stageId <= endStageId; stageId++)
            {
                var node = PoolManager.Instance.Get(PoolKey);
                ConfigureNode(node, stageId);
                _activeNodes.Add(node);
            }

            RefreshPageControls();
        }

        void RenderPageWithoutPool()
        {
            ClearInstantiatedNodes();

            int startStageId = _currentPage * stagesPerPage + 1;
            int endStageId = Mathf.Min(totalStageCount, startStageId + stagesPerPage - 1);

            for (int stageId = startStageId; stageId <= endStageId; stageId++)
            {
                var node = Instantiate(stageNodePrefab);
                ConfigureNode(node, stageId);
                _activeNodes.Add(node);
            }

            RefreshPageControls();
        }

        void ConfigureNode(GameObject node, int stageId)
        {
            node.name = $"StageNode_{stageId:00}";
            node.transform.SetParent(nodeHost, false);
            node.SetActive(true);

            var rect = node.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = GetNodePosition((stageId - 1) % stagesPerPage);
            }

            var label = node.transform.Find("StageLabel")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = stageId.ToString();

            bool hasStage = stageId <= playableStageCount;
            bool unlocked = hasStage && (DataManager.Instance == null || DataManager.Instance.IsStageUnlocked(stageId));
            bool cleared = hasStage && DataManager.Instance != null && DataManager.Instance.IsStageCleared(stageId);

            var image = node.GetComponent<Image>();
            if (image != null)
            {
                if (!hasStage || !unlocked)
                    image.color = new Color(1f, 1f, 1f, 0.16f);
                else if (stageId == GameContext.SelectedStageId)
                    image.color = new Color(1f, 0.78f, 0.32f, 1f);
                else if (cleared)
                    image.color = new Color(0.35f, 0.58f, 1f, 1f);
                else
                    image.color = new Color(0.19f, 0.91f, 0.78f, 1f);
            }

            var button = node.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = unlocked;
                button.onClick.AddListener(() =>
                {
                    if (!unlocked) return;
                    GameContext.SelectedStageId = stageId;
                    if (SceneLoader.Instance != null)
                        SceneLoader.Instance.LoadScene("Game");
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
                });
            }
        }

        static Vector2 GetNodePosition(int stageId)
        {
            int column = stageId % 5;
            int row = stageId / 5;
            float x = -320f + column * 160f;
            float y = 210f - row * 140f;
            return new Vector2(x, y);
        }

        void ReturnActiveNodes()
        {
            if (PoolManager.Instance == null || !PoolManager.Instance.HasPool(PoolKey))
            {
                ClearInstantiatedNodes();
                return;
            }

            for (int i = 0; i < _activeNodes.Count; i++)
            {
                if (_activeNodes[i] != null)
                    PoolManager.Instance.Return(PoolKey, _activeNodes[i]);
            }

            _activeNodes.Clear();
        }

        void ClearInstantiatedNodes()
        {
            for (int i = 0; i < _activeNodes.Count; i++)
            {
                if (_activeNodes[i] != null)
                    Destroy(_activeNodes[i]);
            }

            _activeNodes.Clear();
        }

        void RefreshPageControls()
        {
            int maxPage = GetMaxPage();

            if (pageLabel != null)
                pageLabel.text = $"{_currentPage + 1} / {maxPage + 1}";

            if (previousPageButton != null)
                previousPageButton.interactable = _currentPage > 0;

            if (nextPageButton != null)
                nextPageButton.interactable = _currentPage < maxPage;
        }

        int GetMaxPage()
        {
            return Mathf.Max(0, Mathf.CeilToInt(totalStageCount / (float)Mathf.Max(1, stagesPerPage)) - 1);
        }
    }
}
