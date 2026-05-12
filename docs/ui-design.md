# Project Link — UI/UX Design Specification

> Genre: Hyper-casual puzzle (path-connection grid)  
> Platform: Mobile (iOS / Android), Unity 6 URP 2D  
> Last updated: 2026-05-12

---

## 1. Design System

### Visual Language
| Attribute | Direction |
|-----------|-----------|
| Aesthetic | Clean, vibrant, minimal — bold accent colors on dark/neutral backgrounds |
| Typography | Rounded bold for headings; medium weight for body text |
| Iconography | Filled icons, consistent visual weight |
| Animation driver | Spine 2D for character/reward/celebration sequences; Unity Animator for scene/popup transitions; DOTween for micro-interactions |
| Asset skinning | Static image assets applied via `UIButtonSkin` ScriptableObject (elementName → Sprite mapping). All buttons and image slots participate in skin swaps |

### Global UX Rules

| Rule | Specification |
|------|---------------|
| **Toast (error/info)** | Top of screen. Stack, max 3 simultaneous. New toast appears at bottom of stack; existing toasts shift upward. Auto-dismiss after 3 s. Non-critical API/network errors only; auth errors use blocking popup |
| **API loading state** | Button `interactable=false` + spinner overlay on the interaction area while awaiting response |
| **Currency delta feedback** | Floating `+N` text animation originating from the HUD currency/stamina icon on any reward or refill |
| **Popup behavior** | Close button (×) fixed at top-right corner. Background (outside popup panel) tap closes the popup. Entry: scale+fade in 0.25 s cubic-bezier. Exit: scale+fade out 0.2 s. Exceptions: non-dismissible popups have no × and background tap is inert |
| **Text binding** | All user-facing strings bound to `clientstring` table string IDs. Error codes map to `error_messages` table IDs. No hardcoded display strings |
| **Safe area** | Root canvas panels adjusted via `SafeAreaFitter` for notch/home-bar devices |
| **Spine in "static" contexts** | Even decorative/static images use Spine idle loops for ambient life (e.g., stage nodes pulse, currency icons shimmer) |

---

## 2. Bootstrap Scene

**Purpose**: App initialization gate — version check → transition to Title.

### UI States

| State | Layout / Expression |
|-------|---------------------|
| Loading | Full-screen. Centered game logo with Spine idle animation. Bottom animated progress bar |
| Network error | Loading state + centered retry CTA |
| Version OK | Silent fade transition to Title scene (no user action) |
| **Version outdated** | `ForceUpdatePopup` overlays the loading screen (non-dismissible — no × button, no background tap). Single "Update Now" CTA → opens platform app store. User cannot proceed without updating |

### Event-Driven

| Trigger | API | Success | Failure |
|---------|-----|---------|---------|
| Scene load | `GET /api/bootstrap/config` | `RequiredClientVersion ≤ client` → Title scene | Network fail → retry button |
| — | — | `RequiredClientVersion > client` → ForceUpdatePopup | — |

> **Maintenance check is not performed in Bootstrap.** It is checked at the Title → Lobby transition.

---

## 3. Title Scene

**Purpose**: Authentication entry point. Silent login for returning users; OIDC/guest onboarding for new users.

### Silent Login (Scene Entry, Background)

```
Scene entry
└─ Valid local refreshToken?
   ├─ YES → silentLogin (session restore)
   │         ├─ Success → skip Title UI → Lobby scene (fade)
   │         └─ SESSION_EXPIRED → clear token → show Title UI
   └─ NO  → show Title UI
```

### Title UI Layout

