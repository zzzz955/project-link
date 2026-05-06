import { MAX_NODE_GROUP_ID, StagePayload, ValidationError } from "./stage";
import { normalizeAndValidateStage } from "./validation";

export interface GenerateStageInput {
  width: number;
  height: number;
  difficulty: number;
  seed?: number | string;
}

export interface GeneratedStage {
  payload: StagePayload;
  seed: string;
  generatorSeed: number;
  validation: { valid: true; issues: [] };
}

const MIN_GENERATOR_SIZE = 3;
const MIN_DIFFICULTY = 1;
const MAX_DIFFICULTY = 5;
const OBSTACLE_CODE = 1;

export function generateStagePayload(input: GenerateStageInput): GeneratedStage {
  const width = assertGeneratorInteger(input.width, "width");
  const height = assertGeneratorInteger(input.height, "height");
  const difficulty = assertGeneratorInteger(input.difficulty, "difficulty");
  const issues = [];

  if (width < MIN_GENERATOR_SIZE) issues.push({ field: "width", message: `must be >= ${MIN_GENERATOR_SIZE}` });
  if (height < MIN_GENERATOR_SIZE) issues.push({ field: "height", message: `must be >= ${MIN_GENERATOR_SIZE}` });
  if (difficulty < MIN_DIFFICULTY || difficulty > MAX_DIFFICULTY) {
    issues.push({ field: "difficulty", message: `must be ${MIN_DIFFICULTY}..${MAX_DIFFICULTY}` });
  }
  if (issues.length > 0) {
    throw new ValidationError(issues);
  }

  const seed = input.seed === undefined ? defaultSeed(width, height, difficulty) : String(input.seed);
  const generatorSeed = hashSeed(seed);
  const rng = createRng(seed);
  const area = width * height;
  const maxByArea = Math.max(1, Math.floor(area / 6));
  const targetGroups = Math.min(MAX_NODE_GROUP_ID, maxByArea, 2 + difficulty * 2);
  const nodeMap = new Array<number>(area).fill(0);
  const cellMap = new Array<number>(area).fill(0);
  const protectedCells = new Set<number>();
  const lanes = shuffledInteriorRows(height, rng).slice(0, targetGroups);
  const groupCount = lanes.length;

  for (let group = 1; group <= groupCount; group += 1) {
    const y = lanes[group - 1];
    const left = 1 + randomInt(rng, Math.max(1, Math.floor(width / 4)));
    const rightLimit = Math.max(left + 1, width - 2);
    const right = rightLimit - randomInt(rng, Math.max(1, Math.floor(width / 4)));
    const startX = Math.min(left, right - 1);
    const endX = Math.max(startX + 1, right);
    const start = y * width + startX;
    const end = y * width + endX;
    nodeMap[start] = group;
    nodeMap[end] = group;
    for (let x = startX; x <= endX; x += 1) {
      protectedCells.add(y * width + x);
    }
  }

  const obstacleBudget = Math.floor(area * (0.04 + difficulty * 0.035));
  const candidates: number[] = [];
  for (let index = 0; index < area; index += 1) {
    if (nodeMap[index] === 0 && !protectedCells.has(index)) {
      candidates.push(index);
    }
  }
  shuffle(candidates, rng);
  for (const index of candidates.slice(0, obstacleBudget)) {
    cellMap[index] = OBSTACLE_CODE;
  }

  const payload: StagePayload = {
    width,
    height,
    timeLimit: 45 + difficulty * 15 + groupCount * 5,
    difficulty,
    nodeMap,
    cellMap,
    generatorSeed
  };
  normalizeAndValidateStage(1, payload);
  return { payload, seed, generatorSeed, validation: { valid: true, issues: [] } };
}

function assertGeneratorInteger(value: unknown, field: string): number {
  if (!Number.isInteger(value)) {
    throw new ValidationError([{ field, message: "must be an integer" }]);
  }
  return value as number;
}

function shuffledInteriorRows(height: number, rng: () => number): number[] {
  const rows: number[] = [];
  for (let y = 1; y < height - 1; y += 1) {
    rows.push(y);
  }
  shuffle(rows, rng);
  return rows;
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

function defaultSeed(width: number, height: number, difficulty: number): string {
  return `${width}x${height}-d${difficulty}`;
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
