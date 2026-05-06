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

export function validateStageSolvable(stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">): SolverValidationResult {
  const issues: ValidationIssue[] = [];
  const paths: SolverPath[] = [];
  const occupied = new Set<number>();
  const groups = collectNodeGroups(stage);

  for (const [nodeGroupId, nodes] of groups) {
    const matched = matchGroup(stage, nodeGroupId, nodes, occupied);
    if (matched === undefined) {
      issues.push({ field: "nodeMap", message: `nodeGroupId ${nodeGroupId} cannot connect all node pairs` });
      continue;
    }

    for (const path of matched) {
      paths.push(path);
      for (const cell of path.cells) {
        occupied.add(cell);
      }
    }
  }

  return { valid: issues.length === 0, issues, paths };
}

function matchGroup(
  stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">,
  nodeGroupId: number,
  nodes: readonly IndexedPoint[],
  occupied: ReadonlySet<number>
): SolverPath[] | undefined {
  if (nodes.length % 2 !== 0) {
    return undefined;
  }
  return matchRemaining(stage, nodeGroupId, [...nodes], new Set(occupied));
}

function matchRemaining(
  stage: Pick<Stage, "width" | "height" | "nodeMap" | "cellMap">,
  nodeGroupId: number,
  remaining: IndexedPoint[],
  occupied: Set<number>
): SolverPath[] | undefined {
  if (remaining.length === 0) {
    return [];
  }

  const start = remaining[0];
  const candidates = remaining.slice(1)
    .map((goal, offset) => ({
      goal,
      index: offset + 1,
      path: findPath(stage, start, goal, occupied)
    }))
    .filter((candidate): candidate is { goal: IndexedPoint; index: number; path: number[] } => candidate.path !== undefined)
    .sort((a, b) => a.path.length - b.path.length || a.goal.index - b.goal.index);

  for (const candidate of candidates) {
    const nextOccupied = new Set(occupied);
    for (const cell of candidate.path) {
      nextOccupied.add(cell);
    }

    const nextRemaining = remaining.filter((_, index) => index !== 0 && index !== candidate.index);
    const rest = matchRemaining(stage, nodeGroupId, nextRemaining, nextOccupied);
    if (rest !== undefined) {
      return [
        { nodeGroupId, from: start.index, to: candidate.goal.index, cells: candidate.path },
        ...rest
      ];
    }
  }

  return undefined;
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

function findPath(
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
