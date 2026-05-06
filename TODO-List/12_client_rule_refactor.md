# 12 - Client Rule Refactor

## Direction

- Update the client runtime to match the `nodeMap` + `cellMap` stage model.
- Keep rule behavior compatible with stage editor validation and solver validation.
- Do not hardcode stage colors or generated data assumptions in gameplay code.

## Stage Data

- [ ] Load stages from generated `ingame_stage` data.
- [ ] Decode `nodeMap` into node group IDs.
- [ ] Decode `cellMap` into obstacles, gimmicks, and future cell metadata.
- [ ] Support node group IDs `1..20`.
- [ ] Validate even node counts per present group before gameplay starts.

## Node Colors

- [ ] Load node colors from generated `ingame_node_colors` data.
- [ ] Replace hardcoded `ColorPalette` stage colors with data-driven hex colors.
- [ ] Add fallback/error handling for missing node color rows.

## Path Model

- [ ] Replace the current single-pair color path model with node-group pair connectivity.
- [ ] Support multiple valid pair connections in the same node group.
- [ ] Persist incomplete paths after input ends.
- [ ] Allow incomplete paths to resume only from the current tail.
- [ ] Track path ownership per painted cell.

## Overwrite Erase

- [ ] Replace long-press erase with overwrite behavior.
- [ ] Drawing onto a filled non-obstacle/non-gimmick cell clears previous path ownership.
- [ ] Paint overwritten cells as the active group.
- [ ] Recompute affected path connectivity after ownership changes.
- [ ] Block overwrite attempts on obstacles, gimmicks, and invalid nodes.

## Clear Condition

- [ ] Clear only when every node in every group remains connected as valid pairs.
- [ ] Match clear checks with solver-compatible validation.
- [ ] Keep incomplete or disconnected groups from clearing the stage.

## Large Board

- [ ] Add camera zoom support for large boards.
- [ ] Add camera pan support for large boards.
- [ ] Keep touch/mouse hit testing correct under camera transforms.
- [ ] Evaluate chunked, mesh, or tilemap board rendering before large-board rollout.

<!-- changed: added client implementation tasks for nodeMap/cellMap rule refactor -->
