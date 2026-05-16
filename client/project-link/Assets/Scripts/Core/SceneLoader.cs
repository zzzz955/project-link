using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace ProjectLink.Core
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public bool IsLoading { get; private set; }

        bool _readyToFadeIn = true;

        public void HoldForReady() => _readyToFadeIn = false;
        public void NotifyReady() => _readyToFadeIn = true;

        private Image _overlay;
        private TextMeshProUGUI _loadingText;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateTransitionOverlay();
        }

        private void CreateTransitionOverlay()
        {
            var canvasGo = new GameObject("TransitionOverlay");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var imageGo = new GameObject("FadeImage");
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rt = imageGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _overlay = imageGo.AddComponent<Image>();
            _overlay.color = new Color(0, 0, 0, 0);
            _overlay.raycastTarget = false;

            var textGo = new GameObject("LoadingText");
            textGo.transform.SetParent(canvasGo.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.5f, 0.5f);
            textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = new Vector2(200, 60);
            textRt.anchoredPosition = Vector2.zero;
            _loadingText = textGo.AddComponent<TextMeshProUGUI>();
            _loadingText.text = "...";
            _loadingText.fontSize = 48;
            _loadingText.alignment = TextAlignmentOptions.Center;
            _loadingText.color = Color.white;
            _loadingText.gameObject.SetActive(false);
        }

        private IEnumerator FadeOut(float duration)
        {
            float elapsed = 0f;
            _overlay.raycastTarget = true;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _overlay.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            _overlay.color = new Color(0, 0, 0, 1);
        }

        private IEnumerator FadeIn(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _overlay.color = new Color(0, 0, 0, Mathf.Clamp01(1f - elapsed / duration));
                yield return null;
            }
            _overlay.color = new Color(0, 0, 0, 0);
            _overlay.raycastTarget = false;
        }

        public void LoadScene(string sceneName, Action onComplete = null)
        {
            if (IsLoading) return;
            StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
        }

        public void LoadScene(int buildIndex, Action onComplete = null)
        {
            if (IsLoading) return;
            StartCoroutine(LoadSceneRoutine(buildIndex, onComplete));
        }

        private IEnumerator LoadSceneRoutine(string sceneName, Action onComplete)
        {
            _readyToFadeIn = true;
            IsLoading = true;
            yield return FadeOut(0.3f);
            _loadingText.gameObject.SetActive(true);
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;
            float elapsed = 0f;
            while (!(op.progress >= 0.9f && elapsed >= 0.5f))
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            _loadingText.gameObject.SetActive(false);
            op.allowSceneActivation = true;
            yield return new WaitUntil(() => op.isDone);
            yield return new WaitUntil(() => _readyToFadeIn);
            yield return FadeIn(0.3f);
            IsLoading = false;
            onComplete?.Invoke();
        }

        private IEnumerator LoadSceneRoutine(int buildIndex, Action onComplete)
        {
            _readyToFadeIn = true;
            IsLoading = true;
            yield return FadeOut(0.3f);
            _loadingText.gameObject.SetActive(true);
            var op = SceneManager.LoadSceneAsync(buildIndex);
            op.allowSceneActivation = false;
            float elapsed = 0f;
            while (!(op.progress >= 0.9f && elapsed >= 0.5f))
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            _loadingText.gameObject.SetActive(false);
            op.allowSceneActivation = true;
            yield return new WaitUntil(() => op.isDone);
            yield return new WaitUntil(() => _readyToFadeIn);
            yield return FadeIn(0.3f);
            IsLoading = false;
            onComplete?.Invoke();
        }
    }
}
