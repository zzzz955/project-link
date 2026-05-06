import http, { IncomingMessage, ServerResponse } from "http";
import { createReadStream, existsSync } from "fs";
import path from "path";
import { GenerateStageInput, generateStagePayload, StagePayload, ValidationError } from "../shared";
import { StageRepository } from "./stageRepository";

interface JsonResponse {
  status: number;
  body?: unknown;
}

export function createServer(repository: StageRepository): http.Server {
  return http.createServer(async (request, response) => {
    try {
      const result = await route(repository, request, response);
      if (result.status === 0) {
        return;
      }
      sendJson(response, result.status, result.body);
    } catch (error) {
      handleError(response, error);
    }
  });
}

async function route(repository: StageRepository, request: IncomingMessage, response: ServerResponse): Promise<JsonResponse> {
  const method = request.method ?? "GET";
  const url = new URL(request.url ?? "/", "http://localhost");
  const pathname = url.pathname;

  if (method === "OPTIONS") {
    return { status: 204 };
  }

  if ((method === "GET" || method === "HEAD") && !pathname.startsWith("/api/")) {
    return serveStatic(request, response, pathname);
  }

  if (method === "GET" && pathname === "/api/stages") {
    return { status: 200, body: await repository.listStages() };
  }

  if (method === "GET" && pathname === "/api/node-colors") {
    return { status: 200, body: await repository.listNodeColors() };
  }

  if (method === "POST" && pathname === "/api/stages/generate") {
    const input = await readJsonBody<GenerateStageInput>(request);
    return { status: 200, body: generateStagePayload(input) };
  }

  const validateMatch = /^\/api\/stages\/(\d+)\/validate$/.exec(pathname);
  if (validateMatch && method === "POST") {
    const stageId = parseInt(validateMatch[1], 10);
    const payload = await readJsonBody<StagePayload>(request);
    return { status: 200, body: repository.validateStage(stageId, payload) };
  }

  const stageMatch = /^\/api\/stages\/(\d+)$/.exec(pathname);
  if (stageMatch) {
    const stageId = parseInt(stageMatch[1], 10);
    if (method === "GET") {
      const stage = await repository.getStage(stageId);
      return stage ? { status: 200, body: stage } : { status: 404, body: { error: "stage not found" } };
    }
    if (method === "POST") {
      const payload = await readJsonBody<StagePayload>(request);
      return { status: 201, body: await repository.createStage(stageId, payload) };
    }
    if (method === "PUT") {
      const payload = await readJsonBody<StagePayload>(request);
      return { status: 200, body: await repository.updateStage(stageId, payload) };
    }
    if (method === "DELETE") {
      await repository.deleteStage(stageId);
      return { status: 204 };
    }
  }

  return { status: 404, body: { error: "not found" } };
}

function serveStatic(request: IncomingMessage, response: ServerResponse, pathname: string): JsonResponse {
  const clientRoot = path.resolve(__dirname, "..", "client");
  const requested = pathname === "/" ? "index.html" : pathname.replace(/^\/+/, "");
  const target = path.resolve(clientRoot, requested);
  const safeTarget = target.startsWith(clientRoot) ? target : path.join(clientRoot, "index.html");
  const filePath = existsSync(safeTarget) ? safeTarget : path.join(clientRoot, "index.html");

  if (!existsSync(filePath)) {
    return { status: 404, body: { error: "stage tool frontend not built; run npm run build or use npm run dev" } };
  }

  response.statusCode = 200;
  response.setHeader("Content-Type", contentType(filePath));
  if (request.method === "HEAD") {
    response.end();
  } else {
    createReadStream(filePath).pipe(response);
  }
  return { status: 0 };
}

function contentType(filePath: string): string {
  const ext = path.extname(filePath).toLowerCase();
  if (ext === ".html") return "text/html; charset=utf-8";
  if (ext === ".js") return "text/javascript; charset=utf-8";
  if (ext === ".css") return "text/css; charset=utf-8";
  if (ext === ".svg") return "image/svg+xml";
  return "application/octet-stream";
}

function readJsonBody<T>(request: IncomingMessage): Promise<T> {
  return new Promise((resolve, reject) => {
    const chunks: Buffer[] = [];
    request.on("data", (chunk: Buffer) => chunks.push(chunk));
    request.on("error", reject);
    request.on("end", () => {
      try {
        const raw = Buffer.concat(chunks).toString("utf8").trim();
        resolve((raw.length === 0 ? {} : JSON.parse(raw)) as T);
      } catch (error) {
        reject(new ValidationError([{ field: "body", message: "must be valid JSON" }]));
      }
    });
  });
}

function sendJson(response: ServerResponse, status: number, body?: unknown): void {
  response.statusCode = status;
  response.setHeader("Access-Control-Allow-Origin", "*");
  response.setHeader("Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS");
  response.setHeader("Access-Control-Allow-Headers", "Content-Type");
  if (status === 204 || body === undefined) {
    response.end();
    return;
  }
  response.setHeader("Content-Type", "application/json; charset=utf-8");
  response.end(JSON.stringify(body));
}

function handleError(response: ServerResponse, error: unknown): void {
  if (error instanceof ValidationError) {
    sendJson(response, 400, { error: "validation failed", issues: error.issues });
    return;
  }
  sendJson(response, 500, { error: error instanceof Error ? error.message : String(error) });
}
