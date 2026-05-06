# 08 — Sound & VFX Polish

Existing: `SoundManager.cs`, `HapticManager.cs` in `Core/`

---

## BGM

- [ ] Prepare 3 BGM tracks: Title (calm), Lobby (upbeat), Game (ambient/focus)
- [ ] Wire to `SoundManager`: `PlayBGM(SceneType)` called by each scene on load
- [ ] Crossfade on scene transition (fade out old, fade in new during `SceneLoader` transition)
- [ ] Loop seamlessly (set loop point in audio clip or via `AudioSource.loop`)

---

## SFX

- [ ] Path draw: soft tick sound per cell entered during drag
- [ ] Pipe complete (color fully connected): satisfying chime
- [ ] Path erase (circular gauge trigger): soft swoosh
- [ ] Stage clear: fanfare / positive jingle
- [ ] Invalid move: subtle error click

Wire each SFX call through `SoundManager.PlaySFX(SFXType)`.

---

## Clear VFX

- [ ] Cell pulse animation: scale bounce on cells when the last path is drawn
- [ ] Stage clear particle burst: `ParticleSystem` at board center, auto-destroy
- [ ] Pipe glow on completion: swap material or activate emission on completed path `LineRenderer`

---

## Audio Import Settings

- [ ] BGM clips: Vorbis compression, streaming load type
- [ ] SFX clips: PCM or ADPCM, decompress on load (short clips)
- [ ] All clips: Force Mono unless stereo is intentional

---

## Integration

- [ ] `SoundManager` volume controlled by Settings toggle (see `02_outgame_ui.md`)
- [ ] `HapticManager` tied to Settings haptic toggle; trigger on pipe complete and stage clear