| Component | Layout / Expression | Event-Driven |
|-----------|---------------------|--------------|
| Game logo | Center screen, Spine animated idle loop | — |
| "TAP TO START" | Bottom-center, large primary CTA. Pulsing scale attention animation. | Tap → execute TAP TO START auth logic (see below) |
| Google login button | Below logo, left-of-center. UIButtonSkin icon + localized label. Grayed out when Apple auth is active | Tap → client-side OIDC auth (no web redirect) → success: store OAuth token, show Google auth badge, clear Apple badge / fail: toast |
| Apple login button | Below logo, right-of-center. UIButtonSkin icon + localized label. Grayed out when Google auth is active | Tap → client-side OIDC auth → success: store OAuth token, show Apple auth badge, clear Google badge / fail: toast |
| Auth badge | Appears below the authenticated provider button after OIDC success. Provider icon + display name/email (small text). Replaced when switching provider | — (display only) |
| Settings icon | Top-right corner icon button | Tap → SettingPopup |
| Version text | Bottom corner, small non-interactive | — |

### TAP TO START Auth Logic

```
Tap "TAP TO START"
├─ OAuth token stored (Google or Apple OIDC authenticated)
│   └─ POST /api/auth/social (provider + idToken)
│       ├─ Success → check maintenance → Lobby scene
│       └─ Fail → LoginFailedPopup
├─ No OAuth token + valid refreshToken
│   └─ session restore
│       ├─ Success → check maintenance → Lobby scene
│       └─ SESSION_EXPIRED → POST /api/auth/guest (new)
└─ No OAuth token + no refreshToken
    └─ POST /api/auth/guest (new guest creation)
        ├─ Success → check maintenance → Lobby scene
        └─ Fail → toast error

Maintenance check (after any auth success):
├─ Maintenance == true → MaintenancePopup (non-dismissible)
└─ Maintenance == false → Lobby scene transition
```

### Title Popups

| Popup | Trigger | Dismissible | Event-Driven |
|-------|---------|-------------|--------------|
| **SettingPopup** | Settings icon tap | × + background tap | Open: `GET /api/settings` / Save: `PATCH /api/settings` → success: apply + close / fail: toast |
| **MaintenancePopup** | `Maintenance == true` after login | Non-dismissible | Displays `MaintenanceMessage` from bootstrap config |
| **LoginFailedPopup** | Social login server error | × + background tap | Error message display; retry/cancel |

---

## 4. Lobby Scene

### 4-1. HUD (Always Visible, Two-Row Strip)

**Row 1**

| Zone | Component | Event-Driven |
|------|-----------|--------------|
| Left | Circular avatar (UIButtonSkin) + nickname text | Tap → AccountPopup |
| Right | Menu icon (⋮) | Tap → slide-down dropdown panel (see below) |

**Row 2**

| Zone | Component | Event-Driven |
|------|-----------|--------------|
| Left-center | Stamina: icon + `current/max` + recharge countdown (if applicable) | Tap → EnergyPopup |
| Right-center | Soft currency: coin icon (Spine shimmer idle) + amount | Tap → switch to Shop tab |

**Menu Dropdown** (⋮ tap)
- Slides down from top-right corner (not a full popup; lightweight panel)
- Items: [Settings] [Language]
- Background tap → dismiss dropdown

| Dropdown Item | Event-Driven |
|---------------|--------------|
| Settings | SettingPopup → `GET /api/settings` / `PATCH /api/settings` |
| Language | If runtime re-render is lightweight: apply immediately. If heavy: **LanguageChangePopup** ("Changing language requires returning to Title. Continue?" — confirm → Title scene; Lobby re-entry renders updated language) |

HUD data populated by `GET /api/lobby` → `LobbyStateResponse` on scene entry.

---

### 4-2. Tab Bar (Bottom Navigation)

Order (left → right): **상점 \| 홈 \| 랭킹**  
Default selected: **홈** (center)

---

### 4-3. Home Tab

#### Stage Carousel — 3-Focus

| Component | Layout / Expression | Event-Driven |
|-----------|---------------------|--------------|
| Previous stage node | Left, reduced scale + lower opacity (depth/perspective effect) | — |
| **Current stage node** | Center, largest. Stage number label. Star rating (0–3) with Spine fill animation. Locked/unlocked state indicator. Spine idle animation on node | Tap → **StageDetailPopup** |
| Next stage node | Right, reduced scale + lower opacity | — |
| Navigation arrows | Left / right flanking arrows | Single tap: move 1 stage. Long press: progressive speed acceleration (slow start → ramp up as held duration increases) |

