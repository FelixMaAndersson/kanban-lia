# Getting Started

## Prerequisites

- .NET SDK (the project targets `net10.0`)
- Docker (for SQL Server, migrations and RabbitMQ)

## 1. Start the dependencies

From the `kanban-lia/` folder:

```bash
docker compose up -d
```

This starts:

- **SQL Server** on port **1434** — the `KanbanDb` database is created automatically
- **Flyway** — applies the migrations from `Migrations/`
- **RabbitMQ** on ports 5672 (AMQP) and 15672 (management UI, user `kanban` / password `kanban-dev`)

## 2. Configure

Settings live in `appsettings.json` / `appsettings.Development.json`:

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string (points at `localhost,1434`) |
| `RabbitMq:*` | Host, credentials, exchange (`placement.events`) and routing key (`placement.created`) used by the outbox publisher |

## 3. Run the API

```bash
dotnet run
```

- Swagger UI: `https://localhost:<port>/swagger` (development only)
- SignalR hub: `/hubs/board`

The React front-end in `kanban-frontend-lia/` (Vite, `npm run dev` on port 5173) is already allowed through CORS.

## Next steps

- Explore the endpoint groups in the [API reference](xref:kanban_lia.Services.Placements.PlacementService)
- Read about the [domain model](xref:kanban_lia.Models.Domain.Boards.Board) and the schema constants in `Infrastructure/Schemas`
