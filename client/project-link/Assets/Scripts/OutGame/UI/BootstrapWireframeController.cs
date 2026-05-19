using System.Collections;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectLink.Core;

namespace ProjectLink.OutGame.UI
{
    public sealed class BootstrapWireframeController : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI loadingLabelText;
        [SerializeField] TextMeshProUGUI versionText;
        [SerializeField] Image progressFillImage;
        [SerializeField] Button retryButton;
        [SerializeField] TextMeshProUGUI networkErrorText;

        BootstrapViewModel _viewModel;
        bool _forceUpdateShown;
        bool _titleLoadRequested;
        Coroutine _progressCoroutine;

        void Start()
        {
            progressFillImage ??= FindImage("Fill");
            retryButton ??= FindButton("Btn_Retry");
            networkErrorText ??= FindText("Txt_NetworkError");
            loadingLabelText ??= FindText("Txt_Loading");
            versionText ??= FindText("Txt_Version");

            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(Retry);
            }

            _viewModel = new BootstrapViewModel(UiServiceLocator.UiData);
            _viewModel.Changed += Render;
            _viewModel.Load();
        }

        void OnDestroy()
        {
            if (_viewModel != null)
                _viewModel.Changed -= Render;
        }

        void Retry()
        {
            _viewModel?.Load();
        }

        void Render()
        {
            if (_viewModel == null) return;

            SetText(loadingLabelText, LocalizationManager.Get(_viewModel.StatusStringId));
            SetText(versionText, _viewModel.Version);
            SetText(networkErrorText, LocalizationManager.GetError(_viewModel.ErrorCode));
            if (networkErrorText != null)
                networkErrorText.gameObject.SetActive(!string.IsNullOrEmpty(_viewModel.ErrorCode));
            if (retryButton != null)
                retryButton.gameObject.SetActive(_viewModel.RetryVisible);

            AnimateProgress(_viewModel.Progress);

            if (_viewModel.RequiresForceUpdate)
            {
                if (!_forceUpdateShown)
                {
                    _forceUpdateShown = true;
                    PopupManager.Request(PopupId.ForceUpdate);
                }
                return;
            }

            if (_viewModel.ReadyToEnterTitle && SceneLoader.Instance != null && !_titleLoadRequested)
            {
                _titleLoadRequested = true;
                SceneLoader.Instance.LoadScene("Title");
            }
        }

        void AnimateProgress(float target)
        {
            if (progressFillImage == null) return;
            if (_progressCoroutine != null) StopCoroutine(_progressCoroutine);
            _progressCoroutine = StartCoroutine(ProgressRoutine(target));
        }

        IEnumerator ProgressRoutine(float target)
        {
            float start = progressFillImage.fillAmount;
            float elapsed = 0f;
            const float duration = 0.45f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                progressFillImage.fillAmount = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            progressFillImage.fillAmount = target;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        Image FindImage(string childName)
        {
            foreach (var img in GetComponentsInChildren<Image>(true))
                if (img.name == childName) return img;
            return null;
        }

        Button FindButton(string childName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == childName) return button;
            return null;
        }

        TextMeshProUGUI FindText(string childName)
        {
            foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (label.name == childName) return label;
            return null;
        }
    }
}
