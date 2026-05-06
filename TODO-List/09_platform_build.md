# 09 — Platform Build

---

## Android (Primary Target)

### Player Settings

- [ ] Company name + product name set
- [ ] Package name: `com.<studio>.<gamename>` — decide and lock in
- [ ] Version: `1.0.0` / Version Code: `1`
- [ ] Minimum API: 26 (Android 8.0) | Target API: 34 (Android 14)
- [ ] Scripting backend: IL2CPP
- [ ] Target architectures: ARM64 + ARMv7

### Signing

- [ ] Generate release keystore (store outside repo — document path in team wiki)
- [ ] Configure keystore alias + passwords via environment variables or Unity Cloud Build

### Assets

- [ ] Adaptive icon: foreground layer (512×512 PNG) + background layer
- [ ] Splash screen: configure in `Project Settings > Player > Splash Image`
- [ ] `AndroidManifest.xml`: `INTERNET` permission, AdMob App ID meta-data

### Build & Test

- [ ] Test on at least one physical Android device before submission
- [ ] Build signed AAB (Android App Bundle) for Play Store upload
- [ ] Verify AdMob test ads appear correctly on device

### Google Play Console

- [ ] Create app listing
- [ ] Upload signed AAB to Internal Testing track
- [ ] Complete store listing: title, short description, full description
- [ ] Upload screenshots: phone (min 2), 7" tablet (optional)
- [ ] Upload feature graphic (1024×500 PNG)
- [ ] Content rating questionnaire
- [ ] Target audience declaration (affects ad content policy)

---

## iOS (Pending)

> No Apple developer account — skip until account is obtained.
> When ready: Bundle ID, provisioning profile, Xcode project export, AdMob iOS App ID, App Store Connect listing.
