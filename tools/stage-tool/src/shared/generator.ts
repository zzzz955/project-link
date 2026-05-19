import { MAX_NODE_GROUP_ID, StagePayload, ValidationError, ValidationIssue } from "./stage";
import { normalizeAndValidateStage } from "./validation";

export interface GenerateStageInput {
  width: number;
  height: number;
  difficulty: number;
  nodeCount?: number;
  obstacleCount?: number;
  seed?: number | string;
}

export interface GeneratedStage {
  payload: StagePayload;
  seed: string;
  generatorSeed: number;
  validation: { valid: true; issues: [] };
}

interface Point {
  x: number;
  y: number;
}

interface DifficultyParams {
  minDistance: number;
  edgePreference: boolean;
  obstacleStyle: "edge" | "choke";
}

const MIN_GENERATOR_SIZE = 3;
const MIN_DIFFICULTY = 1;
const MAX_DIFFICULTY = 5;
const OBSTACLE_CODE = 1;
const MAX_BOARD_ATTEMPTS = 40;
const ENDPOINT_ATTEMPTS = 400;

export function generateStagePayload(input: GenerateStageInput): GeneratedStage {
  const width = assertGeneratorInteger(input.width, "width");
  const height = assertGeneratorInteger(input.height, "height");
  const difficulty = assertGeneratorInteger(input.difficulty, "difficulty");
  const issues: ValidationIssue[] = [];

  if (width < MIN_GENERATOR_SIZE) issues.push({ field: "width", message: `must be >= ${MIN_GENERATOR_SIZE}` });
  if (height < MIN_GENERATOR_SIZE) issues.push({ field: "height", message: `must be >= ${MIN_GENERATOR_SIZE}` });
  if (difficulty < MIN_DIFFICULTY || difficulty > MAX_DIFFICULTY) {
    issues.push({ field: "difficulty", message: `must be ${MIN_DIFFICULTY}..${MAX_DIFFICULTY}` });
  }

  const area = width * height;
  const maxByArea = Math.max(1, Math.min(MAX_NODE_GROUP_ID, Math.floor(area / 4)));
  const defaultNodeCount = Math.min(maxByArea, 2 + difficulty * 2);
  const nodeCount = input.nodeCount === undefined
    ? defaultNodeCount
    : assertGeneratorInteger(input.nodeCount, "nodeCount");
  if (nodeCount < 1 || nodeCount > maxByArea) {
    issues.push({ field: "nodeCount", message: `must be 1..${maxByArea}` });
  }

  let obstacleCount: number | undefined;
  if (input.obstacleCount !== undefined) {
    obstacleCount = assertGeneratorInteger(input.obstacleCount, "obstacleCount");
    if (obstacleCount < 0) {
      issues.push({ field: "obstacleCount", message: "must be >= 0" });
    }
  }

  if (issues.length > 0) {
    throw new ValidationError(issues);
  }

  const seed = input.seed === undefined ? defaultSeed(width, height, difficulty, nodeCount) : String(input.seed);
  const generatorSeed = hashSeed(seed);
  let lastFailure = "";

  for (let attempt = 0; attempt < MAX_BOARD_ATTEMPTS; attempt += 1) {
    const rng = createRng(`${seed}#${attempt}`);
    try {
      const candidate = buildCandidate(width, height, difficulty, nodeCount, obstacleCount, rng, generatorSeed);
      normalizeAndValidateStage(1, candidate);
      return { payload: candidate, seed, generatorSeed, validation: { valid: true, issues: [] } };
    } catch (error) {
      lastFailure = error instanceof Error ? error.message : String(error);
    }
  }

  throw new ValidationError([
    {
      field: "generator",
      message: `could not build a solvable ${width}x${height} difficulty ${difficulty} board with ${nodeCount} node group(s)${lastFailure ? `; last failure: ${lastFailure}` : ""}`
    }
  ]);
}

function getDifficultyParams(difficulty: number, width: number, height: number): DifficultyParams {
  const span = width + height;
  return {
    minDistance: Math.max(2, Math.floor(span * (0.05 + difficulty * 0.1))),
    edgePreference: difficulty >= 3,
    obstacleStyle: difficulty >= 3 ? "choke" : "edge",
  };
}

