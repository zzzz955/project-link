using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectLink.Core;

namespace ProjectLink.InGame.UI
{
    public class ClearPopup : PopupBase
    {
        public void Init(int stageId, int stars)
        {
            var overlay = gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.7f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRect.pivot            = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta        = new Vector2(600f, 700f);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            AddLabel(panelGo.transform, "STAGE CLEAR", 72, Color.white,
                new Vector2(0f, 220f), new Vector2(560f, 100f));

            float starSpacing = 120f;
            float startX = -(stars - 1) * starSpacing * 0.5f;
            for (int i = 0; i < stars; i++)
            {
                var starGo = new GameObject($"Star_{i}");
                starGo.transform.SetParent(panelGo.transform, false);
                var starRect = starGo.AddComponent<RectTransform>();
                starRect.anchorMin        = new Vector2(0.5f, 0.5f);
                starRect.anchorMax        = new Vector2(0.5f, 0.5f);
                starRect.pivot            = new Vector2(0.5f, 0.5f);
                starRect.sizeDelta        = new Vector2(80f, 80f);
                starRect.anchoredPosition = new Vector2(startX + i * starSpacing, 100f);
                var starImg = starGo.AddComponent<Image>();
                starImg.color = new Color(1f, 0.85f, 0f, 1f);
            }

            var nextBtn  = AddButton(panelGo.transform, "NEXT",  new Vector2(0f,  -80f));
            var retryBtn = AddButton(panelGo.transform, "RETRY", new Vector2(0f, -190f));
            var lobbyBtn = AddButton(panelGo.transform, "LOBBY", new Vector2(0f, -300f));

            nextBtn.onClick.AddListener(() =>
            {
                GameContext.SelectedStageId++;
                PopupManager.Instance.CloseAll();
                SceneLoader.Instance.LoadScene("Game");
            });

            retryBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.CloseAll();
                SceneLoader.Instance.LoadScene("Game");
            });

            lobbyBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.CloseAll();
                SceneLoader.Instance.LoadScene("Lobby");
            });
        }

        public override void OnBackPressed() { }

        Button AddButton(Transform parent, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(label + "Btn");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.sizeDelta        = new Vector2(400f, 90f);
            rect.anchoredPosition = anchoredPos;
            go.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);
            var btn = go.AddComponent<Button>();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 44;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;

            return btn;
        }

        void AddLabel(Transform parent, string text, float fontSize, Color color, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("TitleLabel");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.sizeDelta        = size;
            rect.anchoredPosition = anchoredPos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = color;
        }
    }
}
