using ProjectLink.Core;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class EnergyPopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button closeIconButton;
        [SerializeField] Button watchAdButton;
        [SerializeField] Button refillButton;
        [SerializeField] TextMeshProUGUI energyCounterText;
        [SerializeField] TextMeshProUGUI watchAdRewardText;
        [SerializeField] TextMeshProUGUI refillRewardText;

        bool _initialized;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindClose(closeButton);
            BindClose(closeIconButton);
            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(ClaimAdReward);
            if (refillButton != null)
                refillButton.onClick.AddListener(Refill);
            RefreshEnergy();
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("CloseButton");
            closeIconButton ??= FindButton("CloseIconButton");
            watchAdButton ??= FindButton("WatchAdButton");
            refillButton ??= FindButton("RefillButton");
            energyCounterText ??= FindText("EnergyCounterText");
            watchAdRewardText ??= FindText("WatchAdRewardText");
            refillRewardText ??= FindText("RefillRewardText");
        }

        void RefreshEnergy()
        {
            UiServiceLocator.UiData.GetStamina(result =>
            {
                if (!result.IsSuccess)
                {
                    SetText(energyCounterText, result.ErrorCode);
                    return;
                }

                var model = UiViewModelMapper.ToEnergyPopup(result.Value, UiServiceLocator.Catalog);
                SetText(energyCounterText, $"{model.Current}/{model.Max}");
                SetText(watchAdRewardText, $"+{model.AdRewardAmount}");
                SetText(refillRewardText, $"+{model.Max}");
            });
        }

        void ClaimAdReward()
        {
            SetInteractable(false);
            UiServiceLocator.UiData.ClaimStaminaAdReward("", result =>
            {
                SetInteractable(true);
                if (!result.IsSuccess)
                {
                    SetText(energyCounterText, result.ErrorCode);
                    return;
                }

                SetText(energyCounterText, $"{result.Value.Current}/{result.Value.Max}");
                SetText(watchAdRewardText, $"+{result.Value.Added}");
            });
        }

        void Refill()
        {
            SetInteractable(false);
            UiServiceLocator.UiData.RefillStamina(result =>
            {
                SetInteractable(true);
                if (!result.IsSuccess)
                {
                    SetText(energyCounterText, result.ErrorCode);
                    return;
                }

                SetText(energyCounterText, $"{result.Value.Current}/{result.Value.Max}");
                SetText(refillRewardText, $"+{result.Value.Added}");
            });
        }

        void SetInteractable(bool interactable)
        {
            if (watchAdButton != null) watchAdButton.interactable = interactable;
            if (refillButton != null) refillButton.interactable = interactable;
        }

        void BindClose(Button button)
        {
            if (button != null)
                button.onClick.AddListener(CloseTop);
        }

        Button FindButton(string buttonName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.name == buttonName)
                    return button;
            }

            return null;
        }

        TextMeshProUGUI FindText(string labelName)
        {
            foreach (var label in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (label.name == labelName)
                    return label;
            }

            return null;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }
    }
}
