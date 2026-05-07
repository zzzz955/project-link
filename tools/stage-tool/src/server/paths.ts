import { existsSync, readFileSync } from "fs";
import path from "path";

export interface DataPaths {
  stageCsv: string;
  nodeColorsCsv: string;
  projectRoot: string;
}

export interface EditorDefaults {
  width: number;
  height: number;
  timeLimit: number;
  difficulty: number;
}

export function resolveDataPaths(): DataPaths {
  const explicitRoot = process.env.PROJECT_LINK_ROOT;
  const root = explicitRoot ? path.resolve(explicitRoot) : findProjectRoot();
  const ingameDir = path.join(root, "shared", "datas", "ingame");

  return {
    stageCsv: path.join(ingameDir, "ingame_stage.csv"),
    nodeColorsCsv: path.join(ingameDir, "ingame_node_colors.csv"),
    projectRoot: root,
  };
}

export function resolveEditorDefaults(projectRoot: string): EditorDefaults {
  const iniPath = path.join(projectRoot, "template.ini");
  if (!existsSync(iniPath)) {
    return { width: 8, height: 8, timeLimit: 120, difficulty: 1 };
  }
  const ini = parseIni(readFileSync(iniPath, "utf-8"));
  const sec = ini["stage-editor"] ?? {};
  return {
    width: clampInt(sec["default_width"], 1, 40, 8),
    height: clampInt(sec["default_height"], 1, 40, 8),
    timeLimit: clampInt(sec["default_time"], 0, 99999, 120),
    difficulty: clampInt(sec["default_difficulty"], 1, 5, 1),
  };
}

function findProjectRoot(): string {
  const candidates = [
    process.cwd(),
    path.resolve(__dirname, "..", "..", "..", "..")
  ];

  for (const candidate of candidates) {
    if (existsSync(path.join(candidate, "shared", "datas", "ingame"))) {
      return candidate;
    }
  }

  return path.resolve(__dirname, "..", "..", "..", "..");
}

function parseIni(content: string): Record<string, Record<string, string>> {
  const cfg: Record<string, Record<string, string>> = {};
  let section = "_";
  for (const raw of content.split(/\r?\n/)) {
    const line = raw.trim();
    if (!line || line.startsWith(";") || line.startsWith("#")) continue;
    const sec = line.match(/^\[(.+)\]$/);
    if (sec) { section = sec[1]; cfg[section] = cfg[section] ?? {}; continue; }
    const eq = line.indexOf("=");
    if (eq > 0) {
      cfg[section] = cfg[section] ?? {};
      cfg[section][line.slice(0, eq).trim()] = line.slice(eq + 1).trim();
    }
  }
  return cfg;
}

function clampInt(value: string | undefined, min: number, max: number, fallback: number): number {
  const n = parseInt(value ?? "", 10);
  if (!Number.isInteger(n)) return fallback;
  return Math.min(max, Math.max(min, n));
}

