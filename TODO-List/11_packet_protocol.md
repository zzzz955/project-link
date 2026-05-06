# 11 — Packet Protocol (gen-packets Rewrite)

Current: `tools/gen-packets.js` generates language-agnostic JSON-based packet classes.
Target: C#-specific `record` types with JSON serialization attributes and OpenAPI annotations, generated for both Unity client and ASP.NET server.

Protocol: HTTP REST + JSON body. Scalar/OpenAPI documentation auto-generated from annotations.
Binary protocol (MessagePack etc.) deferred — only relevant if WebSocket/real-time is introduced.

---

## Client-First Contract Scope

Generate or mirror these DTOs before replacing mock/local services with HTTP adapters.

- [ ] Auth DTOs needed by `IAuthService`
  - guest login request/response
  - login request/response
  - refresh request/response
  - error response
- [ ] Progress DTOs needed by `IProgressService`
  - get progress response
  - batch upsert request/response
  - stage progress item
- [ ] DTO field names match client service models exactly
- [ ] Server controllers consume the same generated DTO types where possible

---

## Output Targets

| Target | Location | Convention |
|--------|----------|------------|
| Client DTOs | `client/project-link/Assets/Scripts/Data/Generated/packets/` | `using System.Text.Json.Serialization;` |
| Server DTOs | `server/src/ProjectLink.API/Generated/Packets/` | + XML doc comments for Scalar |

Both output dirs are `generated/` — DO NOT edit manually; edit source `.packet.json` and re-run `npm run gen:packets`.

---

## Extended packet.json Schema

Extend `shared/packets/*.packet.json` to include C#-specific fields:

```json
{
  "domain": "progress",
  "namespace_client": "ProjectLink.Data.Generated.Packets",
  "namespace_server": "ProjectLink.API.Generated.Packets",
  "packets": [
    {
      "name": "UpsertProgress",
      "direction": "client→server",
      "description": "Batch upsert stage progress records.",
      "request": {
        "fields": [
          { "name": "items", "type": "array", "element": "StageProgressItem" }
        ]
      },
      "response": {
        "fields": [
          { "name": "success", "type": "bool" }
        ]
      }
    }
  ],
  "types": [
    {
      "name": "StageProgressItem",
      "fields": [
        { "name": "stageId",   "type": "int32" },
        { "name": "stars",     "type": "int32" },
        { "name": "clearedAt", "type": "datetime" }
      ]
    }
  ]
}
```

---

## C# Generation Rules

### Request DTO (both client + server)
```csharp
// Generated — DO NOT edit. Source: shared/packets/[domain].packet.json
public record [Name]Request(
    [property: JsonPropertyName("fieldName")] Type FieldName
);
```

### Response DTO (both client + server)
```csharp
public record [Name]Response(
    [property: JsonPropertyName("fieldName")] Type FieldName
);
```

### Shared nested types
```csharp
public record [TypeName](
    [property: JsonPropertyName("fieldName")] Type FieldName
);
```

### Server-side additions (XML doc for Scalar)
```csharp
/// <summary>[description]</summary>
public record [Name]Request(...);
```

### Type mapping
| packet.json type | C# type |
|-----------------|---------|
| `int32` | `int` |
| `int64` | `long` |
| `bool` | `bool` |
| `string` | `string` |
| `datetime` | `DateTimeOffset` |
| `float` | `float` |
| `array` of T | `List<T>` |
| `T?` (nullable) | `T?` |

---

## gen-packets.js Rewrite Tasks

- [ ] Parse extended schema: add `namespace_client`, `namespace_server`, `types` fields
- [ ] Generate client C# file per domain → `Assets/Scripts/Data/Generated/packets/[domain]Packets.cs`
- [ ] Generate server C# file per domain → `server/src/ProjectLink.API/Generated/Packets/[domain]Packets.cs`
- [ ] Emit `JsonPropertyName` attribute on all properties (source field name, camelCase)
- [ ] Emit XML doc `<summary>` on server-side records (from `description` field)
- [ ] Add file header comment: `// Generated — DO NOT edit. Source: shared/packets/[domain].packet.json`
- [ ] Validation: duplicate packet names → error; referenced type not defined → error
- [ ] Update `tools/AGENTS.md` with new output paths after implementation

## Existing packet.json Files

Check `shared/packets/` — migrate any existing files to the extended format.
If none exist, create `shared/packets/progress.packet.json` as the first example covering the Stage Progress API.

<!-- changed: packet protocol now prioritizes auth/progress DTOs required to swap client adapters to HTTP -->
