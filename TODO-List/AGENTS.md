# TODO-List

Navigation and progress tracker for all remaining work before release.

## Nav

| File | Area | Status |
|------|------|--------|
| [01_ingame_ui.md](01_ingame_ui.md) | InGame UI | TODO |
| [02_outgame_ui.md](02_outgame_ui.md) | OutGame UI | TODO |
| [03_scene_transition.md](03_scene_transition.md) | Scene Transition | TODO |
| [04_ad_system.md](04_ad_system.md) | Ad System (AdMob) | TODO |
| [05_auth_system.md](05_auth_system.md) | Auth System | TODO |
| [06_progression_save.md](06_progression_save.md) | Progression Save | TODO |
| [07_stage_editor.md](07_stage_editor.md) | Stage Editor (Editor Tool) | TODO |
| [08_sound_vfx.md](08_sound_vfx.md) | Sound & VFX Polish | TODO |
| [09_platform_build.md](09_platform_build.md) | Platform Build (Android) | TODO |
| [10_server.md](10_server.md) | Server (ASP.NET) | TODO |

## Progress Summary

All areas pending. No work started.

Suggested execution order:
1. `01` → `02` → `03` — playable end-to-end loop
2. `06` (local layer) — persistent progress
3. `07` — unblock content creation
4. `10` → `05` → `06` (server layer) — backend
5. `04` — monetization
6. `08` — polish
7. `09` — release build

## Convention

- **Task done**: change `[ ]` to `[x]` in the relevant TODO file
- **Area status**: update the Status column above — `TODO` → `IN_PROGRESS` → `DONE`
- **Scope change**: add/remove items in the relevant file; append a comment `<!-- changed: <reason> -->` at the bottom of that file
- This file is the single source of truth for release progress — keep it in sync after every work session
