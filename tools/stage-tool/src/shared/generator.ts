import { MAX_NODE_GROUP_ID, StagePayload, ValidationError, ValidationIssue } from "./stage";
import { normalizeAndValidateStage } from "./validation";

export interface GenerateStageInput {
  width: number;
  height: number;
  difficulty: number;
  nodeCount?: number;
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

interface GeneratedPath {
  group: number;
  cells: number[];
}

const MIN_GENERATOR_SIZE = 3;
const MIN_DIFFICULTY = 1;
const MAX_DIFFICULTY = 5;
const OBSTACLE_CODE = 1;
const RESERVED_GIMMICK_CODE = 2;
const MAX_BOARD_ATTEMPTS = 160;
const PATH_ATTEMPTS_PER_GROUP = 500;

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

  if (issues.length > 0) {
    throw new ValidationError(issues);
  }

  const seed = input.seed === undefined ? defaultSeed(width, height, difficulty, nodeCount) : String(input.seed);
  const generatorSeed = hashSeed(seed);
  let lastFailure = "";

  for (let attempt = 0; attempt < MAX_BOARD_ATTEMPTS; attempt += 1) {
    const rng = createRng(`${seed}#${attempt}`);
    try {
      const candidate = buildCandidate(width, height, difficulty, nodeCount, rng, generatorSeed);
      normalizeAndValidateStage(1, candidate);
      return { payload: candidate, seed, generatorSeed, validation: { valid: true, issues: [] } };
    } catch (error) {
      lastFailure = error instanceof Error ? error.message : String(error);
      // Retry with a deterministic sub-seed; generation must return only solver-valid boards.
    }
  }

  throw new ValidationError([
    {
      field: "generator",
      message: `could not build a solvable ${width}x${height} difficulty ${difficulty} board with ${nodeCount} node group(s)${lastFailure ? `; last failure: ${lastFailure}` : ""}`
    }
  ]);
}

function buildCandidate(
  width: number,
  height: number,
  difficulty: number,
  nodeCount: number,
  rng: () => number,
  generatorSeed: number
): StagePayload {
  const area = width * height;
  const nodeMap = new Array<number>(area).fill(0);
  const cellMap = new Array<number>(area).fill(0);
  const paths: GeneratedPath[] = [];
  const occupied = new Set<number>();

  for (let group = 1; group <= nodeCount; group += 1) {
    const path = carveGroupPath(width, height, difficulty, nodeCount, occupied, rng);
    if (path === undefined) {
      throw new Error("path generation failed");
    }
    paths.push({ group, cells: path });
    for (const cell of path) {
      occupied.add(cell);
    }
    nodeMap[path[0]] = group;
    nodeMap[path[path.length - 1]] = group;
  }

  addDifficultyCells(cellMap, nodeMap, paths, width, height, difficulty, rng);

  return {
    width,
    height,
    timeLimit: 45 + difficulty * 18 + nodeCount * 6,
    difficulty,
    nodeMap,
    cellMap,
    generatorSeed
  };
}

function carveGroupPath(
  width: number,
  height: number,
  difficulty: number,
  nodeCount: number,
  occupied: ReadonlySet<number>,
  rng: () => number
): number[] | undefined {
  const area = width * height;
  const pathBudget = Math.max(4, Math.floor((area * 0.68) / nodeCount));
  const distanceByDifficulty = Math.max(2, Math.floor((width + height) * (0.12 + difficulty * 0.07)));
  const densityPressure = Math.min(0.68, nodeCount / Math.max(1, area / 8));
  const minDistance = Math.min(Math.max(2, Math.floor(distanceByDifficulty * (1 - densityPressure))), Math.max(2, pathBudget - 2));
  const minLength = Math.min(pathBudget, minDistance + Math.ceil(difficulty * 0.8));
  const maxLength = Math.max(minLength, Math.floor(pathBudget * 1.35) + difficulty);
  const minTurns = pathBudget >= 12 ? Math.min(Math.max(1, difficulty - 2), Math.max(1, Math.floor(minLength / 5))) : 1;

  for (let attempt = 0; attempt < PATH_ATTEMPTS_PER_GROUP; attempt += 1) {
    const start = randomFreePoint(width, height, occupied, rng);
    const end = randomFreePoint(width, height, occupied, rng);
    if (!start || !end || samePoint(start, end) || manhattan(start, end) < minDistance) {
      continue;
    }

    const path = findOpenPath(width, height, start, end, occupied, rng);
    if (path === undefined) {
      continue;
    }
    const uniqueCells = new Set(path);
    if (uniqueCells.size !== path.length || path.some((cell) => occupied.has(cell))) {
      continue;
    }
    if (path.length < minLength || path.length > maxLength || countTurns(path, width) < minTurns) {
      continue;
    }
    return path;
  }

  return undefined;
}

