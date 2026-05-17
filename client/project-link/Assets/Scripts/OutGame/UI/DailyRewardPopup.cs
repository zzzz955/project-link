using System.Collections.Generic;
using System.Globalization;
using ProjectLink.Core;
using ProjectLink.Data.Generated;
using ProjectLink.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class DailyRewardPopup : PopupBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button claimButton;
        [SerializeField] RectTransform dayGridRoot;
        [SerializeField] TextMeshProUGUI balanceText;

        bool _initialized;

        public void Init()
        {
            if (_initialized) return;
            _initialized = true;

            ResolveMissingReferences();
            BindOverlayClose();

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseTop);
            if (claimButton != null)
                claimButton.onClick.AddListener(CloseTop);

            RenderRewards(UiServiceLocator.Catalog?.DailyRewards);
            UiServiceLocator.UiData.GetLobbyState(result =>
            {
                if (result.IsSuccess && result.Value != null)
                    SetText(balanceText, FormatNumber(result.Value.Currency.SoftAmount));
            });
        }

        void ResolveMissingReferences()
        {
            closeButton ??= FindButton("Btn_Close");
            claimButton ??= FindButton("Btn_Claim");
            dayGridRoot ??= FindRect("Grid_Days");
            balanceText ??= FindText("Txt_Balance");
        }

        void RenderRewards(IReadOnlyList<OutgameDailyReward> rewards)
        {
            if (dayGridRoot == null || rewards == null || rewards.Count == 0)
                return;

            for (int i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                var card = FindRect($"Day_{reward.streakDay}");
                if (card == null) continue;

                var amount = FindTextIn(card, "Txt_Amount");
                if (amount != null)
                    amount.text = FormatNumber(reward.amount);

                var day = FindTextIn(card, "Txt_Day");
                if (day != null)
                {
                    var fmt = LocalizationManager.Get("popup.daily_reward.day_fmt");
                    day.text = string.Format(CultureInfo.InvariantCulture, fmt, reward.streakDay);
                }
            }
        }

        Button FindButton(string childName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == childName) return button;
            return null;
        }

        RectTransform FindRect(string childName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
                if (rect.name == childName) return rect;
            return null;
        }

        TextMeshProUGUI FindText(string childName)
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == childName) return text;
            return null;
        }

        static TextMeshProUGUI FindTextIn(RectTransform parent, string childName)
        {
            foreach (var text in parent.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == childName) return text;
            return null;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }

        static string FormatNumber(long value)
            => value.ToString("N0", CultureInfo.InvariantCulture);

        static void CloseTop()
        {
            PopupManager.Instance?.CloseTop();
        }
    }
}
