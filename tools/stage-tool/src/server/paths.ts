import { existsSync } from "fs";
import path from "path";

export interface DataPaths {
  stageCsv: string;
  nodeColorsCsv: string;
}

export function resolveDataPaths(): DataPaths {
  const explicitRoot = process.env.PROJECT_LINK_ROOT;
  const root = explicitRoot ? path.resolve(explicitRoot) : findProjectRoot();
  const ingameDir = path.join(root, "shared", "datas", "ingame");

  return {
    stageCsv: path.join(ingameDir, "ingame_stage.csv"),
    nodeColorsCsv: path.join(ingameDir, "ingame_node_colors.csv")
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