function buildCandidate(
  width: number,
  height: number,
  difficulty: number,
  nodeCount: number,
  obstacleCount: number | undefined,
  rng: () => number,
  generatorSeed: number
): StagePayload {
  const area = width * height;
  const params = getDifficultyParams(difficulty, width, height);
  const nodeMap = new Array<number>(area).fill(0);
  const cellMap = new Array<number>(area).fill(0);

  const endpointSet = new Set<number>();
  const committedIntermediate = new Set<number>();

  // Interleaved placement + routing: place endpoints and route each group immediately.
  // Routability is verified before committing each group, preventing topological dead ends.
  for (let group = 1; group <= nodeCount; group += 1) {
    const blocked = new Set([...endpointSet, ...committedIntermediate]);

    const result = placeRoutableEndpoints(width, height, area, params, blocked, rng);
    if (result === undefined) {
      throw new Error(`endpoint placement failed for group ${group}`);
    }

    const [startCell, endCell, path] = result;
    endpointSet.add(startCell);
    endpointSet.add(endCell);
    nodeMap[startCell] = group;
    nodeMap[endCell] = group;

    for (let i = 1; i < path.length - 1; i += 1) {
      committedIntermediate.add(path[i]);
    }
  }

  const solutionCells = new Set([...endpointSet, ...committedIntermediate]);
  const resolvedObstacleCount = obstacleCount ?? difficultyObstacleCount(width, height, difficulty);
  placeObstacles(cellMap, nodeMap, solutionCells, width, height, params, resolvedObstacleCount, rng);

  return {
    width,
    height,
    timeLimit: 45 + difficulty * 18 + nodeCount * 6,
    moveLimit: 0,
    soft_reward: getSoftReward(difficulty),
    difficulty,
    nodeMap,
    cellMap,
    generatorSeed,
  };
}

function getSoftReward(difficulty: number): number {
  const rewards: Record<number, number> = {
    1: 10,
    2: 15,
    3: 20,
    4: 30,
    5: 40
  };
  return rewards[difficulty] || 10;
}

// Places a pair of endpoints satisfying difficulty constraints, verifying a valid route exists.
// Returns [startCell, endCell, routePath] or undefined if placement is impossible.
function placeRoutableEndpoints(
  width: number,
  height: number,
  area: number,
  params: DifficultyParams,
  blocked: ReadonlySet<number>,
  rng: () => number
): [number, number, number[]] | undefined {
  // Primary: difficulty-aware placement with minDistance + routability check
  for (let attempt = 0; attempt < ENDPOINT_ATTEMPTS; attempt += 1) {
    const start = pickEndpointCell(width, height, params.edgePreference, blocked, rng);
    if (start === undefined) break;

    const startBlocked = new Set([...blocked, start]);
    const end = pickEndpointCell(width, height, params.edgePreference, startBlocked, rng);
    if (end === undefined) continue;

    if (manhattan(indexToPoint(start, width), indexToPoint(end, width)) < params.minDistance) continue;

    const path = bfsShortestPath(start, end, width, height, blocked);
    if (path !== undefined) {
      if (params.edgePreference) { // edgePreference is true for difficulty >= 3
        // High difficulty: try to find a longer path by temporarily blocking cells on the shortest path
        let bestPath = path;
        for (let i = 0; i < 3; i += 1) {
          if (bestPath.length <= 2) break;
          const midIndex = 1 + randomInt(rng, bestPath.length - 2);
          const cellToBlock = bestPath[midIndex];
          const detourPath = bfsShortestPath(start, end, width, height, new Set([...blocked, cellToBlock]));
          if (detourPath && detourPath.length > bestPath.length) {
            bestPath = detourPath;
          }
        }
        return [start, end, bestPath];
      }
      return [start, end, path];
    }
  }

  // Fallback: relax placement preference and distance, keep routability check
  for (let attempt = 0; attempt < ENDPOINT_ATTEMPTS; attempt += 1) {
    const start = randomFreeCell(area, blocked, rng);
    if (start === undefined) break;

    const startBlocked = new Set([...blocked, start]);
    const end = randomFreeCell(area, startBlocked, rng);
    if (end === undefined) continue;

    if (manhattan(indexToPoint(start, width), indexToPoint(end, width)) < 2) continue;

    const path = bfsShortestPath(start, end, width, height, blocked);
    if (path !== undefined) return [start, end, path];
  }

  return undefined;
}

function pickEndpointCell(
  width: number,
  height: number,
  edgePreference: boolean,
  occupied: ReadonlySet<number>,
  rng: () => number
): number | undefined {
  const candidates: number[] = [];

  if (edgePreference) {
    // High difficulty: prefer edge cells
    for (let x = 0; x < width; x += 1) {
      candidates.push(x);
      candidates.push((height - 1) * width + x);
    }
    for (let y = 1; y < height - 1; y += 1) {
      candidates.push(y * width);
      candidates.push(y * width + width - 1);
    }
  } else {
    // Low difficulty: prefer interior cells
    for (let y = 1; y < height - 1; y += 1) {
      for (let x = 1; x < width - 1; x += 1) {
        candidates.push(y * width + x);
      }
    }
    // If board is very small, fallback to any cell
    if (candidates.length < 4) {
      for (let i = 0; i < width * height; i += 1) {
        candidates.push(i);
      }
    }
  }

  shuffle(candidates, rng);
  for (const cell of candidates) {
    if (!occupied.has(cell)) return cell;
  }
  return undefined;
}

