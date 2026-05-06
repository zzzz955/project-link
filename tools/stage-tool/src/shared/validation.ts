import {
  assertInteger,
  CellCode,
  MAX_NODE_GROUP_ID,
  Stage,
  StagePayload,
  ValidationError,
  ValidationIssue
} from "./stage";
import { normalizeMap } from "./codec";
import { validateStageSolvable } from "./solver";

const MIN_SIZE = 1;
const MAX_SIZE = 128;
const MIN_TIME_LIMIT = 0;
const MAX_TIME_LIMIT = 86400;
const MIN_DIFFICULTY = 1;
const MAX_DIFFICULTY = 5;
const MAX_CELL_CODE = 36 ** 2 - 1;

export function validateStageSequence(stageIds: readonly number[]): void {
  const issues: ValidationIssue[] = [];
  const sorted = [...stageIds].sort((a, b) => a - b);
  for (let index = 0; index < sorted.length; index += 1) {
    const expected = index + 1;
    if (sorted[index] !== expected) {
      issues.push({ field: "stageId", message: `sequence must be continuous 1..max; expected ${expected}` });
      break;
    }
  }
  throwIfIssues(issues);
}

export function validateCreateStageId(stageId: number, existingIds: readonly number[]): void {
  validateStageSequence(existingIds);
  const expected = existingIds.length === 0 ? 1 : Math.max(...existingIds) + 1;
  if (stageId !== expected) {
    throw new ValidationError([{ field: "stageId", message: `POST only allows ${expected}` }]);
  }
}

export function validateUpdateStageId(stageId: number, existingIds: readonly number[]): void {
  validateStageSequence(existingIds);
  if (!existingIds.includes(stageId)) {
    throw new ValidationError([{ field: "stageId", message: "PUT only allows existing stages" }]);
  }
}

export function validateDeleteStageId(stageId: number, existingIds: readonly number[]): void {
  validateStageSequence(existingIds);
  const last = existingIds.length === 0 ? undefined : Math.max(...existingIds);
  if (last === undefined || stageId !== last) {
    throw new ValidationError([{ field: "stageId", message: last === undefined ? "no stages exist" : `DELETE only allows last stage ${last}` }]);
  }
}

export function normalizeAndValidateStage(stageId: number, payload: StagePayload): Stage {
  const issues: ValidationIssue[] = [];

  assertPositiveId(stageId, "stageId", issues);
  const widthOk = assertInteger(payload.width, "width", issues);
  const heightOk = assertInteger(payload.height, "height", issues);
  const timeLimitOk = assertInteger(payload.timeLimit, "timeLimit", issues);
  const difficultyOk = assertInteger(payload.difficulty, "difficulty", issues);

  if (widthOk && (payload.width < MIN_SIZE || payload.width > MAX_SIZE)) {
    issues.push({ field: "width", message: `must be ${MIN_SIZE}..${MAX_SIZE}` });
  }
  if (heightOk && (payload.height < MIN_SIZE || payload.height > MAX_SIZE)) {
    issues.push({ field: "height", message: `must be ${MIN_SIZE}..${MAX_SIZE}` });
  }
  if (timeLimitOk && (payload.timeLimit < MIN_TIME_LIMIT || payload.timeLimit > MAX_TIME_LIMIT)) {
    issues.push({ field: "timeLimit", message: `must be ${MIN_TIME_LIMIT}..${MAX_TIME_LIMIT}` });
  }
  if (difficultyOk && (payload.difficulty < MIN_DIFFICULTY || payload.difficulty > MAX_DIFFICULTY)) {
    issues.push({ field: "difficulty", message: `must be ${MIN_DIFFICULTY}..${MAX_DIFFICULTY}` });
  }

  let nodeMap: number[] = [];
  let cellMapRaw: number[] = [];
  try {
    nodeMap = normalizeMap(payload.nodeMap, "nodeMap");
  } catch (error) {
    issues.push({ field: "nodeMap", message: errorMessage(error) });
  }
  try {
    cellMapRaw = normalizeMap(payload.cellMap, "cellMap");
  } catch (error) {
    issues.push({ field: "cellMap", message: errorMessage(error) });
  }

  const expectedLength = widthOk && heightOk ? payload.width * payload.height : undefined;
  if (expectedLength !== undefined) {
    if (nodeMap.length !== expectedLength) {
      issues.push({ field: "nodeMap", message: `length must equal width * height (${expectedLength})` });
    }
    if (cellMapRaw.length !== expectedLength) {
      issues.push({ field: "cellMap", message: `length must equal width * height (${expectedLength})` });
    }
  }

  const cellMap = cellMapRaw.map((value, index): CellCode => {
    if (!Number.isInteger(value) || value < 0 || value > MAX_CELL_CODE) {
      issues.push({ field: `cellMap[${index}]`, message: `must be 0..${MAX_CELL_CODE}` });
    }
    return value as CellCode;
  });

  const counts = new Map<number, number>();
  nodeMap.forEach((nodeGroupId, index) => {
    if (!Number.isInteger(nodeGroupId) || nodeGroupId < 0 || nodeGroupId > MAX_NODE_GROUP_ID) {
      issues.push({ field: `nodeMap[${index}]`, message: `must be 0..${MAX_NODE_GROUP_ID}` });
      return;
    }
    if (nodeGroupId > 0) {
      counts.set(nodeGroupId, (counts.get(nodeGroupId) ?? 0) + 1);
      if (cellMapRaw[index] > 0) {
        issues.push({ field: `nodeMap[${index}]`, message: "node cannot collide with non-empty cell" });
      }
    }
  });

  for (let id = 1; id <= MAX_NODE_GROUP_ID; id += 1) {
    const count = counts.get(id) ?? 0;
    if (count > 0 && count % 2 !== 0) {
      issues.push({ field: "nodeMap", message: `nodeGroupId ${id} must have an even count` });
    }
  }

  throwIfIssues(issues);

  const stage = {
    stageId,
    width: payload.width,
    height: payload.height,
    timeLimit: payload.timeLimit,
    difficulty: payload.difficulty,
    nodeMap,
    cellMap,
    generatorSeed: payload.generatorSeed
  };
  const solverResult = validateStageSolvable(stage);
  throwIfIssues(solverResult.issues);

  return stage;
}

function assertPositiveId(value: unknown, field: string, issues: ValidationIssue[]): value is number {
  if (!assertInteger(value, field, issues)) {
    return false;
  }
  if (value < 1) {
    issues.push({ field, message: "must be >= 1" });
    return false;
  }
  return true;
}

function throwIfIssues(issues: ValidationIssue[]): void {
  if (issues.length > 0) {
    throw new ValidationError(issues);
  }
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
