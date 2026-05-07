import React, { useCallback, useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

type StageBoardSize = {
  width: number;
  height: number;
};

type StageRecord = {
  stageId: string;
  boardSize: StageBoardSize;
  timeLimit: number;
  difficulty: number;
  nodeMap: number[][];
  cellMap: string[][];
  generatorSeed?: number;
};

type StageSummary = {
  stageId: string;
  difficulty?: string;
  boardSize?: StageBoardSize;
};

type NodeColor = {
  group: number;
  hex: string;
};

type ToolMode = "node" | "cell";

type CellCode = "empty" | "obstacle" | "reserved";

type ValidationError = {
  path?: string;
  message: string;
};

type GenerateSettings = {
  width: number;
  height: number;
  difficulty: number;
  nodeCount: number;
  seed: string;
};

type DragState = {
  action: "node-path" | "cell-paint" | "erase";
  nodeGroup: number;
  cells: number[];
};

type GeneratedStageResponse = Partial<StageRecord> & {
  payload?: unknown;
  stage?: unknown;
  data?: unknown;
  result?: unknown;
  generated?: unknown;
  seed?: string | number;
  generatedSeed?: string | number;
  generatorSeed?: string | number;
  metadata?: {
    seed?: string | number;
    generatedSeed?: string | number;
    generatorSeed?: string | number;
  };
};

const DEFAULT_WIDTH = 8;
const DEFAULT_HEIGHT = 8;
const DEFAULT_TIME_LIMIT = 120;
const DEFAULT_DIFFICULTY = 1;
const MAX_BOARD_EDGE = 40;

type EditorDefaults = {
  width: number;
  height: number;
  timeLimit: number;
  difficulty: number;
};
const MAX_NODE_GROUPS = 20;
const CELL_CODES: Array<{ code: CellCode; label: string; value: string }> = [
  { code: "empty", label: "Empty", value: "0" },
  { code: "obstacle", label: "Block", value: "obstacle" },
  { code: "reserved", label: "Reserve", value: "reserved" },
];

const FALLBACK_NODE_COLORS: NodeColor[] = Array.from({ length: 20 }, (_, index) => ({
  group: index + 1,
  hex: [
    "#e11d48",
    "#f97316",
    "#eab308",
    "#84cc16",
    "#22c55e",
    "#14b8a6",
    "#06b6d4",
    "#0ea5e9",
    "#3b82f6",
    "#6366f1",
    "#8b5cf6",
    "#a855f7",
    "#d946ef",
    "#ec4899",
    "#f43f5e",
    "#64748b",
    "#78716c",
    "#059669",
    "#0284c7",
    "#7c3aed",
  ][index],
}));

function emptyGrid<T>(width: number, height: number, value: T): T[][] {
  return Array.from({ length: height }, () => Array.from({ length: width }, () => value));
}

function createStage(stageId = ""): StageRecord {
  return {
    stageId,
    boardSize: { width: DEFAULT_WIDTH, height: DEFAULT_HEIGHT },
    timeLimit: 120,
    difficulty: 1,
    nodeMap: emptyGrid(DEFAULT_WIDTH, DEFAULT_HEIGHT, 0),
    cellMap: emptyGrid(DEFAULT_WIDTH, DEFAULT_HEIGHT, "0"),
  };
}

function clampSize(value: number): number {
  if (!Number.isFinite(value)) {
    return DEFAULT_WIDTH;
  }
  return Math.min(MAX_BOARD_EDGE, Math.max(1, Math.floor(value)));
}

function clampDifficulty(value: number): number {
  if (!Number.isFinite(value)) {
    return 1;
  }
  return Math.min(5, Math.max(1, Math.floor(value)));
}

function clampNodeCount(value: number): number {
  if (!Number.isFinite(value)) {
    return 4;
  }
  return Math.min(MAX_NODE_GROUPS, Math.max(1, Math.floor(value)));
}

function resizeGrid<T>(grid: T[][], width: number, height: number, fill: T): T[][] {
  return Array.from({ length: height }, (_, y) =>
    Array.from({ length: width }, (_, x) => grid[y]?.[x] ?? fill),
  );
}

function normalizeStage(raw: unknown, fallbackStageId = ""): StageRecord {
  const data = (raw ?? {}) as Partial<StageRecord> & {
    id?: string;
    width?: number;
    height?: number;
    stageId?: string | number;
    difficulty?: string | number;
    nodeMap?: unknown[] | string;
    cellMap?: unknown[] | string;
  };
  const width = clampSize(data.boardSize?.width ?? data.width ?? DEFAULT_WIDTH);
  const height = clampSize(data.boardSize?.height ?? data.height ?? DEFAULT_HEIGHT);
  const nodeMap = normalizeMapInput(data.nodeMap, width, height, 0).map((value) => Number(value) || 0);
  const cellMap = normalizeMapInput(data.cellMap, width, height, 0).map((value) => String(value ?? "0"));

  return {
    stageId: String(data.stageId ?? data.id ?? fallbackStageId),
    boardSize: { width, height },
    timeLimit: Number(data.timeLimit ?? 120),
    difficulty: Number(data.difficulty ?? 1),
    nodeMap: toRows(nodeMap, width, height, 0),
    cellMap: toRows(cellMap, width, height, "0"),
    generatorSeed: Number((data as { generatorSeed?: number }).generatorSeed ?? 0) || undefined,
  };
}

function normalizeMapInput(input: unknown, width: number, height: number, fill: number): unknown[] {
  const expected = width * height;
  if (typeof input === "string") {
    const values: number[] = [];
    for (let index = 0; index < input.length; index += 2) {
      values.push(parseInt(input.slice(index, index + 2), 36) || 0);
    }
    return [...values, ...Array(Math.max(0, expected - values.length)).fill(fill)].slice(0, expected);
  }
  if (!Array.isArray(input)) {
    return Array(expected).fill(fill);
  }
  if (Array.isArray(input[0])) {
    return input.flat();
  }
  return input;
}

function toRows<T>(values: T[], width: number, height: number, fill: T): T[][] {
  return Array.from({ length: height }, (_, y) =>
    Array.from({ length: width }, (_, x) => values[y * width + x] ?? fill),
  );
}

function normalizeStageList(raw: unknown): StageSummary[] {
  const list = Array.isArray(raw) ? raw : ((raw as { stages?: unknown[] })?.stages ?? []);
  return list
    .map((item) => {
      if (typeof item === "string") {
        return { stageId: item };
      }
      const row = item as Partial<StageSummary> & { id?: string };
      return {
        stageId: String(row.stageId ?? row.id ?? ""),
        difficulty: row.difficulty,
        boardSize: row.boardSize ?? ("width" in row && "height" in row
          ? { width: Number((row as { width: number }).width), height: Number((row as { height: number }).height) }
          : undefined),
      };
    })
    .filter((item) => item.stageId.length > 0);
}

function normalizeColors(raw: unknown): NodeColor[] {
  const payload = (raw as { colors?: unknown })?.colors ?? raw;
  if (Array.isArray(payload)) {
    return payload
      .map((item, index) => {
        if (typeof item === "string") {
          return { group: index + 1, hex: item };
        }
        const color = item as {
          group?: number;
          id?: number;
          nodeGroupId?: number;
          hex?: string;
          color?: string;
          hexColor?: string;
        };
        return {
          group: Number(color.group ?? color.nodeGroupId ?? color.id ?? index + 1),
          hex: String(color.hex ?? color.hexColor ?? color.color ?? FALLBACK_NODE_COLORS[index]?.hex ?? "#999999"),
        };
      })
      .filter((item) => item.group >= 1 && item.group <= 20)
      .slice(0, 20);
  }

  if (payload && typeof payload === "object") {
    return Object.entries(payload as Record<string, unknown>)
      .map(([group, hex]) => ({ group: Number(group), hex: String(hex) }))
      .filter((item) => item.group >= 1 && item.group <= 20)
      .sort((a, b) => a.group - b.group)
      .slice(0, 20);
  }

  return FALLBACK_NODE_COLORS;
}

async function readJson(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) {
    return undefined;
  }
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

async function requestJson<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
  });
  const payload = await readJson(response);
  if (!response.ok) {
    throw toApiError(payload, response.statusText);
  }
  return payload as T;
}

