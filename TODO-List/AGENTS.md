# TODO-List

Navigation and progress tracker for all remaining work before release.

## Nav

| File | Area | Status |
|------|------|--------|
| [00_client_first_server_boundary.md](00_client_first_server_boundary.md) | Client/Server Boundary | IN_PROGRESS |
| [01_ingame_ui.md](01_ingame_ui.md) | InGame UI | IN_PROGRESS |
| [02_outgame_ui.md](02_outgame_ui.md) | OutGame UI | TODO |
| [03_scene_transition.md](03_scene_transition.md) | Scene Transition | DONE |
| [04_ad_system.md](04_ad_system.md) | Ad System (AdMob) | TODO |
| [05_auth_system.md](05_auth_system.md) | Auth System | TODO |
| [06_progression_save.md](06_progression_save.md) | Progression Save | IN_PROGRESS |
| [07_stage_editor.md](07_stage_editor.md) | Stage Editor (Web Admin Tool) | TODO |
| [08_sound_vfx.md](08_sound_vfx.md) | Sound & VFX Polish | TODO |
| [09_platform_build.md](09_platform_build.md) | Platform Build (Android) | TODO |
| [10_server.md](10_server.md) | Server (ASP.NET) | IN_PROGRESS |
| [11_packet_protocol.md](11_packet_protocol.md) | Packet Protocol (gen-packets rewrite) | TODO |

## Progress Summary

Client core loop playable (scene transitions, InGame HUD/popups, GameContext).
Client-first boundary gates server wiring: UI/scene code should depend on mock/local service interfaces before HTTP adapters.
Server scaffold complete (4-layer structure, middleware stack, Docker Compose), but gameplay integration waits until client contracts stabilize.
Stage Editor: Unity EditorWindow 방식 폐기, 웹 Admin API 기반 자동 생성으로 전환 예정.
Remaining: Client service boundaries, OutGame UI, Auth/Ad systems, Stage Editor (Web), Sound/VFX, Platform build, server sync wiring.

Suggested execution order:
1. `00` - service boundaries and mock/local adapters
2. `01` -> `02` -> `03` - playable end-to-end client loop
3. `06` (local layer) - persistent progress behind `IProgressService`
4. `07` - unblock content creation
5. `11` - packet/DTO contracts for auth/progress
6. `05` - auth client mock first, then shared auth server
7. `10` - game server wiring after client contracts stabilize
8. `04` - monetization
9. `08` - polish
10. `09` - release build

## Convention

- **Task done**: change `[ ]` to `[x]` in the relevant TODO file
- **Area status**: update the Status column above: `TODO`, `IN_PROGRESS`, `DONE`
- **Scope change**: add/remove items in the relevant file; append a comment `<!-- changed: <reason> -->` at the bottom of that file
- This file is the single source of truth for release progress; keep it in sync after every work session

<!-- changed: execution order updated to finish client core/UI behind mockable service contracts before server attachment -->
