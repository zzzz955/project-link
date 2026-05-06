# InGame Game Rules

## Stage Data

- Stage data uses two row-major map layers:
  - `nodeMap`: node group placement.
  - `cellMap`: obstacles, gimmicks, and other non-node cell metadata.
- `nodeMap` group IDs are `1..20`.
- Node group colors are loaded from `ingame_node_colors.csv`.
- Nodes with the same `nodeGroupId` share the same gameplay property and display color.
- Each present node group must contain an even number of nodes.
- Nodes in the same group must be connectable as valid pairs.
- Obstacles and gimmicks are encoded in `cellMap`.

## Board

- The board is a rectangular 2D grid.
- Coordinates are integer `(x, y)` cells inside the board bounds.
- A cell can contain one primary authored feature from the stage data:
  - Empty cell.
  - Node.
  - Obstacle.
  - Gimmick.
- Runtime path ownership is separate from authored stage data.

## Node Groups

- A node group represents one color/property family.
- Valid group IDs are `1..20`.
- The visual color for each group comes from `ingame_node_colors.csv`.
- Every node in a group must be paired with another node from the same group.
- A group with an odd node count is invalid stage data.

## Drawing

- Drawing starts from a node or from the tail of an incomplete path owned by the active group.
- Incomplete paths persist after input ends.
- An incomplete path can resume only from its current tail.
- Movement is orthogonal only: up, down, left, right.
- Drawing cannot pass through obstacles or gimmicks unless a future gimmick-specific rule explicitly allows it.
- Drawing into a node is valid only when the node belongs to the active group and completes a valid pair.
- Drawing over an already-filled non-obstacle/non-gimmick cell overwrites that cell:
  - Clear previous path ownership for the cell.
  - Paint the cell as part of the active group path.
  - Recompute affected path connectivity after the overwrite.

## Path Model

- Paths are owned by node groups.
- A group can have multiple valid pair connections when it has more than two nodes.
- A path segment is valid only when it is orthogonally contiguous.
- Runtime path state must support:
  - Active group ownership per painted cell.
  - Incomplete path persistence.
  - Tail-only resume for incomplete paths.
  - Overwrite erase when another group paints an occupied drawable cell.
  - Connectivity checks that match solver validation.

## Clear Condition

- A stage is clear only when every node in every group remains connected as valid pairs.
- Every present node group must satisfy:
  - Even node count.
  - All nodes paired with another node in the same group.
  - Each pair connected by a valid path.
  - No pair connection violates obstacles, gimmicks, board bounds, or ownership rules.
- Filling every empty cell is not required unless a future stage rule explicitly requires it.

## Invalid Input

- Reject drawing attempts that start outside a valid node or incomplete-path tail.
- Reject non-orthogonal movement.
- Reject movement outside board bounds.
- Reject movement through obstacles or gimmicks unless a future gimmick rule allows it.
- Reject completion against a node from another group.
- Preserve existing incomplete paths when input ends before a valid pair is completed.

## Large Board Support

- Large boards require camera zoom and pan support.
- Input hit testing must remain accurate under camera transform.
- Rendering should be prepared for larger grids with tilemap, mesh, chunk, or equivalent batching when needed.

## Data Driven Requirements

- Do not hardcode node colors in client game logic.
- Load generated stage data from the shared data pipeline.
- Edit source CSVs under `shared/datas/**`; never edit generated files directly.
- Stage validation must use the same structural rules as runtime and solver validation.