function toApiError(payload: unknown, fallback: string): Error & { validation?: ValidationError[] } {
  const error = new Error(fallback) as Error & { validation?: ValidationError[] };
  const data = payload as {
    message?: string;
    error?: string;
    errors?: Array<string | ValidationError>;
    validationErrors?: Array<string | ValidationError>;
    issues?: Array<string | { field?: string; path?: string; message: string }>;
  };
  error.message = String(data?.message ?? data?.error ?? fallback);
  const errors = normalizeValidationErrors(data);
  if (errors.length > 0) {
    error.validation = errors;
  }
  return error;
}

function normalizeValidationErrors(payload: unknown): ValidationError[] {
  const data = payload as {
    errors?: Array<string | ValidationError>;
    validationErrors?: Array<string | ValidationError>;
    issues?: Array<string | { field?: string; path?: string; message: string }>;
    solverIssues?: Array<string | { field?: string; path?: string; message: string }>;
    solver?: {
      errors?: Array<string | ValidationError>;
      issues?: Array<string | { field?: string; path?: string; message: string }>;
    };
  };
  const errors =
    data?.validationErrors ??
    data?.errors ??
    data?.issues ??
    data?.solverIssues ??
    data?.solver?.issues ??
    data?.solver?.errors;

  if (!Array.isArray(errors)) {
    return [];
  }

  return errors.map((item) =>
    typeof item === "string" ? { message: item } : { path: item.path ?? item.field, message: item.message },
  );
}

