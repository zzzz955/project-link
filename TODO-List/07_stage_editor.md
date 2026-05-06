# 07 - Stage Editor (Web Admin Tool)

## Direction

- Build a local TypeScript web tool for stage authoring, validation, and CSV writes.
- Edit source CSVs under `shared/datas/ingame/`; never edit generated files directly.
- Production builds consume generated/static data and must not expose stage mutation.
- CI must run the same stage validator used by the tool before data changes are merged.

## Data Model

- [x] Migrate stage authoring to `ingame_stage.csv` as one row per stage.
- [x] Use `nodeMap` + `cellMap` row-major base36 fixed-width 2-char layers.
- [x] Use `ingame_node_colors.csv` as the canonical node group color table.
- [x] Keep legacy `ingame_stage_info.csv` and `ingame_stage_nodes.csv` until Unity client migration is complete.

## Stage Tool

- [x] Web UI: explicit `stageId` lookup/add/update/delete.
- [x] Web UI: visual grid editor for node groups, obstacles, and future gimmicks.
- [x] Web UI: board size, time limit, difficulty, seed, and extensible stage metadata.
- [x] Web UI: validation preview before save.
- [x] Web UI: auto-generate stage drafts from board size, difficulty `1..5`, and optional seed.
- [x] Server API: `GET /api/stages`, `GET /api/stages/:stageId`.
- [x] Server API: `POST /api/stages/:stageId`, `PUT /api/stages/:stageId`, `DELETE /api/stages/:stageId`.
- [x] Server API: `POST /api/stages/generate`.
- [x] Server API: `GET /api/node-colors`.
- [x] Server: atomic CSV write with metadata rows preserved.
- [x] Generator: emit generated client/server stage data from source CSV.

## Validation

- [x] Stage IDs must be contiguous from `1..maxStageId`.
- [x] Add only `maxStageId + 1`; delete only `maxStageId`; update only existing stages.
- [x] `nodeMap` and `cellMap` length must equal `width * height * 2`.
- [x] Node cells cannot overlap obstacles or gimmicks.
- [x] Node groups must be in `1..20` and each present group must have an even node count.
- [x] Save only when structural validation and solver validation pass.
- [x] CI command must fail on stage sequence gaps or invalid stage rows.

## Client Rule Refactor

- Runtime client work is tracked in `12_client_rule_refactor.md`.

<!-- changed: Stage Editor implementation checklist completed; runtime client refactor moved to 12_client_rule_refactor.md -->
