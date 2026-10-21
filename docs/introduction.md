# Introduction

kanban-lia is an ASP.NET Core (net10.0) web API that powers the Kanban front-end. Domain operations are grouped into four endpoint sets — boards, columns, placements and column edges — backed by application services and Dapper repositories over SQL Server.

## Request pipeline

A typical request flows:

```
HTTP endpoint → Service → Repository (Dapper) → SQL Server
                    │
                    └─► Outbox (same transaction) ─► OutboxWorker ─► RabbitMQ
```

- **Endpoints** (`Endpoints/`) bind requests, call the matching service and map the result to response DTOs via AutoMapper profiles (`Mappings/`).
- **Services** (`Services/`) implement the domain rules — for example `PlacementService` validates columns and boards, takes per-column sort-key locks, appends placement rows, and writes a `PlacementCreated` integration event to the outbox in the same transaction.
- **Repositories** (`Infrastructure/Repositories/`) use Dapper against `KanbanDb`; table and column names come from the `Infrastructure/Schemas` constants.

## Cross-cutting concerns

- **Errors** — a global exception handler maps known domain exceptions and SQL Server constraint errors to problem responses: not-found → 404, non-writable column → 403, duplicate keys → 409, everything else → 500. See `Program.cs`.
- **Realtime** — the [BoardHub](xref:kanban_lia.Hubs.BoardHub) at `/hubs/board` lets the front-end subscribe to board changes.
- **Messaging** — an [OutboxWorker](xref:kanban_lia.Infrastructure.Repositories.Outbox.OutboxWorker) drains committed outbox messages and publishes them to the `placement.events` RabbitMQ exchange via `RabbitMqIntegrationEventPublisher`.
- **JSON** — strongly typed identifier converters (`BoardIdJsonConverter`, `ColumnIdJsonConverter`, `EntityIdJsonConverter`) serialize ids as GUID strings.
- **CORS** — the local front-end origin (`http://localhost:5173`) is allowed with credentials.
- **Swagger** — enabled in development at `/swagger`.
