using ProjectLink.Core;
using ProjectLink.Contracts.Reward;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class RewardPopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button watchAdButton;
        [SerializeField] Button claimButton;
        [SerializeField] TextMeshProUGUI rewardAmountText;

        bool _initialized;
        string _rewardSource = "manual";
        string _rewardToken = "";

        public void Init(string rewardSource = "manual", string rewardToken = "")
        {
            if (_initialized) return;
            _initialized = true;
            _rewardSource = rewardSource;
            _rewardToken = rewardToken;

            ResolveMissingReferences();
            BindClose(closeButton);
            if (claimButton != null)
                claimButton.onClick.AddListener(() => Claim(1));
            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(() => Claim(3));
        }

        void Claim(int multiplier)
        {
            UiServiceLocator.UiData.ClaimReward(_rewardSource, _rewardToken, multiplier, Apply);
        }

        void Apply(ServiceResult<RewardClaimResponse> result)
        {
            if (!result.IsSuccess)
            {
                SetText(rewardAmountText, result.ErrorCode);
                return;
            }

            if (result.Value.RewardsGranted.Count > 0)
            {
                var reward = result.Value.RewardsGranted[0];
                SetText(rewardAmountText, $"+{reward.Amount}");
            }

            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("CloseButton");
            watchAdButton ??= FindButton("WatchAdButton");
            claimButton ??= FindButton("ClaimButton");
            rewardAmountText ??= FindText("RewardAmountText");
        }

        Button FindButton(string name)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == name) return button;
            return null;
        }

        TextMeshProUGUI FindText(string name)
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == name) return text;
            return null;
        }

        static void BindClose(Button button)
        {
            if (button != null)
                button.onClick.AddListener(CloseTop);
        }

        static void CloseTop()
        {
            if (PopupManager.Instance != null)
                PopupManager.Instance.CloseTop();
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }
    }
}
