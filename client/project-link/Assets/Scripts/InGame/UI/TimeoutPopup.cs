using ProjectLink.Core;
using ProjectLink.Data.Generated;
using ProjectLink.Services;
using ProjectLink.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.InGame.UI
{
    public class TimeoutPopup : PopupBase
    {
        [SerializeField] Button btnRetry;
        [SerializeField] Button btnLobby;
        [SerializeField] Button btnExtend;
        [SerializeField] TextMeshProUGUI txtExtendDesc;
        [SerializeField] TextMeshProUGUI txtExtendCost;

        int _stageId;
        int _extensionCount;
        IUiDataService _uiData;

        public void Init(int stageId)
        {
            _stageId = stageId;
            _extensionCount = 0;
            _uiData = UiServiceLocator.UiData;

            btnRetry  ??= FindButtonInChildren("Btn_Retry");
            btnLobby  ??= FindButtonInChildren("Btn_Lobby");
            btnExtend ??= FindButtonInChildren("Btn_Extend");
            txtExtendDesc ??= FindTextInChildren("Txt_ExtendDesc");
            txtExtendCost ??= FindTextInChildren("Txt_ExtendCost");

            BindOverlayClose();
            RefreshExtendButton();

            if (btnRetry != null)
                btnRetry.onClick.AddListener(() =>
                {
                    PopupManager.Instance.CloseAll();
                    if (InGameController.Instance != null)
                        InGameController.Instance.AbandonStageAndLoad("Game");
                    else
                        SceneLoader.Instance.LoadScene("Game");
                });

            if (btnLobby != null)
                btnLobby.onClick.AddListener(() =>
                {
                    PopupManager.Instance.CloseAll();
                    if (InGameController.Instance != null)
                        InGameController.Instance.AbandonStageAndLoad("Lobby");
                    else
                        SceneLoader.Instance.LoadScene("Lobby");
                });

            if (btnExtend != null)
                btnExtend.onClick.AddListener(OnExtendClicked);
        }

        public override void OnBackPressed() { }

        void RefreshExtendButton()
        {
            if (btnExtend == null) return;
            var nextCount = _extensionCount + 1;
            var config    = GetExtendConfig(nextCount);
            btnExtend.gameObject.SetActive(config != null);
            if (config == null) return;

            if (txtExtendDesc != null)
                txtExtendDesc.text = LocalizationManager.Get("popup.timeout.extend_desc");
            if (txtExtendCost != null)
                txtExtendCost.text = config.costSoft.ToString();
        }

        void OnExtendClicked()
        {
            if (btnExtend != null) btnExtend.interactable = false;
            _uiData.ExtendStageTime(_stageId, result =>
            {
                if (!result.IsSuccess)
                {
                    if (btnExtend != null) btnExtend.interactable = true;
                    UiEventBus.Publish(new UiErrorRaised("extend_time", result.ErrorCode, result.ErrorMessage));
                    return;
                }

                _extensionCount++;
                if (InGameController.Instance != null)
                    InGameController.Instance.ExtendTime(result.Value.ExtendedSeconds);
            });
        }

        static OutgameTimeExtendConfig GetExtendConfig(int extensionCount)
        {
            var rows = CsvLoader.Load<OutgameTimeExtendConfig>(OutgameTimeExtendConfig.ResourcePath);
            if (rows == null) return null;
            foreach (var row in rows)
                if (row.extensionCount == extensionCount) return row;
            return null;
        }

        Button FindButtonInChildren(string childName)
        {
            foreach (var btn in GetComponentsInChildren<Button>(true))
                if (btn.name == childName) return btn;
            return null;
        }

        TextMeshProUGUI FindTextInChildren(string childName)
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == childName) return t;
            return null;
        }
    }
}
