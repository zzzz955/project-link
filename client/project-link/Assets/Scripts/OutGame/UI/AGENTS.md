# OutGame UI

## Role
Runtime helpers for Title, Lobby, and other non-gameplay UI surfaces.

## Rules
- Scene navigation must go through `ProjectLink.Core.SceneLoader` when available.
- Shared state must go through `ProjectLink.Core.GameContext`.
- Use `ProjectLink.Core.PoolManager` for repeated runtime UI items such as lobby stage nodes.
- Popups here are allowed for outgame navigation confirmations and scene escape handling.
- Namespace mirrors folder path: `ProjectLink.OutGame.UI`.
