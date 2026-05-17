using ProjectLink.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLink.InGame.UI
{
    public sealed class GameWireframeController : MonoBehaviour
    {
        [SerializeField] Button backButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button hintButton;
        [SerializeField] Button undoButton;
        [SerializeField] Button paintItemButton;
        [SerializeField] Button hammerButton;
        [SerializeField] Button brushButton;
        [SerializeField] TextMeshProUGUI levelLabelText;
        [SerializeField] TextMeshProUGUI moveCounterText;
        [SerializeField] TextMeshProUGUI timerText;
        [SerializeField] TextMeshProUGUI objectiveText;

        public TextMeshProUGUI MoveCounterText => moveCounterText;
        public TextMeshProUGUI TimerText => timerText;
        public TextMeshProUGUI ObjectiveText => objectiveText;

        void Awake()
        {
            // Resolve refs by name if not set in inspector
            timerText ??= FindText("Txt_Timer");
            objectiveText ??= FindText("Txt_Objective");
            if (moveCounterText == null)
                moveCounterText = FindText("Txt_Moves");
        }

        void Start()
        {
            SetText(levelLabelText, string.Format(LocalizationManager.Get("popup.stage.title_n_fmt"), GameContext.SelectedStageId));
        }

        public void SetStageLabel(int stageId)
        {
            SetText(levelLabelText, string.Format(LocalizationManager.Get("popup.stage.title_n_fmt"), stageId));
        }

        public void SetToolButtonsInteractable(bool interactable)
        {
            if (backButton != null) backButton.interactable = interactable;
            if (settingsButton != null) settingsButton.interactable = interactable;
            if (hintButton != null) hintButton.interactable = interactable;
            if (undoButton != null) undoButton.interactable = interactable;
            if (paintItemButton != null) paintItemButton.interactable = interactable;
            if (hammerButton != null) hammerButton.interactable = interactable;
            if (brushButton != null) brushButton.interactable = interactable;
        }

        TextMeshProUGUI FindText(string childName)
        {
            foreach (var t in GetComponentsInChildren<TextMeshProUGUI>(true))
                if (t.name == childName) return t;
            return null;
        }

        static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value ?? "";
        }
    }
}