Scene entry: `GET /api/lobby` → focus carousel on `NextUnlockedStageId` from `LobbyProgressSummary`.  
Locked nodes: tap triggers lock-shake animation only (no API).

#### Home Tab Content Sections

| Section | Layout / Expression | Event-Driven |
|---------|---------------------|--------------|
| Daily Challenge card | Card below carousel. Streak count + today completion status + play progress bar. Spine completion animation when `CompletedToday == true` | Tap → **DailyChallengePopup** |
| Season Event banner | Card (hidden when `LobbySeasonEvent == null`). Animated banner (Spine) with event name + end date countdown | Tap → **SeasonEventPopup** |

---

### 4-4. Shop Tab

| Component | Layout / Expression | Event-Driven |
|-----------|---------------------|--------------|
| Product list | Vertical scroll. Category section headers. Each card: UIButtonSkin product image (Spine idle where applicable), localized name, price, purchase button | Tab entry: `GET /api/shop/catalog` |
| Purchase button | Per card, disabled when `SoftBalance < PriceSoft` | `POST /api/shop/purchase` → success: HUD currency delta animation / insufficient funds: **BuyItemPopup** / fail: toast |

---

### 4-5. Ranking Tab

| Component | Layout / Expression | Event-Driven |
|-----------|---------------------|--------------|
| Category selector | Top segment control: [Stages Cleared] [Total Score] | Switch: respective `GET /api/ranking/global/*` |
| Leaderboard | Vertical scroll. Top 3 rows with podium highlight styling (gold/silver/bronze). Each row: rank badge, avatar, nickname, value. My row pinned to bottom with highlight | Tab entry: `GET /api/ranking/global/stages` + `GET /api/ranking/me` |
| Empty state | Centered illustration + localized message | — |

---

### 4-6. Lobby Popups

| Popup | Trigger | Dismissible | Event-Driven |
|-------|---------|-------------|--------------|
| **StageDetailPopup** | Stage carousel center node tap | × + background tap | Open: `POST /api/progress/batch` + `GET /api/ranking/stage/{id}` → renders stars/best score/my rank. Play CTA: `POST /api/stage/{id}/start` → success: Game scene / STAMINA_INSUFFICIENT: EnergyPopup / fail: toast |
| **EnergyPopup** | Stamina HUD tap / STAMINA_INSUFFICIENT from stage start | × + background tap | Open: current stamina state (from cached lobby data). Watch Ad: `POST /api/stamina/ad-reward` → success: HUD delta. Paid Refill: `POST /api/stamina/refill` → success: HUD delta / insufficient funds: toast |
| **DailyChallengePopup** | Daily Challenge card tap | × + background tap | Open: `GET /api/daily-challenge` → streak tiles, today's rewards preview. Complete CTA (when `CanComplete`): `POST /api/daily-challenge/complete` → success: **RewardPopup** |
| **RewardPopup** | After DailyChallenge complete | × + background tap | Displays granted rewards. Claim (×1): `POST /api/rewards/claim` (Multiplier: 1). Claim ×2 (ad-gated): `POST /api/rewards/claim` (Multiplier: 2) → success: delta animations + close |
| **AccountPopup** | Avatar HUD tap | × + background tap | Open: `GET /api/account/me` → profile, IsGuest, LinkedProviders. Social link button: client OIDC → server link API |
| **SettingPopup** | Menu dropdown → Settings | × + background tap | Open: `GET /api/settings` / Save: `PATCH /api/settings` → success: apply + close |
| **LanguageChangePopup** | Menu dropdown → Language (when heavy re-render path) | Background tap = cancel | Confirm → Title scene transition |
| **SeasonEventPopup** | Season Event banner tap | × + background tap | Open: `GET /api/events/season` → read-only event info display |
| **BuyItemPopup** | Purchase fail (insufficient soft currency) | × + background tap | Shop tab navigation CTA only (no API) |
| **SessionExpiredPopup** | Auth returns SESSION_EXPIRED (any scene) | Non-dismissible | Confirm → Title scene |
| **ReturnTitlePopup** | Back gesture in Lobby | × + background tap | Confirm → Title scene |
| **ExitGamePopup** | Back gesture on Title (or nested back when already at root) | × + background tap | Confirm → app exit |