function flattenGrid<T>(grid: T[][]): T[] {
  return grid.flatMap((row) => row);
}

function encodeCellValue(value: string): number {
  if (value === "obstacle" || value === "1") {
    return 1;
  }
  if (value === "reserved" || value === "2") {
    return 2;
  }
  return Number(value) || 0;
}

function stagePayload(stage: StageRecord) {
  return {
    stageId: Number(stage.stageId),
    width: stage.boardSize.width,
    height: stage.boardSize.height,
    timeLimit: stage.timeLimit,
    difficulty: stage.difficulty,
    nodeMap: flattenGrid(stage.nodeMap),
    cellMap: flattenGrid(stage.cellMap).map(encodeCellValue),
    ...(stage.generatorSeed ? { generatorSeed: stage.generatorSeed } : {}),
  };
}

function toCellIndex(x: number, y: number, width: number): number {
  return y * width + x;
}

function toPoint(index: number, width: number): { x: number; y: number } {
  return { x: index % width, y: Math.floor(index / width) };
}

function segmentIndices(from: number, to: number, width: number): number[] {
  const start = toPoint(from, width);
  const end = toPoint(to, width);
  const cells: number[] = [];
  let x = start.x;
  let y = start.y;
  const push = () => cells.push(toCellIndex(x, y, width));
  push();
  while (x !== end.x) {
    x += Math.sign(end.x - x);
    push();
  }
  while (y !== end.y) {
    y += Math.sign(end.y - y);
    push();
  }
  return cells;
}

function appendDragCells(current: DragState, nextIndex: number, width: number): DragState {
  const previous = current.cells[current.cells.length - 1];
  const nextCells = previous === undefined ? [nextIndex] : segmentIndices(previous, nextIndex, width);
  const merged = [...current.cells];
  for (const cell of nextCells) {
    if (merged[merged.length - 1] !== cell) {
      merged.push(cell);
    }
  }
  return { ...current, cells: merged };
}

