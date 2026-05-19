import { resolveDataPaths } from "./src/server/paths";
import { readCsvTable, writeCsvTableAtomic } from "./src/server/csv";
import path from "path";

async function migrate() {
  const paths = resolveDataPaths();
  console.log(`Migrating ${paths.stageCsv}...`);

  const table = await readCsvTable(paths.stageCsv);
  const rewards: Record<number, number> = {
    1: 10,
    2: 15,
    3: 20,
    4: 30,
    5: 40
  };

  table.records = table.records.map(record => {
    const difficulty = parseInt(record.difficulty, 10) || 1;
    return {
      ...record,
      moveLimit: record.moveLimit && record.moveLimit.trim() !== "" ? record.moveLimit : "0",
      soft_reward: record.soft_reward && record.soft_reward.trim() !== "" ? record.soft_reward : String(rewards[difficulty] || 10)
    };
  });

  await writeCsvTableAtomic(paths.stageCsv, table);
  console.log("Migration complete.");
}

migrate().catch(console.error);
