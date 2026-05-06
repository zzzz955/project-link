# 01 — InGame UI

Scripts: `client/project-link/Assets/Scripts/InGame/UI/`
Scene: `Assets/Scenes/Game.unity`

---

## HUD

- [ ] Stage number label (top-center)
- [ ] Pipe connection counter: "X / Y connected" updates on each path completion
- [ ] Pause button (top-right) — opens Pause Popup

---

## Client-First Constraints

- [ ] InGame UI reads stage/progress through `GameContext` + `IProgressService`, not server classes
- [ ] Clear flow calls local/mock progress save first; HTTP sync is deferred to `06_progression_save.md`
- [ ] Loading/error UI can be tested with mock service states

---

## Clear Popup

Triggered by `GameStateMachine` on clear state entry via `PopupManager`.

- [ ] Create `ClearPopup` prefab + script under `Assets/Prefabs/`
- [ ] Display: stage number, star rating (criteria TBD: e.g. all cells filled = 3 stars)
- [ ] Buttons: Next Stage, Retry, Back to Lobby
- [ ] Entry animation: scale-in + confetti particle burst

---

## Pause Popup

- [ ] Create `PausePopup` prefab + script
- [ ] On open: freeze `InGameController` (disable input)
- [ ] Buttons: Resume, Restart, Back to Lobby
- [ ] On resume: re-enable `InGameController` input

---

## Tutorial Overlay

- [ ] Trigger condition: stage index 0–2 AND stage never cleared before
- [ ] Step 1: arrow + "Drag to connect" tooltip shown before first input
- [ ] Step 2: "Fill all cells" text shown after first pipe is completed
- [ ] Dismiss: auto-hide on correct gesture; skip button always visible
- [ ] Persistence: save `tutorialSeen` flag via client progress/settings boundary (see `06_progression_save.md`)

<!-- changed: InGame UI tasks now keep server sync behind the client service boundary -->
