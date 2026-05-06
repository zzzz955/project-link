import { promises as fs } from "fs";
import path from "path";

export interface CsvTable {
  metadataRows: string[][];
  records: Record<string, string>[];
}

export async function readCsvTable(filePath: string): Promise<CsvTable> {
  const raw = await fs.readFile(filePath, "utf8");
  const rows = parseCsv(raw);
  if (rows.length < 4) {
    throw new Error(`${filePath} must contain 4 metadata rows`);
  }

  const header = rows[0];
  const records = rows.slice(4).filter((row) => row.some((cell) => cell.length > 0)).map((row) => {
    const record: Record<string, string> = {};
    header.forEach((name, index) => {
      record[name] = row[index] ?? "";
    });
    return record;
  });

  return {
    metadataRows: rows.slice(0, 4),
    records
  };
}

export async function writeCsvTableAtomic(filePath: string, table: CsvTable): Promise<void> {
  const header = table.metadataRows[0];
  const rows = [
    ...table.metadataRows,
    ...table.records.map((record) => header.map((name) => record[name] ?? ""))
  ];
  const data = `${rows.map(formatCsvRow).join("\n")}\n`;
  const tempPath = path.join(path.dirname(filePath), `.${path.basename(filePath)}.${process.pid}.${Date.now()}.tmp`);

  await fs.writeFile(tempPath, data, "utf8");
  try {
    await fs.rename(tempPath, filePath);
  } catch (error) {
    await fs.unlink(tempPath).catch(() => undefined);
    throw error;
  }
}

function parseCsv(input: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let cell = "";
  let quoted = false;

  for (let index = 0; index < input.length; index += 1) {
    const char = input[index];
    const next = input[index + 1];

    if (quoted) {
      if (char === "\"" && next === "\"") {
        cell += "\"";
        index += 1;
      } else if (char === "\"") {
        quoted = false;
      } else {
        cell += char;
      }
      continue;
    }

    if (char === "\"") {
      quoted = true;
    } else if (char === ",") {
      row.push(cell);
      cell = "";
    } else if (char === "\n") {
      row.push(cell);
      rows.push(row);
      row = [];
      cell = "";
    } else if (char !== "\r") {
      cell += char;
    }
  }

  if (cell.length > 0 || row.length > 0) {
    row.push(cell);
    rows.push(row);
  }

  return rows;
}

function formatCsvRow(row: readonly string[]): string {
  return row.map(formatCsvCell).join(",");
}

function formatCsvCell(value: string): string {
  if (!/[",\r\n]/.test(value)) {
    return value;
  }
  return `"${value.replace(/"/g, "\"\"")}"`;
}

