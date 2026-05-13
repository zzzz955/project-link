using System;
using ProjectLink.Core;
using ProjectLink.OutGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.InGame.UI
{
    public sealed class ClearNextStageConfirmModel
    {
        public ClearNextStageConfirmModel(int stageId, Action onConfirm, Action onCancel)
        {
            StageId = stageId;
            OnConfirm = onConfirm;
            OnCancel = onCancel;
        }

        public int StageId { get; }
        public Action OnConfirm { get; }
        public Action OnCancel { get; }
    }

    public sealed class ClearNextStageConfirmPopup : PopupBase
    {
        [SerializeField] Button closeIconButton;
        [SerializeField] Button cancelButton;
        [SerializeField] Button confirmButton;
        [SerializeField] TextMeshProUGUI bodyText;

        bool _initialized;
        ClearNextStageConfirmModel _model;

        public void Init(ClearNextStageConfirmModel model)
        {
            if (_initialized) return;
            _initialized = true;
            _model = model;

            ResolveMissingReferences();
            if (cancelButton == null || confirmButton == null)
                BuildFallback();

            if (bodyText != null && _model != null)
                bodyText.text = $"Stage {_model.StageId} is already cleared.";

            BindOverlayCancel();
            BindCancel(closeIconButton);
            BindCancel(cancelButton);
            BindConfirm(confirmButton);
        }

        public override void OnBackPressed()
        {
            Cancel();
        }

        void BindOverlayCancel()
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
            {
                if (button.name != "Overlay") continue;
                button.onClick.AddListener(Cancel);
                return;
            }
        }

        void BindCancel(Button button)
        {
            if (button != null)
                button.onClick.AddListener(Cancel);
        }

        void BindConfirm(Button button)
        {
            if (button != null)
                button.onClick.AddListener(Confirm);
        }

        void Confirm()
        {
            _model?.OnConfirm?.Invoke();
        }

        void Cancel()
        {
            _model?.OnCancel?.Invoke();
        }

        void ResolveMissingReferences()
        {
            closeIconButton ??= FindButton("Btn_Close");
            cancelButton ??= FindButton("Btn_Cancel");
            confirmButton ??= FindButton("Btn_Confirm");
            bodyText ??= FindText("Txt_Body");
        }

        void BuildFallback()
        {
            gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(720f, 420f);
            panelRect.anchoredPosition = Vector2.zero;
            panelGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 1f);

            AddLocalizedLabel(panelGo.transform, "popup.clear.title", 44, new Vector2(0f, 130f), new Vector2(640f, 70f));
            bodyText = AddLabel(panelGo.transform, "Txt_Body", 30, new Vector2(0f, 40f), new Vector2(620f, 80f));
            cancelButton = AddLocalizedButton(panelGo.transform, "Btn_Cancel", "common.cancel", new Vector2(-170f, -120f), false);
            confirmButton = AddLocalizedButton(panelGo.transform, "Btn_Confirm", "common.confirm", new Vector2(170f, -120f), true);
        }

        static Button AddLocalizedButton(Transform parent, string name, string stringId, Vector2 anchoredPos, bool primary)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280f, 82f);
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = primary
                ? new Color(0.2f, 0.5f, 0.9f, 1f)
                : new Color(1f, 1f, 1f, 0.14f);
            AddLocalizedLabel(go.transform, stringId, 32, Vector2.zero, new Vector2(260f, 76f));
            return go.GetComponent<Button>();
        }

        static TextMeshProUGUI AddLabel(Transform parent, string name, float fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        static void AddLocalizedLabel(Transform parent, string stringId, float fontSize, Vector2 anchoredPos, Vector2 size)
        {
            var label = AddLabel(parent, "Label", fontSize, anchoredPos, size);
            label.gameObject.AddComponent<LocalizedText>().SetStringId(stringId);
        }

        Button FindButton(string childName)
        {
            foreach (var button in GetComponentsInChildren<Button>(true))
                if (button.name == childName) return button;
            return null;
        }

        TextMeshProUGUI FindText(string childName)
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (text.name == childName) return text;
            return null;
        }
    }
}
