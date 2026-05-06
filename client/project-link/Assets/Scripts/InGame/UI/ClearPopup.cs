using ProjectLink.Core;
using ProjectLink.OutGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

            AddLocalizedLabel(panelGo.transform, "game_stage_clear", 72, Color.white,
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

            var nextBtn  = AddLocalizedButton(panelGo.transform, "game_next",  new Vector2(0f,  -80f));
            var retryBtn = AddLocalizedButton(panelGo.transform, "game_retry", new Vector2(0f, -190f));
            var lobbyBtn = AddLocalizedButton(panelGo.transform, "game_lobby", new Vector2(0f, -300f));

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

        Button AddLocalizedButton(Transform parent, string stringId, Vector2 anchoredPos)
        {
            var go = new GameObject(stringId + "Btn");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.sizeDelta        = new Vector2(400f, 90f);
            rect.anchoredPosition = anchoredPos;
            go.AddComponent<Image>().color = new Color(0.2f, 0.5f, 0.9f, 1f);
            var btn = go.AddComponent<Button>();
            AddLocalizedLabel(go.transform, stringId, 44, Color.white, Vector2.zero, new Vector2(400f, 90f));
            return btn;
        }

        void AddLocalizedLabel(Transform parent, string stringId, float fontSize, Color color, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.sizeDelta        = size;
            rect.anchoredPosition = anchoredPos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize  = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = color;
            tmp.raycastTarget = false;
            go.AddComponent<LocalizedText>().SetStringId(stringId);
        }
    }
}