function hexToRgba(hex: string | undefined, alpha: number): string | undefined {
  if (!hex || !/^#[0-9a-f]{6}$/i.test(hex)) {
    return undefined;
  }
  const red = parseInt(hex.slice(1, 3), 16);
  const green = parseInt(hex.slice(3, 5), 16);
  const blue = parseInt(hex.slice(5, 7), 16);
  return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}

function normalizeGeneratedStage(raw: unknown, fallbackStageId: string): { stage: StageRecord; seed: string } {
  const response = (raw ?? {}) as GeneratedStageResponse;
  const board = response.stage ?? response.payload ?? response.data ?? response.result ?? response.generated ?? response;
  const seed = response.generatedSeed ?? response.seed ?? response.generatorSeed ?? response.metadata?.generatedSeed ?? response.metadata?.seed ?? response.metadata?.generatorSeed ?? "";
  return {
    stage: normalizeStage(board, fallbackStageId),
    seed: seed === "" || seed === undefined ? "" : String(seed),
  };
}

function App() {
  const [editorDefaults, setEditorDefaults] = useState<EditorDefaults>({
    width: DEFAULT_WIDTH,
    height: DEFAULT_HEIGHT,
    timeLimit: DEFAULT_TIME_LIMIT,
    difficulty: DEFAULT_DIFFICULTY,
  });
  const [stages, setStages] = useState<StageSummary[]>([]);
  const [stage, setStage] = useState<StageRecord>(() => createStage());
  const [selectedStageId, setSelectedStageId] = useState("");
  const [draftStageId, setDraftStageId] = useState("");
  const [generateSettings, setGenerateSettings] = useState<GenerateSettings>({
    width: DEFAULT_WIDTH,
    height: DEFAULT_HEIGHT,
    difficulty: 1,
    nodeCount: 4,
    seed: "",
  });
  const [generatedSeed, setGeneratedSeed] = useState("");
  const [nodeColors, setNodeColors] = useState<NodeColor[]>(FALLBACK_NODE_COLORS);
  const [mode, setMode] = useState<ToolMode>("node");
  const [selectedNode, setSelectedNode] = useState(1);
  const [selectedCell, setSelectedCell] = useState<CellCode>("empty");
  const [dragState, setDragState] = useState<DragState | undefined>();
  const [validationErrors, setValidationErrors] = useState<ValidationError[]>([]);
  const [status, setStatus] = useState("");
  const [isBusy, setBusy] = useState(false);

  const colorByGroup = useMemo(() => {
    const map = new Map<number, string>();
    for (const color of nodeColors) {
      map.set(color.group, color.hex);
    }
    return map;
  }, [nodeColors]);

  const previewCells = useMemo(() => new Set(dragState?.cells ?? []), [dragState]);

  const loadStages = useCallback(async () => {
    const payload = await requestJson<unknown>("/api/stages");
    setStages(normalizeStageList(payload));
  }, []);

  const loadStage = useCallback(async (stageId: string) => {
    setBusy(true);
    setStatus("");
    setValidationErrors([]);
    try {
      const payload = await requestJson<unknown>(`/api/stages/${encodeURIComponent(stageId)}`);
      const nextStage = normalizeStage(payload, stageId);
      setStage(nextStage);
      setSelectedStageId(nextStage.stageId);
      setDraftStageId(nextStage.stageId);
      setGeneratedSeed("");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Load failed");
    } finally {
      setBusy(false);
    }
  }, []);

  useEffect(() => {
    requestJson<EditorDefaults>("/api/defaults")
      .then((data) => setEditorDefaults(data))
      .catch(() => {});
    requestJson<unknown>("/api/node-colors")
      .then((payload) => setNodeColors(normalizeColors(payload)))
      .catch(() => setNodeColors(FALLBACK_NODE_COLORS));
    loadStages().catch((error) => setStatus(error instanceof Error ? error.message : "List failed"));
  }, [loadStages]);

  const updateStage = (updater: (current: StageRecord) => StageRecord) => {
    setStage((current) => updater(current));
    setValidationErrors([]);
  };

  const setBoardSize = (axis: keyof StageBoardSize, value: number) => {
    updateStage((current) => {
      const size = {
        ...current.boardSize,
        [axis]: clampSize(value),
      };
      return {
        ...current,
        boardSize: size,
        nodeMap: resizeGrid(current.nodeMap, size.width, size.height, 0),
        cellMap: resizeGrid(current.cellMap, size.width, size.height, "0"),
      };
    });
  };

  const paintCell = (x: number, y: number) => {
    updateStage((current) => {
      if (mode === "node") {
        const nodeMap = current.nodeMap.map((row, rowIndex) =>
          rowIndex === y ? row.map((cell, colIndex) => (colIndex === x ? selectedNode : cell)) : row,
        );
        return { ...current, nodeMap };
      }

      const value = CELL_CODES.find((cell) => cell.code === selectedCell)?.value ?? "0";
      const cellMap = current.cellMap.map((row, rowIndex) =>
        rowIndex === y ? row.map((cell, colIndex) => (colIndex === x ? value : cell)) : row,
      );
      return { ...current, cellMap };
    });
  };

  const clearCells = (indices: readonly number[]) => {
    updateStage((current) => ({
      ...current,
      nodeMap: current.nodeMap.map((row, rowIndex) =>
        row.map((cell, colIndex) => (indices.includes(toCellIndex(colIndex, rowIndex, current.boardSize.width)) ? 0 : cell)),
      ),
      cellMap: current.cellMap.map((row, rowIndex) =>
        row.map((cell, colIndex) => (indices.includes(toCellIndex(colIndex, rowIndex, current.boardSize.width)) ? "0" : cell)),
      ),
    }));
  };

  const clearCell = (x: number, y: number) => {
    clearCells([toCellIndex(x, y, stage.boardSize.width)]);
  };

  const beginCellInteraction = (event: React.PointerEvent<HTMLButtonElement>, x: number, y: number) => {
    event.preventDefault();
    const index = toCellIndex(x, y, stage.boardSize.width);

    if (event.button === 2) {
      clearCell(x, y);
      setDragState({ action: "erase", nodeGroup: 0, cells: [index] });
      return;
    }

    if (event.button !== 0) {
      return;
    }

    paintCell(x, y);
    if (mode === "node" && selectedNode > 0) {
      setDragState({ action: "node-path", nodeGroup: selectedNode, cells: [index] });
      return;
    }
    setDragState({ action: "cell-paint", nodeGroup: 0, cells: [index] });
  };

  const continueCellInteraction = (x: number, y: number) => {
    if (!dragState) {
      return;
    }

    const index = toCellIndex(x, y, stage.boardSize.width);
    const nextDragState = appendDragCells(dragState, index, stage.boardSize.width);
    setDragState(nextDragState);

    if (dragState.action === "erase") {
      clearCells(nextDragState.cells);
    } else if (dragState.action === "cell-paint") {
      paintCell(x, y);
    }
  };

  const finishCellInteraction = (x: number, y: number) => {
    if (!dragState) {
      return;
    }

    if (dragState.action === "node-path") {
      paintCell(x, y);
    } else if (dragState.action === "erase") {
      clearCell(x, y);
    } else {
      paintCell(x, y);
    }
    setDragState(undefined);
  };

  const newStage = () => {
    const nextId = stages.reduce((max, item) => Math.max(max, Number(item.stageId) || 0), 0) + 1;
    const { width, height, timeLimit, difficulty } = editorDefaults;
    const next: StageRecord = {
      stageId: String(nextId),
      boardSize: { width, height },
      timeLimit,
      difficulty,
      nodeMap: emptyGrid(width, height, 0),
      cellMap: emptyGrid(width, height, "0"),
    };
    setStage(next);
    setSelectedStageId("");
    setDraftStageId(next.stageId);
    setGeneratedSeed("");
    setValidationErrors([]);
    setStatus("");
  };

  const validateBeforeSave = async (current: StageRecord): Promise<boolean> => {
    try {
      const payload = await requestJson<unknown>(
        `/api/stages/${encodeURIComponent(current.stageId)}/validate`,
        {
          method: "POST",
          body: JSON.stringify(stagePayload(current)),
        },
      );
      const errors = normalizeValidationErrors(payload);
      setValidationErrors(errors);
      const valid = (payload as { valid?: boolean; ok?: boolean }).valid ?? (payload as { ok?: boolean }).ok;
      if (errors.length === 0 && valid === false) {
        setValidationErrors([{ message: "Solver validation failed" }]);
        return false;
      }
      return errors.length === 0;
    } catch (error) {
      const apiError = error as Error & { validation?: ValidationError[] };
      if (apiError.validation?.length) {
        setValidationErrors(apiError.validation);
        return false;
      }
      setValidationErrors([{ message: apiError.message || "Validation failed" }]);
      return false;
    }
  };

  const generateStage = async () => {
    setBusy(true);
    setStatus("");
    setValidationErrors([]);
    try {
      const seed = generateSettings.seed.trim();
      const payload = await requestJson<unknown>("/api/stages/generate", {
        method: "POST",
        body: JSON.stringify({
          width: clampSize(generateSettings.width),
          height: clampSize(generateSettings.height),
          difficulty: clampDifficulty(generateSettings.difficulty),
          nodeCount: clampNodeCount(generateSettings.nodeCount),
          ...(seed ? { seed } : {}),
        }),
      });
      const next = normalizeGeneratedStage(payload, draftStageId);
      setStage(next.stage);
      setSelectedStageId("");
      setDraftStageId(next.stage.stageId);
      setGeneratedSeed(next.seed);
      setStatus("Generated");
    } catch (error) {
      const apiError = error as Error & { validation?: ValidationError[] };
      setValidationErrors(apiError.validation ?? [{ message: apiError.message || "Generate failed" }]);
      setStatus("Generate failed");
    } finally {
      setBusy(false);
    }
  };

  const saveStage = async () => {
    const current = normalizeStage({ ...stage, stageId: draftStageId }, draftStageId);
    if (!Number.isInteger(Number(current.stageId)) || Number(current.stageId) < 1) {
      setValidationErrors([{ path: "stageId", message: "must be an integer >= 1" }]);
      return;
    }

    setBusy(true);
    setStatus("");
    try {
      const canSave = await validateBeforeSave(current);
      if (!canSave) {
        setStatus("Validation failed");
        return;
      }

      try {
        await requestJson(`/api/stages/${encodeURIComponent(current.stageId)}`, {
          method: selectedStageId ? "PUT" : "POST",
          body: JSON.stringify(stagePayload(current)),
        });
      } catch (error) {
        if (!selectedStageId) {
          await requestJson(`/api/stages`, {
            method: "POST",
            body: JSON.stringify(stagePayload(current)),
          });
        } else {
          throw error;
        }
      }

      setStage(current);
      setSelectedStageId(current.stageId);
      setGeneratedSeed("");
      await loadStages();
      setStatus("Saved");
    } catch (error) {
      const apiError = error as Error & { validation?: ValidationError[] };
      setValidationErrors(apiError.validation ?? [{ message: apiError.message || "Save failed" }]);
      setStatus("Save failed");
    } finally {
      setBusy(false);
    }
  };

  const deleteStage = async () => {
    const stageId = selectedStageId || draftStageId;
    if (!stageId) {
      return;
    }
    setBusy(true);
    setStatus("");
    setValidationErrors([]);
    try {
      await requestJson(`/api/stages/${encodeURIComponent(stageId)}`, { method: "DELETE" });
      await loadStages();
      const next = createStage();
      setStage(next);
      setSelectedStageId("");
      setDraftStageId("");
      setGeneratedSeed("");
      setStatus("Deleted");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Delete failed");
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="app-shell">
      <section className="stage-list" aria-label="Stages">
        <div className="panel-title">
          <h1>Stage Tool</h1>
          <button type="button" onClick={newStage} disabled={isBusy}>
            Add
          </button>
        </div>
        <div className="list-scroll">
          {[...stages].sort((a, b) => Number(b.stageId) - Number(a.stageId)).map((item) => (
            <button
              type="button"
              key={item.stageId}
              className={item.stageId === selectedStageId ? "stage-row selected" : "stage-row"}
              onClick={() => loadStage(item.stageId)}
              disabled={isBusy}
            >
              <span>{item.stageId}</span>
              <small>{item.boardSize ? `${item.boardSize.width}x${item.boardSize.height}` : item.difficulty}</small>
            </button>
          ))}
        </div>
      </section>

      <section className="editor" aria-label="Stage editor">
        <div className="toolbar">
          <label>
            <span>stageId</span>
            <input
              type="number"
              min={1}
              value={draftStageId}
              onChange={(event) => setDraftStageId(event.target.value)}
            />
          </label>
          <label>
            <span>Width</span>
            <input
              type="number"
              min={1}
              max={MAX_BOARD_EDGE}
              value={stage.boardSize.width}
              onChange={(event) => setBoardSize("width", Number(event.target.value))}
            />
          </label>
          <label>
            <span>Height</span>
            <input
              type="number"
              min={1}
              max={MAX_BOARD_EDGE}
              value={stage.boardSize.height}
              onChange={(event) => setBoardSize("height", Number(event.target.value))}
            />
          </label>
          <label>
            <span>Time</span>
            <input
              type="number"
              min={0}
              value={stage.timeLimit}
              onChange={(event) =>
                updateStage((current) => ({ ...current, timeLimit: Math.max(0, Number(event.target.value) || 0) }))
              }
            />
          </label>
          <label>
            <span>Difficulty</span>
            <input
              type="number"
              min={1}
              max={5}
              value={stage.difficulty}
              onChange={(event) =>
                updateStage((current) => ({ ...current, difficulty: clampDifficulty(Number(event.target.value)) }))
              }
            />
          </label>
          <div className="toolbar-actions">
            <button type="button" onClick={saveStage} disabled={isBusy}>
              Save
            </button>
            <button type="button" className="danger" onClick={deleteStage} disabled={isBusy || !draftStageId}>
              Delete
            </button>
          </div>
        </div>

        <div className="generate-bar" aria-label="Stage generator">
          <label>
            <span>Gen W</span>
            <input
              type="number"
              min={1}
              max={MAX_BOARD_EDGE}
              value={generateSettings.width}
              onChange={(event) =>
                setGenerateSettings((current) => ({ ...current, width: clampSize(Number(event.target.value)) }))
              }
            />
          </label>
          <label>
            <span>Gen H</span>
            <input
              type="number"
              min={1}
              max={MAX_BOARD_EDGE}
              value={generateSettings.height}
              onChange={(event) =>
                setGenerateSettings((current) => ({ ...current, height: clampSize(Number(event.target.value)) }))
              }
            />
          </label>
          <label>
            <span>Gen Diff</span>
            <input
              type="number"
              min={1}
              max={5}
              value={generateSettings.difficulty}
              onChange={(event) =>
                setGenerateSettings((current) => ({
                  ...current,
                  difficulty: clampDifficulty(Number(event.target.value)),
                }))
              }
            />
          </label>
          <label>
            <span>Gen Nodes</span>
            <input
              type="number"
              min={1}
              max={MAX_NODE_GROUPS}
              value={generateSettings.nodeCount}
              onChange={(event) =>
                setGenerateSettings((current) => ({
                  ...current,
                  nodeCount: clampNodeCount(Number(event.target.value)),
                }))
              }
            />
          </label>
          <label>
            <span>Seed</span>
            <input
              type="text"
              value={generateSettings.seed}
              onChange={(event) => setGenerateSettings((current) => ({ ...current, seed: event.target.value }))}
            />
          </label>
          <button type="button" onClick={generateStage} disabled={isBusy}>
            Generate
          </button>
          <div className="metadata" aria-label="Generated metadata">
            <span>Generated seed</span>
            <strong>{generatedSeed || "-"}</strong>
          </div>
        </div>

        <div className="editor-scroll">
        {validationErrors.length > 0 && (
          <div className="errors" role="alert">
            {validationErrors.map((error, index) => (
              <div key={`${error.path ?? "error"}-${index}`}>
                <b>{error.path ?? "stage"}</b>
                <span>{error.message}</span>
              </div>
            ))}
          </div>
        )}

        {status && <div className="status">{status}</div>}

        <div className="workbench">
          <aside className="palette" aria-label="Palette">
            <div className="mode-switch">
              <button type="button" className={mode === "node" ? "active" : ""} onClick={() => setMode("node")}>
                Nodes
              </button>
              <button type="button" className={mode === "cell" ? "active" : ""} onClick={() => setMode("cell")}>
                Cells
              </button>
            </div>

            {mode === "node" ? (
              <div className="node-palette">
                <button
                  type="button"
                  className={selectedNode === 0 ? "swatch selected" : "swatch"}
                  onClick={() => setSelectedNode(0)}
                  title="Erase node"
                >
                  0
                </button>
                {nodeColors.map((color) => (
                  <button
                    type="button"
                    key={color.group}
                    className={selectedNode === color.group ? "swatch selected" : "swatch"}
                    style={{ backgroundColor: color.hex }}
                    onClick={() => setSelectedNode(color.group)}
                    title={`Node ${color.group}`}
                  >
                    {color.group}
                  </button>
                ))}
              </div>
            ) : (
              <div className="cell-palette">
                {CELL_CODES.map((cell) => (
                  <button
                    type="button"
                    key={cell.code}
                    className={selectedCell === cell.code ? `cell-chip ${cell.code} selected` : `cell-chip ${cell.code}`}
                    onClick={() => setSelectedCell(cell.code)}
                  >
                    {cell.label}
                  </button>
                ))}
              </div>
            )}
          </aside>

          <div
            className="board"
            style={{
              gridTemplateColumns: `repeat(${stage.boardSize.width}, minmax(22px, 1fr))`,
              maxWidth: `${stage.boardSize.width * 42}px`,
            }}
          >
            {stage.nodeMap.map((row, y) =>
              row.map((nodeGroup, x) => {
                const index = toCellIndex(x, y, stage.boardSize.width);
                const cellCode = stage.cellMap[y]?.[x] ?? "0";
                const nodeColor = nodeGroup > 0 ? colorByGroup.get(nodeGroup) : undefined;
                const previewColor =
                  dragState?.action === "node-path" && previewCells.has(index)
                    ? hexToRgba(colorByGroup.get(dragState.nodeGroup), 0.24)
                    : undefined;
                const cellClass =
                  cellCode === "obstacle" || cellCode === "1"
                    ? "obstacle"
                    : cellCode === "reserved" || cellCode === "2"
                      ? "reserved"
                      : "";
                return (
                  <button
                    type="button"
                    key={`${x}-${y}`}
                    className={`grid-cell ${cellClass}${previewColor ? " path-preview" : ""}`}
                    style={{ backgroundColor: nodeColor ?? previewColor ?? undefined }}
                    onPointerDown={(event) => beginCellInteraction(event, x, y)}
                    onPointerEnter={() => continueCellInteraction(x, y)}
                    onPointerUp={() => finishCellInteraction(x, y)}
                    onPointerCancel={() => setDragState(undefined)}
                    onContextMenu={(event) => {
                      event.preventDefault();
                    }}
                    title={`${x},${y}`}
                  >
                    <span>{nodeGroup || ""}</span>
                  </button>
                );
              }),
            )}
          </div>
        </div>
        </div>
      </section>
    </main>
  );
}

createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
