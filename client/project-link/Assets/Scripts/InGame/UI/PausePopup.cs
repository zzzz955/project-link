using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ProjectLink.Core;

namespace ProjectLink.InGame.UI
{
    public class PausePopup : PopupBase
    {
        Action _onResume;

        public void Init(Action onResume)
        {
            _onResume = onResume;

            var overlay = gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.7f);

            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(transform, false);
            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin        = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax        = new Vector2(0.5f, 0.5f);
            panelRect.pivot            = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta        = new Vector2(600f, 580f);
            panelRect.anchoredPosition = Vector2.zero;
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

            AddLabel(panelGo.transform, "PAUSED", 72, Color.white,
                new Vector2(0f, 190f), new Vector2(560f, 100f));

            var resumeBtn = AddButton(panelGo.transform, "RESUME", new Vector2(0f,  50f));
            var retryBtn  = AddButton(panelGo.transform, "RETRY",  new Vector2(0f, -60f));
            var lobbyBtn  = AddButton(panelGo.transform, "LOBBY",  new Vector2(0f, -170f));

            resumeBtn.onClick.AddListener(DoResume);

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

        public override void OnBackPressed() => DoResume();

        void DoResume()
        {
            PopupManager.Instance.CloseTop();
            _onResume?.Invoke();
        }

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
