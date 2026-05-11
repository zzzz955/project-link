# 00 - Client-First Server Boundary

Goal: finish client core/UI flow with mock/local services first, then replace adapters with real server calls without rewriting scene logic.

---

## Phase 1 - Client Contract Boundary

- [x] Define `IAuthService` in `client/project-link/Assets/Scripts/Core/`
  - `EnsureAuth()` (covers guest login)
  - `Refresh()`
  - `GetToken()` / `SetToken()` / `ClearToken()`
- [ ] Define `IProgressService`
  - `GetAllProgress()`
  - `ClearStage(stageId, stars)`
  - `SyncOnLoginAsync()`
  - `PushPendingAsync()`
- [ ] Define `INetworkClient`
  - `GetAsync<T>()`
  - `PostAsync<TRequest,TResponse>()`
  - 401 refresh/retry hook
- [x] Add service mode switch
  - `MockAuthScenario` (Success / Failure / SessionExpired) via `MockAuthService.Scenario`
  - `Http` mode: replace `MockAuthService` with `HttpAuthService` at `NetworkManager.AuthService` (P4/P5)

---

## Phase 2 - Mock Failure States

- [x] Mock auth success/failure/session-expired scenarios (`MockAuthScenario` enum)
- [ ] Mock progress fetch delay/error/offline queue scenarios
- [ ] UI loading state for login, lobby progress load, stage clear save
- [ ] UI error state for auth failed, sync failed, retry available
- [x] Session expired popup flow: `HttpUiDataService` → `PopupManager.Request(SessionExpired)` → `SessionExpiredPopup` → return to Title

---

## Phase 3 - Client Complete Before Server Wiring

- [ ] Title -> Lobby -> Game -> Clear -> Next Stage loop works with mock/local services
- [ ] Lobby locked/unlocked/star display reads only through `IProgressService`
- [ ] Tutorial seen flag reads only through progress/settings service boundary
- [ ] Settings popup persists locally and is independent of server
- [ ] Scene transitions do not directly call HTTP/server classes

---

## Phase 4 - Server Attachment Gate

Server integration starts only after all items below are true.

- [ ] `IAuthService` and `IProgressService` are used by UI/scene code
- [ ] Mock/local implementation covers normal, loading, failure, and expired-session states
- [ ] Packet DTOs for auth/progress are generated or manually mirrored from `shared/packets/`
- [ ] Server endpoints in `10_server.md` match the client service contracts
- [ ] Edge/auth infra can be added without changing scene flow

<!-- changed: client-first flow added so server wiring waits behind service boundaries -->
