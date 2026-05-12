# Project Link — Unity6 UI Spec

> Machine-readable UI hierarchy + inspector specs for AI agents to scaffold Unity6 prefabs/scenes.

## Files
| File | Purpose |
|------|---------|
| `scenes.json` | Scene canvases (Bootstrap, Title, Lobby, Game) — full GameObject tree |
| `popups.json` | All popup prefabs — same tree schema |
| `strings.json` | Localization keys (en / ko) used by every `stringKey` reference |
| `skin-keys.json` | `UIButtonSkin` element names with intended sprite role + size hint |
| `flow.json` | Navigation graph (which trigger opens which popup / scene) |

## Tree node schema
Every node represents a single `GameObject` under a parent.

```jsonc
{
  "name": "string",              // GameObject.name
  "rect": {                      // RectTransform — see Anchor Presets
    "anchor": "preset-id",       // e.g. "stretch", "top-center", "bottom-stretch"
    "pivot":  [px, py],          // optional, defaults from preset
    "anchoredPos": [x, y],       // optional
    "sizeDelta":  [w, h],        // optional
    "offsets":    [l, t, r, b]   // for stretch anchors only
  },
  "components": [                // ordered list, mapped 1:1 to Unity components
    { "type": "Image",        "color": "#FFFFFFAA", "raycast": true, "skinKey": "btn_primary" },
    { "type": "TMP_Text",     "stringKey": "title.start", "size": 28, "weight": "Bold", "align": "Center" },
    { "type": "Button",       "onClick": "evt.title.tapToStart" },
    { "type": "VerticalLayoutGroup", "spacing": 12, "padding": [16,16,16,16], "childForceExpand": [true,false] },
    { "type": "ContentSizeFitter", "vertical": "PreferredSize" },
    { "type": "Mask" },
    { "type": "CanvasGroup",  "alpha": 1, "interactable": true, "blocksRaycasts": true },
    { "type": "<ScriptName>", "script": "Assets/Scripts/UI/...cs", "fields": { ... } }
  ],
  "children": [ ... ]            // recursive
}
```

### Anchor Presets (shorthand)
Maps to Unity RectTransform `anchorMin` / `anchorMax`:

| id | anchorMin | anchorMax | typical pivot |
|----|-----------|-----------|---------------|
| `top-left`        | (0,1) | (0,1) | (0,1) |
| `top-center`      | (.5,1) | (.5,1) | (.5,1) |
| `top-right`       | (1,1) | (1,1) | (1,1) |
| `middle-left`     | (0,.5) | (0,.5) | (0,.5) |
| `middle-center`   | (.5,.5) | (.5,.5) | (.5,.5) |
| `middle-right`    | (1,.5) | (1,.5) | (1,.5) |
| `bottom-left`     | (0,0) | (0,0) | (0,0) |
| `bottom-center`   | (.5,0) | (.5,0) | (.5,0) |
| `bottom-right`    | (1,0) | (1,0) | (1,0) |
| `stretch`         | (0,0) | (1,1) | (.5,.5) |
| `top-stretch`     | (0,1) | (1,1) | (.5,1) |
| `bottom-stretch`  | (0,0) | (1,0) | (.5,0) |
| `left-stretch`    | (0,0) | (0,1) | (0,.5) |
| `right-stretch`   | (1,0) | (1,1) | (1,.5) |
| `middle-stretch`  | (0,.5) | (1,.5) | (.5,.5) |

## Canvas defaults (all scenes)
```
Canvas.renderMode      = ScreenSpaceOverlay
CanvasScaler.uiScaleMode = ScaleWithScreenSize
CanvasScaler.referenceResolution = (1080, 1920)
CanvasScaler.screenMatchMode = MatchWidthOrHeight
CanvasScaler.matchWidthOrHeight = 0.5
CanvasScaler.referencePixelsPerUnit = 100
GraphicRaycaster.ignoreReversedGraphics = true
```

## Safe Area
A `SafeAreaFitter` component (custom script) must wrap every top-level scene root that has HUD or top/bottom-pinned UI. Apply at:
- `BootstrapCanvas/SafeArea`
- `TitleCanvas/SafeArea`
- `LobbyCanvas/SafeArea`
- `GameCanvas/SafeArea`
- All popups → `PopupRoot/SafeArea`

## Skin System
Every `Image` or `Button` that intends to host swappable art carries a `skinKey`. At runtime a `UIButtonSkin.ScriptableObject` maps that key → `Sprite`. Apply via the `SkinTarget` component on the same GameObject:
```
SkinTarget.skinKey = "btn_primary"
SkinTarget.target  = <Image reference>
```

## Localization
Every visible string uses `stringKey`. Apply via `LocalizedText` component:
```
LocalizedText.key = "title.tap_to_start"
LocalizedText.target = <TMP_Text reference>
```

## Component naming conventions
- `Panel_*` — non-interactive container with `Image` background
- `Card_*` — surface card
- `Btn_*` — interactive `Button`
- `Txt_*` — `TMP_Text`
- `Icon_*` — small `Image` (square)
- `Slot_*` — image slot driven by `UIButtonSkin`
- `Group_*` — pure layout container (no `Image`)
- `HUD_*` — heads-up display
- `Pop_*` — popup root
- `Tab_*` — tab body
