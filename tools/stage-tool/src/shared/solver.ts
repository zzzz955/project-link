import { Stage, ValidationIssue } from "./stage";

interface Point {
  x: number;
  y: number;
}

interface IndexedPoint extends Point {
  index: number;
}

export interface SolverPath {
  nodeGroupId: number;
  from: number;
  to: number;
  cells: number[];
}

export interface SolverValidationResult {
  valid: boolean;
  issues: ValidationIssue[];
  paths: SolverPath[];
}

const MAX_PAIR_PATH_OPTIONS = 80;
const MAX_GROUP_ROUTE_OPTIONS = 160;
const BASE_MAX_SOLVER_STATES = 20000;
const MAX_SOLVER_STATES_PER_CELL = 40;

export function validateStageSolvable(stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">): SolverValidationResult {
  const groups = [...collectNodeGroups(stage).entries()];
  const maxStates = Math.max(BASE_MAX_SOLVER_STATES, stage.width * stage.height * MAX_SOLVER_STATES_PER_CELL);
  const context = { states: 0, maxStates };
  const solved = solveGroups(stage, groups, new Set(), [], context);
  if (solved !== undefined) {
    return { valid: true, issues: [], paths: solved };
  }

  return {
    valid: false,
    issues: groups.map(([nodeGroupId]) => ({
      field: "nodeMap",
      message: `nodeGroupId ${nodeGroupId} cannot connect all node pairs`
    })),
    paths: []
  };
}

function solveGroups(
  stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">,
  remaining: Array<[number, IndexedPoint[]]>,
  occupied: Set<number>,
  paths: SolverPath[],
  context: { states: number; maxStates: number }
): SolverPath[] | undefined {
  context.states += 1;
  if (context.states > context.maxStates) {
    return undefined;
  }
  if (remaining.length === 0) {
    return paths;
  }

  const candidates = remaining
    .map(([nodeGroupId, nodes], index) => ({
      index,
      nodeGroupId,
      options: matchGroupOptions(stage, nodeGroupId, nodes, occupied)
    }))
    .sort((a, b) => a.options.length - b.options.length || a.nodeGroupId - b.nodeGroupId);

  // Forward check: any group with 0 options means this branch is unsolvable
  if (candidates[0]?.options.length === 0) {
    return undefined;
  }

  const next = candidates[0];
  if (next === undefined) {
    return undefined;
  }

  const nextRemaining = remaining.filter((_, index) => index !== next.index);
  for (const option of next.options) {
    const nextOccupied = new Set(occupied);
    for (const path of option) {
      for (const cell of path.cells) {
        nextOccupied.add(cell);
      }
    }
    const solved = solveGroups(stage, nextRemaining, nextOccupied, [...paths, ...option], context);
    if (solved !== undefined) {
      return solved;
    }
  }

  return undefined;
}

function pathCost(paths: readonly SolverPath[]): number {
  return paths.reduce((sum, path) => sum + path.cells.length, 0);
}

function matchGroupOptions(
  stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">,
  nodeGroupId: number,
  nodes: readonly IndexedPoint[],
  occupied: ReadonlySet<number>
): SolverPath[][] {
  if (nodes.length % 2 !== 0) {
    return [];
  }
  return matchRemainingOptions(stage, nodeGroupId, [...nodes], new Set(occupied)).slice(0, MAX_GROUP_ROUTE_OPTIONS);
}

function matchRemainingOptions(
  stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">,
  nodeGroupId: number,
  remaining: IndexedPoint[],
  occupied: Set<number>
): SolverPath[][] {
  if (remaining.length === 0) {
    return [[]];
  }

  const start = remaining[0];
  const results: SolverPath[][] = [];
  const candidates = remaining.slice(1).flatMap((goal, offset) =>
    findPathOptions(stage, start, goal, occupied).map((path) => ({
      goal,
      index: offset + 1,
      path
    }))
  ).sort((a, b) => a.path.length - b.path.length || a.goal.index - b.goal.index);

  for (const candidate of candidates) {
    const nextOccupied = new Set(occupied);
    for (const cell of candidate.path) {
      nextOccupied.add(cell);
    }

    const nextRemaining = remaining.filter((_, index) => index !== 0 && index !== candidate.index);
    const restOptions = matchRemainingOptions(stage, nodeGroupId, nextRemaining, nextOccupied);
    for (const rest of restOptions) {
      results.push([
        { nodeGroupId, from: start.index, to: candidate.goal.index, cells: candidate.path },
        ...rest
      ]);
      if (results.length >= MAX_GROUP_ROUTE_OPTIONS) {
        return results.sort((a, b) => pathCost(a) - pathCost(b));
      }
    }
  }

  return results.sort((a, b) => pathCost(a) - pathCost(b));
}

