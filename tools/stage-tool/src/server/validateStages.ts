import { ValidationError } from "../shared";
import { resolveDataPaths } from "./paths";
import { StageRepository } from "./stageRepository";

async function main(): Promise<void> {
  const paths = resolveDataPaths();
  const repository = new StageRepository(paths.stageCsv, paths.nodeColorsCsv);
  const stages = await repository.listStages();
  process.stdout.write(`[stage-tool:validate] OK: ${stages.length} stage(s)\n`);
}

main().catch((error: unknown) => {
  if (error instanceof ValidationError) {
    process.stderr.write("[stage-tool:validate] ERROR: validation failed\n");
    for (const issue of error.issues) {
      process.stderr.write(`  ${issue.field}: ${issue.message}\n`);
    }
  } else {
    process.stderr.write(`[stage-tool:validate] ERROR: ${error instanceof Error ? error.message : String(error)}\n`);
  }
  process.exit(1);
});
