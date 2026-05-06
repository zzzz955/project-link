# 07 — Stage Editor (Unity Editor Tool)

Type: `EditorWindow` — Editor-only, not available at runtime.
Location: `client/project-link/Assets/Editor/StageEditorWindow.cs` (create `Editor/` folder)
Output: CSV rows appended to `shared/datas/ingame/` — fed into `npm run gen:data` pipeline.

---

## Editor Window

- [ ] Create `Assets/Editor/` directory
- [ ] Implement `StageEditorWindow : EditorWindow`; open via menu `Tools > Stage Editor`
- [ ] Grid size input: width (2–10) × height (2–10)
- [ ] Color slot panel: list of active colors (pulled from `ColorPalette.cs`)
  - Select a color, then click two cells to place endpoint pair
  - Click an occupied cell again to clear it
- [ ] Grid render: draw cells as colored circles/squares using `GUI`/`EditorGUI`
- [ ] Stage ID field: auto-increments from last row in CSV; editable

---

## Validation

- [ ] "Validate" button — runs solver before allowing export
- [ ] Solver: backtracking DFS
  - All cells must be fillable
  - Each color must have exactly 2 endpoints
  - Returns: solvable (bool) + one example solution path (for preview)
- [ ] Display error inline if validation fails; block export
- [ ] Show solution preview overlay on valid result

---

## Export

- [ ] "Export" button (enabled only after successful validation)
- [ ] Append one row to `shared/datas/ingame/ingame_stage_nodes.csv`
- [ ] Append one row to `shared/datas/ingame/ingame_stage_info.csv`
- [ ] Column format must match existing CSV headers exactly
- [ ] After export: dialog prompting to run `npm run gen:data`

---

## CSV Format Reference

Source files: `shared/datas/ingame/`
Generated C# models: `Assets/Scripts/Data/Generated/ingame/IngameStageNodes.cs`, `IngameStageInfo.cs`
Match column names from generated model field names exactly.
