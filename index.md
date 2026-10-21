---
_layout: landing
---

# kanban-lia

Backend web API for the Kanban board application. It serves the React front-end (`kanban-frontend-lia`) with board, column, placement and column-edge endpoints, pushes live updates over SignalR, and publishes integration events to RabbitMQ through a transactional outbox.

## What it provides

- **REST endpoints** for boards, columns, placements and column edges, returning problem-details responses on errors
- **Live updates** via the SignalR board hub at `/hubs/board`
- **Transactional outbox** — placement changes are committed together with integration events and published to RabbitMQ by a background worker
- **Strongly typed IDs** (`BoardId`, `ColumnId`, `EntityId`) that travel over JSON as GUID strings

## Solution layout

| Area | Role |
| --- | --- |
| `Endpoints/` | Endpoint groups: Boards, Columns, Placements, ColumnEdges |
| `Services/` | Application services, DTOs and integration events |
| `Infrastructure/` | Dapper repositories, outbox worker, RabbitMQ publisher, JSON converters |
| `Models/` | Domain model, value types and DTOs |
| `Hubs/` | SignalR hub for real-time board updates |
| `Mappings/` | AutoMapper profiles |
| `Migrations/` | Flyway SQL migrations |

Start with [Introduction](docs/introduction.md) for the architecture or [Getting Started](docs/getting-started.md) to run it locally.
