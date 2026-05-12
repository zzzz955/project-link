using System;
using ProjectLink.Core;
using ProjectLink.OutGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.InGame.UI
{
    public class PausePopup : PopupBase
    {
        Action _onResume;

        public void Init() => Init(null);

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

            AddLocalizedLabel(panelGo.transform, "game_paused", 72, Color.white,
                new Vector2(0f, 190f), new Vector2(560f, 100f));

            var resumeBtn = AddLocalizedButton(panelGo.transform, "game_resume", new Vector2(0f,  50f));
            var retryBtn  = AddLocalizedButton(panelGo.transform, "game_retry",  new Vector2(0f, -60f));
            var lobbyBtn  = AddLocalizedButton(panelGo.transform, "game_lobby",  new Vector2(0f, -170f));

            resumeBtn.onClick.AddListener(DoResume);

            retryBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.CloseAll();
                if (InGameController.Instance != null)
                    InGameController.Instance.AbandonStageAndLoad("Game");
                else
                    SceneLoader.Instance.LoadScene("Game");
            });

            lobbyBtn.onClick.AddListener(() =>
            {
                PopupManager.Instance.CloseAll();
                if (InGameController.Instance != null)
                    InGameController.Instance.AbandonStageAndLoad("Lobby");
                else
                    SceneLoader.Instance.LoadScene("Lobby");
            });
        }

        public override void OnBackPressed() => DoResume();

        void DoResume()
        {
            PopupManager.Instance.CloseTop();
            _onResume?.Invoke();
        }

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