function randomFreeCell(
  area: number,
  occupied: ReadonlySet<number>,
  rng: () => number
): number | undefined {
  for (let attempt = 0; attempt < 80; attempt += 1) {
    const cell = randomInt(rng, area);
    if (!occupied.has(cell)) return cell;
  }
  return undefined;
}

function bfsShortestPath(
  start: number,
  end: number,
  width: number,
  height: number,
  blocked: ReadonlySet<number>
): number[] | undefined {
  if (start === end) return [start];
  const queue: number[] = [start];
  const visited = new Set<number>([start]);
  const previous = new Map<number, number>();

  for (let cursor = 0; cursor < queue.length; cursor += 1) {
    const current = queue[cursor];
    if (current === end) return reconstructPath(current, previous);
    for (const next of neighbors(current, width, height)) {
      if (!visited.has(next) && !blocked.has(next)) {
        visited.add(next);
        previous.set(next, current);
        queue.push(next);
      }
    }
  }
  return undefined;
}

function difficultyObstacleCount(width: number, height: number, difficulty: number): number {
  // Increase obstacle density with difficulty to minimize empty cells
  const density = 0.05 + difficulty * 0.08; // 1: 0.13, 2: 0.21, 3: 0.29, 4: 0.37, 5: 0.45
  return Math.floor(width * height * density);
}

function placeObstacles(
  cellMap: number[],
  nodeMap: readonly number[],
  solutionCells: ReadonlySet<number>,
  width: number,
  height: number,
  params: DifficultyParams,
  count: number,
  rng: () => number
): void {
  if (count <= 0) return;

  const area = width * height;
  const candidates: number[] = [];
  for (let i = 0; i < area; i += 1) {
    if (nodeMap[i] === 0 && !solutionCells.has(i)) {
      candidates.push(i);
    }
  }

  shuffle(candidates, rng);

  if (params.obstacleStyle === "choke") {
    // Hard: prefer cells adjacent to many solution path cells (chokepoints)
    candidates.sort((a, b) => {
      const scoreA = neighbors(a, width, height).filter((n) => solutionCells.has(n)).length;
      const scoreB = neighbors(b, width, height).filter((n) => solutionCells.has(n)).length;
      if (scoreA !== scoreB) return scoreB - scoreA;
      // Secondary: prefer being far from edges (interior) to stay near the path network
      return edgeDistance(b, width, height) - edgeDistance(a, width, height);
    });
  } else {
    // Easy: prefer cells close to edges (far from path network)
    candidates.sort((a, b) => edgeDistance(a, width, height) - edgeDistance(b, width, height));
  }

  const target = Math.min(count, candidates.length);
  for (let i = 0; i < target; i += 1) {
    cellMap[candidates[i]] = OBSTACLE_CODE;
  }
}

function edgeDistance(index: number, width: number, height: number): number {
  const x = index % width;
  const y = Math.floor(index / width);
  return Math.min(x, width - 1 - x, y, height - 1 - y);
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

function indexToPoint(index: number, width: number): Point {
  return { x: index % width, y: Math.floor(index / width) };
}

function manhattan(a: Point, b: Point): number {
  return Math.abs(a.x - b.x) + Math.abs(a.y - b.y);
}

function shuffle<T>(values: T[], rng: () => number): void {
  for (let index = values.length - 1; index > 0; index -= 1) {
    const swapIndex = randomInt(rng, index + 1);
    const value = values[index];
    values[index] = values[swapIndex];
    values[swapIndex] = value;
  }
}

function randomInt(rng: () => number, maxExclusive: number): number {
  return Math.floor(rng() * maxExclusive);
}

function assertGeneratorInteger(value: unknown, field: string): number {
  if (!Number.isInteger(value)) {
    throw new ValidationError([{ field, message: "must be an integer" }]);
  }
  return value as number;
}

function defaultSeed(width: number, height: number, difficulty: number, nodeCount: number): string {
  return `${width}x${height}-d${difficulty}-n${nodeCount}`;
}

function createRng(seed: string): () => number {
  let state = hashSeed(seed);
  return () => {
    state += 0x6d2b79f5;
    let value = state;
    value = Math.imul(value ^ (value >>> 15), value | 1);
    value ^= value + Math.imul(value ^ (value >>> 7), value | 61);
    return ((value ^ (value >>> 14)) >>> 0) / 4294967296;
  };
}

function hashSeed(seed: string): number {
  let state = 2166136261;
  for (let index = 0; index < seed.length; index += 1) {
    state ^= seed.charCodeAt(index);
    state = Math.imul(state, 16777619);
  }
  return state >>> 0;
}
