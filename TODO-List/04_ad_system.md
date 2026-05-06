# 04 — Ad System (AdMob)

SDK: Google Mobile Ads Unity Plugin (latest LTS)
Platform: Android (iOS pending — see `09_platform_build.md`)

---

## SDK Integration

- [ ] Import Google Mobile Ads Unity Plugin via Unity Package Manager / `.unitypackage`
- [ ] Set AdMob App ID in `Assets/Plugins/Android/AndroidManifest.xml`
- [ ] Call `MobileAds.Initialize()` in `BootstrapEntry` before any ad load
- [ ] Create `AdManager` singleton; register in Bootstrap DDL alongside other managers

---

## Banner Ad

Placement: bottom of Lobby scene

- [ ] Load and show banner on Lobby scene load
- [ ] Destroy/hide banner on Lobby scene exit
- [ ] Ad unit ID: use test ID during development — replace before release

---

## Interstitial Ad

Placement: after every N stage clears (default N = 3, make configurable)

- [ ] Preload interstitial when Game scene loads
- [ ] Track clear count in `AdManager`; show when threshold is reached, reset counter
- [ ] Reload immediately after display

---

## Rewarded Ad

No current trigger (hints removed, IAP deferred). Stub in place for future use.

- [ ] Implement `ShowRewardedAd(Action onRewarded)` in `AdManager`
- [ ] Preload on demand
- [ ] Wire up to a future reward trigger when defined

---

## Notes

- Use AdMob test ad unit IDs during all development and QA
- Replace with real ad unit IDs only in the final release build
- Ad requests require `INTERNET` permission in `AndroidManifest.xml` (already needed for server sync)
