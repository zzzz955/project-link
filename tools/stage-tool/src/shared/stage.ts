export const MAX_NODE_GROUP_ID = 20;
export const ENCODED_CELL_WIDTH = 2;

export type CellCode = number;

export interface Stage {
  stageId: number;
  width: number;
  height: number;
  timeLimit: number;
  difficulty: number;
  nodeMap: number[];
  cellMap: CellCode[];
  generatorSeed?: number;
}

export interface StagePayload {
  width: number;
  height: number;
  timeLimit: number;
  difficulty: number;
  nodeMap: number[] | string;
  cellMap: CellCode[] | number[] | string;
  generatorSeed?: number;
}

export interface NodeColor {
  nodeGroupId: number;
  hexColor: string;
  displayName?: string;
}

export interface ValidationIssue {
  field: string;
  message: string;
}

export class ValidationError extends Error {
  readonly issues: ValidationIssue[];

  constructor(issues: ValidationIssue[]) {
    super(issues.map((issue) => `${issue.field}: ${issue.message}`).join("; "));
    this.name = "ValidationError";
    this.issues = issues;
  }
}

export function assertInteger(value: unknown, field: string, issues: ValidationIssue[]): value is number {
  if (!Number.isInteger(value)) {
    issues.push({ field, message: "must be an integer" });
    return false;
  }
  return true;
}
