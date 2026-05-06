import { createServer } from "./http";
import { resolveDataPaths } from "./paths";
import { StageRepository } from "./stageRepository";

const port = parsePort(process.env.PORT);
const paths = resolveDataPaths();
const repository = new StageRepository(paths.stageCsv, paths.nodeColorsCsv);
const server = createServer(repository);

server.listen(port, () => {
  process.stdout.write(`stage-tool server listening on http://localhost:${port}\n`);
});

function parsePort(value: string | undefined): number {
  if (value === undefined) {
    return 5178;
  }
  const port = parseInt(value, 10);
  if (!Number.isInteger(port) || port < 1 || port > 65535) {
    throw new Error("PORT must be 1..65535");
  }
  return port;
}