function findOpenPath(
  width: number,
  height: number,
  start: Point,
  end: Point,
  occupied: ReadonlySet<number>,
  rng: () => number
): number[] | undefined {
  const startIndex = start.y * width + start.x;
  const endIndex = end.y * width + end.x;
  const queue: number[] = [startIndex];
  const visited = new Set<number>([startIndex]);
  const previous = new Map<number, number>();

  for (let cursor = 0; cursor < queue.length; cursor += 1) {
    const current = queue[cursor];
    if (current === endIndex) {
      return reconstructPath(current, previous);
    }

    const nextCells = neighbors(current, width, height);
    shuffle(nextCells, rng);
    for (const next of nextCells) {
      if (visited.has(next) || (next !== endIndex && occupied.has(next))) {
        continue;
      }
      visited.add(next);
      previous.set(next, current);
      queue.push(next);
    }
  }

  return undefined;
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

function addDifficultyCells(
  cellMap: number[],
  nodeMap: readonly number[],
  paths: readonly GeneratedPath[],
  width: number,
  height: number,
  difficulty: number,
  rng: () => number
): void {
  const protectedCells = new Set<number>();
  for (const path of paths) {
    for (const cell of path.cells) {
      protectedCells.add(cell);
      for (const neighbor of neighbors(cell, width, height)) {
        if (rng() < 0.32 + difficulty * 0.06) {
          protectedCells.add(neighbor);
        }
      }
    }
  }

  const candidates: number[] = [];
  for (let index = 0; index < cellMap.length; index += 1) {
    if (nodeMap[index] === 0 && !protectedCells.has(index)) {
      candidates.push(index);
    }
  }

  shuffle(candidates, rng);
  const obstacleBudget = Math.floor(width * height * (0.05 + difficulty * 0.055));
  for (let index = 0; index < Math.min(obstacleBudget, candidates.length); index += 1) {
    cellMap[candidates[index]] = difficulty >= 4 && index % 5 === 4 ? RESERVED_GIMMICK_CODE : OBSTACLE_CODE;
  }
}

function randomFreePoint(
  width: number,
  height: number,
  occupied: ReadonlySet<number>,
  rng: () => number
): Point | undefined {
  for (let attempt = 0; attempt < 80; attempt += 1) {
    const point = { x: randomInt(rng, width), y: randomInt(rng, height) };
    if (!occupied.has(point.y * width + point.x)) {
      return point;
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

function countTurns(path: readonly number[], width: number): number {
  let turns = 0;
  let previousDirection = "";
  for (let index = 1; index < path.length; index += 1) {
    const delta = path[index] - path[index - 1];
    const direction = delta === 1 || delta === -1 ? "h" : "v";
    if (previousDirection && previousDirection !== direction) {
      turns += 1;
    }
    previousDirection = direction;
  }
  return turns;
}

function assertGeneratorInteger(value: unknown, field: string): number {
  if (!Number.isInteger(value)) {
    throw new ValidationError([{ field, message: "must be an integer" }]);
  }
  return value as number;
}

function samePoint(a: Point, b: Point): boolean {
  return a.x === b.x && a.y === b.y;
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