---

## 5. Game Scene

**Scene entry**: `GameContext` carries `StageStartResponse` set during Lobby → stage start.

### 5-1. InGame HUD

| Component | Layout / Expression | Event-Driven |
|-----------|---------------------|--------------|
| Pause button | Top-left, icon button | Tap → **PausePopup** |
| Stage number | Top-center label | — |
| Timer | Top-right, `MM:SS` format. Spine urgency animation + red color at ≤ 10 s. Hidden when `TimeLimitSeconds == 0` | Local `StageTimer` |
| Move counter | Top-right (below timer), `used / limit` format. Hidden when `MoveLimit == 0` | Local |
| Objective bar | Thin strip below HUD. Color-coded segment per path goal. Fills as each color path is completed. Spine celebration particle burst per segment completion | Local game state |
| Item toolbar | Bottom fixed row. Per-item slot: UIButtonSkin icon + quantity badge. Quantity = 0 → slot grayed + non-interactable | Tap → `POST /api/items/use` (sessionToken + itemId + quantity: 1) → success: quantity badge decrement + Spine use animation / fail: toast |

### 5-2. Game Board

| Component | Layout / Expression | Event-Driven |
|-----------|---------------------|--------------|
| Path drawing | Drag gesture → Spine/particle trail follows finger along path | Local only |
| Path completion detection | All color paths connected | `POST /api/stage/{id}/end` (Result: "success") → **ClearPopup** / fail: toast |
| Zoom / pan | Pinch zoom + drag for boards larger than screen | Local only |

### 5-3. Game Popups

| Popup | Trigger | Layout / Expression | Event-Driven |
|-------|---------|---------------------|--------------|
| **ClearPopup** | `StageEndResponse` success | Full-screen dim. Spine celebration animation (confetti/character). Star rating animation (0 → N sequential fill). Score display, best-record badge (when `IsBestRecord`). Reward icons. Buttons: [Next Stage] [Retry] [Lobby] | Next Stage: `POST /api/stage/{nextId}/start` → success: Game scene reload / STAMINA_INSUFFICIENT: EnergyPopup. Retry: `POST /api/stage/{id}/start`. Lobby: Lobby scene. `NextStage` button hidden when `NextStageId == null` |
| **PausePopup** | Pause button tap | Full-screen dim (board fully hidden beneath overlay). [Resume] [Retry] [Lobby] buttons. Back-press = Resume | Resume: dismiss. Retry: `POST /api/stage/{id}/start` → Game scene. Lobby: Lobby scene |
| **TimeoutPopup** | `StageTimer.OnTimeUp` fired or move limit reached | Full-screen dim. Spine timeout illustration. [Retry] [Lobby]. Non-dismissible (no × button, no background tap) | Retry: `POST /api/stage/{id}/start`. Lobby: Lobby scene |

---

## 6. Cross-Cutting Notes

### Stamina Cost Model
`POST /api/stage/{id}/start` deducts stamina server-side. The client does not predict stamina deduction; it reads `StaminaCurrent` from `StageStartResponse` and updates HUD after the response.

### Session Token Lifecycle
`StageStartResponse.SessionToken` must be passed to:
- `POST /api/stage/{id}/end`
- `POST /api/items/use`
- `POST /api/stage/{id}/lock` / `extend`

The client stores this in `GameContext` for the duration of the active stage session.

### Popup Stack Management
`PopupManager` maintains a stack. Only one popup is visible at a time (except toast notifications which are independent). Non-dismissible popups block the stack.

### Localization Runtime Behavior
All `LocalizedText` components auto-refresh on `LanguageChanged` event. If the language change triggers a Title-scene round-trip, the new language is applied during Lobby scene initialization.