function collectNodeGroups(stage: Pick<Stage, "width" | "height" | "nodeMap">): Map<number, IndexedPoint[]> {
  const groups = new Map<number, IndexedPoint[]>();
  stage.nodeMap.forEach((nodeGroupId, index) => {
    if (nodeGroupId <= 0) {
      return;
    }
    const nodes = groups.get(nodeGroupId) ?? [];
    nodes.push({ index, x: index % stage.width, y: Math.floor(index / stage.width) });
    groups.set(nodeGroupId, nodes);
  });
  return new Map([...groups.entries()].sort((a, b) => a[0] - b[0]));
}

function findPathOptions(
  stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">,
  start: IndexedPoint,
  goal: IndexedPoint,
  occupied: ReadonlySet<number>
): number[][] {
  const shortest = findShortestPath(stage, start, goal, occupied);
  if (shortest === undefined) {
    return [];
  }

  const results: number[][] = [shortest];
  const queue: number[][] = [shortest];
  const seen = new Set<string>([shortest.join(",")]);

  for (let cursor = 0; cursor < queue.length && results.length < MAX_PAIR_PATH_OPTIONS; cursor += 1) {
    const basePath = queue[cursor];
    for (const blockedCell of basePath.slice(1, -1)) {
      const nextOccupied = new Set(occupied);
      nextOccupied.add(blockedCell);
      const detour = findShortestPath(stage, start, goal, nextOccupied);
      if (detour === undefined) {
        continue;
      }
      const key = detour.join(",");
      if (seen.has(key)) {
        continue;
      }
      seen.add(key);
      results.push(detour);
      queue.push(detour);
      if (results.length >= MAX_PAIR_PATH_OPTIONS) {
        break;
      }
    }
  }

  return results.sort((a, b) => a.length - b.length).slice(0, MAX_PAIR_PATH_OPTIONS);
}

function findShortestPath(
  stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">,
  start: IndexedPoint,
  goal: IndexedPoint,
  occupied: ReadonlySet<number>
): number[] | undefined {
  const queue: number[] = [start.index];
  const visited = new Set<number>([start.index]);
  const previous = new Map<number, number>();

  for (let cursor = 0; cursor < queue.length; cursor += 1) {
    const current = queue[cursor];
    if (current === goal.index) {
      return reconstructPath(current, previous);
    }

    for (const next of neighbors(current, stage.width, stage.height)) {
      if (visited.has(next) || isBlocked(stage, next, start.index, goal.index, occupied)) {
        continue;
      }
      visited.add(next);
      previous.set(next, current);
      queue.push(next);
    }
  }

  return undefined;
}

function neighbors(index: number, width: number, height: number): number[] {
  const x = index % width;
  const y = Math.floor(index / width);
  const result: number[] = [];
  if (x > 0) result.push(index - 1);
  if (x < width - 1) result.push(index + 1);
  if (y > 0) result.push(index - width);
  if (y < height - 1) result.push(index + width);
  return result;
}

function isBlocked(
  stage: Pick<Stage, "nodeMap" | "cellMap">,
  index: number,
  startIndex: number,
  goalIndex: number,
  occupied: ReadonlySet<number>
): boolean {
  if (index === startIndex || index === goalIndex) {
    return false;
  }
  return stage.cellMap[index] > 0 || stage.nodeMap[index] > 0 || occupied.has(index);
}

function reconstructPath(goal: number, previous: ReadonlyMap<number, number>): number[] {
  const path = [goal];
  let current = goal;
  while (previous.has(current)) {
    current = previous.get(current) as number;
    path.push(current);
  }
  return path.reverse();
}
