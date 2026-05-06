# 07 - Stage Editor (Web Admin Tool)

## Direction

- Build a local TypeScript web tool for stage authoring, validation, and CSV writes.
- Edit source CSVs under `shared/datas/ingame/`; never edit generated files directly.
- Production builds consume generated/static data and must not expose stage mutation.
- CI must run the same stage validator used by the tool before data changes are merged.

## Data Model

- [ ] Migrate stage authoring to `ingame_stage.csv` as one row per stage.
- [ ] Use `nodeMap` + `cellMap` row-major base36 fixed-width 2-char layers.
- [ ] Use `ingame_node_colors.csv` as the canonical node group color table.
- [ ] Keep legacy `ingame_stage_info.csv` and `ingame_stage_nodes.csv` until Unity client migration is complete.

## Stage Tool

- [ ] Web UI: explicit `stageId` lookup/add/update/delete.
- [ ] Web UI: visual grid editor for node groups, obstacles, and future gimmicks.
- [ ] Web UI: board size, time limit, difficulty, seed, and extensible stage metadata.
- [ ] Server API: `GET /api/stages`, `GET /api/stages/:stageId`.
- [ ] Server API: `POST /api/stages/:stageId`, `PUT /api/stages/:stageId`, `DELETE /api/stages/:stageId`.
- [ ] Server API: `GET /api/node-colors`.
- [ ] Server: atomic CSV write with metadata rows preserved.

## Validation

- [ ] Stage IDs must be contiguous from `1..maxStageId`.
- [ ] Add only `maxStageId + 1`; delete only `maxStageId`; update only existing stages.
- [ ] `nodeMap` and `cellMap` length must equal `width * height * 2`.
- [ ] Node cells cannot overlap obstacles or gimmicks.
- [ ] Node groups must be in `1..20` and each present group must have an even node count.
- [ ] Save only when structural validation and solver validation pass.
- [ ] CI command must fail on stage sequence gaps or invalid stage rows.

## Client Rule Refactor

- [ ] Load node colors from generated `ingame_node_colors` data.
- [ ] Replace hardcoded `ColorPalette` stage colors with data-driven hex colors.
- [ ] Support incomplete path persistence.
- [ ] Allow drawing to resume from an unfinished path tail.
- [ ] Replace long-press erase with overwrite behavior: drawing onto a filled non-obstacle/non-gimmick cell clears the previous path cell and paints the active group.
- [ ] Clear condition: every node in every group must remain connected as valid pairs.
- [ ] Support large-board camera zoom and pan.
- [ ] Consider chunked/mesh/tilemap board rendering before large board rollout.

<!-- changed: stage editor scope updated for nodeMap/cellMap, node color table, contiguous CRUD, validation, and path rule refactor -->
