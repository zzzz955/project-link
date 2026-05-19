using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.OutGame.UI
{
    public sealed class RankingCard : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI rankText;
        [SerializeField] Image rankImage;
        [SerializeField] TextMeshProUGUI displayNameText;
        [SerializeField] TextMeshProUGUI levelLabelText;
        [SerializeField] TextMeshProUGUI levelValueText;
        [SerializeField] Image backgroundImage;

        static readonly Color NormalBackground = new(0.98f, 0.91f, 0.82f, 0.96f);
        static readonly Color PinnedBackground = new(0.46f, 0.88f, 0.25f, 0.96f);

        public void Init(int rank, string displayName, long level, Sprite rankSprite, Sprite avatarSprite, bool pinned)
        {
            ResolveRefs();

            if (backgroundImage != null)
                backgroundImage.color = pinned ? PinnedBackground : NormalBackground;

            string rankValue = FormatRank(rank);
            if (rankImage != null)
            {
                bool useRankSprite = rankSprite != null;
                rankImage.gameObject.SetActive(useRankSprite);
                if (useRankSprite)
                {
                    rankImage.sprite = rankSprite;
                    rankImage.color = Color.white;
                    rankImage.preserveAspect = true;
                }
            }

            if (rankText != null)
            {
                rankText.gameObject.SetActive(rankSprite == null);
                rankText.text = rankValue;
            }

            if (displayNameText != null)
                displayNameText.text = string.IsNullOrEmpty(displayName)
                    ? LocalizationManager.Get("popup.account.guest")
                    : displayName;
            if (levelLabelText != null)
                levelLabelText.text = LocalizationManager.Get("rank.level");
            if (levelValueText != null)
                levelValueText.text = level.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        }

        static string FormatRank(int rank)
        {
            if (rank <= 0) return "--";
            return rank >= 1000 ? "1000+" : rank.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        void ResolveRefs()
        {
            backgroundImage ??= GetComponent<Image>();
            rankText        ??= FindTmp("Txt_Rank");
            rankImage       ??= FindImage("Img_Rank");
            displayNameText ??= FindTmp("Txt_DisplayName");
            levelLabelText  ??= FindTmp("Txt_LevelLabel");
            levelValueText  ??= FindTmp("Txt_LevelValue");
        }

        TextMeshProUGUI FindTmp(string childName)
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == childName) return text;
            return null;
        }

        Image FindImage(string childName)
        {
            foreach (var image in GetComponentsInChildren<Image>(true))
                if (image.name == childName) return image;
            return null;
        }
    }
}
