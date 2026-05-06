# 03 — Scene Transition

Existing hook: `SceneLoader.cs` in `Core/`
All scene loads must go through `SceneLoader` — never call `SceneManager.LoadScene` directly.

---

## Fade Transition

- [ ] Create `TransitionOverlay` prefab: full-screen black `Image`, `DontDestroyOnLoad`
- [ ] Add `FadeOut(float duration)` and `FadeIn(float duration)` to `SceneLoader`
- [ ] Sequence: FadeOut → load scene async → FadeIn
- [ ] Default duration: 0.3s (adjust per feel)

---

## Loading Screen

- [ ] Show loading panel when `AsyncOperation.progress < 1f`
- [ ] Progress bar bound to `AsyncOperation.progress`
- [ ] Minimum display time: 0.5s to avoid flicker on fast loads
- [ ] Hide on `allowSceneActivation = true`

---

## Scene Flow

Allowed transitions — `SceneLoader` enforces these:

| From | To | Trigger |
|------|----|---------|
| Bootstrap | Title | Init sequence complete |
| Title | Lobby | "Start" button |
| Lobby | Game | Stage cell tap (pass `stageId`) |
| Game | Lobby | "Back to Lobby" in Pause or Clear popup |
| Game | Game | "Retry" in Pause or Clear popup (reload same `stageId`) |
