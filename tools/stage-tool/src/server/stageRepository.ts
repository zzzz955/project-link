import { promises as fs } from "fs";
import {
  encodeFixedBase36,
  NodeColor,
  Stage,
  StagePayload,
  validateCreateStageId,
  validateDeleteStageId,
  validateStageSequence,
  validateUpdateStageId,
  normalizeAndValidateStage
} from "../shared";
import { CsvTable, readCsvTable, writeCsvTableAtomic } from "./csv";

const BOARD_ENCODING = "b36w2-rm-v1";
const STAGE_COLUMNS = ["stageId", "width", "height", "timeLimit", "difficulty", "boardEncoding", "nodeMap", "cellMap", "stageMeta", "generatorSeed"];
const STAGE_METADATA: string[][] = [
  STAGE_COLUMNS,
  ["CS", "CS", "CS", "CS", "CS", "CS", "CS", "CS", "CS", "CS"],
  ["int32", "int32", "int32", "int32", "int32", "string(32)", "string", "string", "string", "uint32"],
  ["PK", "NN", "NN", "NN", "NN", "NN", "NN", "NN", "NN", "NN"]
];

const NODE_COLOR_COLUMNS = ["nodeGroupId", "hexColor", "displayName"];
const NODE_COLOR_METADATA: string[][] = [
  NODE_COLOR_COLUMNS,
  ["CS", "CS", "CS"],
  ["int32", "string(16)", "string(32)"],
  ["PK", "NN", "NN"]
];

export class StageRepository {
  constructor(
    private readonly stageCsvPath: string,
    private readonly nodeColorsCsvPath: string
  ) {}

  async listStages(): Promise<Stage[]> {
    const table = await this.readStageTable();
    const stages = table.records.map(recordToStage).sort((a, b) => a.stageId - b.stageId);
    validateStageSequence(stages.map((stage) => stage.stageId));
    return stages;
  }

  async getStage(stageId: number): Promise<Stage | undefined> {
    return (await this.listStages()).find((stage) => stage.stageId === stageId);
  }

  async createStage(stageId: number, payload: StagePayload): Promise<Stage> {
    const table = await this.readStageTable();
    const existingIds = table.records.map((record) => parseInt(record.stageId, 10));
    validateCreateStageId(stageId, existingIds);
    const stage = normalizeAndValidateStage(stageId, payload);
    table.records.push(stageToRecord(stage));
    await writeCsvTableAtomic(this.stageCsvPath, table);
    return stage;
  }

  async updateStage(stageId: number, payload: StagePayload): Promise<Stage> {
    const table = await this.readStageTable();
    const existingIds = table.records.map((record) => parseInt(record.stageId, 10));
    validateUpdateStageId(stageId, existingIds);
    const stage = normalizeAndValidateStage(stageId, payload);
    table.records = table.records.map((record) => parseInt(record.stageId, 10) === stageId ? stageToRecord(stage, record) : record);
    await writeCsvTableAtomic(this.stageCsvPath, table);
    return stage;
  }

  async deleteStage(stageId: number): Promise<void> {
    const table = await this.readStageTable();
    const existingIds = table.records.map((record) => parseInt(record.stageId, 10));
    validateDeleteStageId(stageId, existingIds);
    table.records = table.records.filter((record) => parseInt(record.stageId, 10) !== stageId);
    await writeCsvTableAtomic(this.stageCsvPath, table);
  }

  validateStage(stageId: number, payload: StagePayload): { valid: true; issues: [] } {
    normalizeAndValidateStage(stageId, payload);
    return { valid: true, issues: [] };
  }

  async listNodeColors(): Promise<NodeColor[]> {
    await ensureCsv(this.nodeColorsCsvPath, NODE_COLOR_METADATA);
    const table = await readCsvTable(this.nodeColorsCsvPath);
    return table.records.map((record) => ({
      nodeGroupId: parseInt(record.nodeGroupId, 10),
      hexColor: record.hexColor || record.color,
      displayName: record.displayName || undefined
    })).filter((color) => Number.isInteger(color.nodeGroupId) && color.hexColor.length > 0)
      .sort((a, b) => a.nodeGroupId - b.nodeGroupId);
  }

  private async readStageTable(): Promise<CsvTable> {
    await ensureCsv(this.stageCsvPath, STAGE_METADATA);
    const table = await readCsvTable(this.stageCsvPath);
    const header = table.metadataRows[0];
    for (const column of ["stageId", "width", "height", "timeLimit", "difficulty", "nodeMap", "cellMap"]) {
      if (!header.includes(column)) {
        throw new Error(`${this.stageCsvPath} missing required column ${column}`);
      }
    }
    return table;
  }
}

async function ensureCsv(filePath: string, metadataRows: string[][]): Promise<void> {
  try {
    await fs.access(filePath);
  } catch (error) {
    const missing = error as NodeJS.ErrnoException;
    if (missing.code !== "ENOENT") {
      throw error;
    }
    await writeCsvTableAtomic(filePath, { metadataRows, records: [] });
  }
}

function recordToStage(record: Record<string, string>): Stage {
  if (record.boardEncoding && record.boardEncoding !== BOARD_ENCODING) {
    throw new Error(`unsupported boardEncoding ${record.boardEncoding}`);
  }
  return normalizeAndValidateStage(parseInt(record.stageId, 10), {
    width: parseInt(record.width, 10),
    height: parseInt(record.height, 10),
    timeLimit: parseInt(record.timeLimit, 10),
    difficulty: parseInt(record.difficulty, 10),
    nodeMap: record.nodeMap,
    cellMap: record.cellMap,
    generatorSeed: parseInt(record.generatorSeed || "0", 10)
  });
}

function stageToRecord(stage: Stage, existing: Record<string, string> = {}): Record<string, string> {
  return {
    ...existing,
    stageId: String(stage.stageId),
    width: String(stage.width),
    height: String(stage.height),
    timeLimit: String(stage.timeLimit),
    difficulty: String(stage.difficulty),
    boardEncoding: existing.boardEncoding || BOARD_ENCODING,
    nodeMap: encodeFixedBase36(stage.nodeMap),
    cellMap: encodeFixedBase36(stage.cellMap),
    stageMeta: existing.stageMeta || "{}",
    generatorSeed: stage.generatorSeed === undefined ? (existing.generatorSeed || "0") : String(stage.generatorSeed)
  };
}
