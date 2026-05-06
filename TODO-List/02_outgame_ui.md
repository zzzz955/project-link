# 02 — OutGame UI

Scripts: `client/project-link/Assets/Scripts/OutGame/`
Scenes: `Assets/Scenes/Title.unity`, `Assets/Scenes/Lobby.unity`

---

## Title Scene UI

- [ ] Game logo / title text (center)
- [ ] "Start" button → calls `SceneLoader` to navigate to Lobby
- [ ] Settings button (top-right) → opens Settings Popup
- [ ] Version label (bottom-right, read from `Application.version`)

---

## Client-First Constraints

- [ ] Title auth entry uses `IAuthService` mock/local mode before real auth server
- [ ] Lobby reads unlock/star state through `IProgressService`
- [ ] Lobby loading/error states are testable without a running server
- [ ] Scene navigation remains available when auth/progress server is offline

---

## Lobby Scene UI

- [ ] Scrollable stage grid
  - [ ] Each cell: stage number, star count (0–3 filled stars), locked/unlocked state
  - [ ] Unlock condition: clear previous stage (read from `SaveService`)
  - [ ] Locked cells: greyed out, non-interactive
  - [ ] Tap unlocked cell → `SceneLoader` loads Game scene with selected stage ID
- [ ] Chapter/World section header (group stages per world if multiple worlds exist)
- [ ] Back button → return to Title

---

## Settings Popup

Accessible from both Title and Lobby via `PopupManager`.

- [ ] Create `SettingsPopup` prefab + script
- [ ] Sound (BGM) toggle → calls `SoundManager.SetBGMEnabled(bool)`
- [ ] Haptic toggle → calls `HapticManager.SetEnabled(bool)`
- [ ] Close button
- [ ] Persist both settings to `PlayerPrefs` on change; load on Bootstrap

<!-- changed: OutGame UI tasks now prioritize mock/local services so scene flow stays free before server wiring -->
