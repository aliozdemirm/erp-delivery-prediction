# ERP Delivery Prediction

A 10-day MVP that predicts manufacturing order delivery time from read-only ERP data.

Prediction combines several independent providers:

- **Rule-Based + Critical Path Method (CPM)** — primary provider, computed in working minutes and converted to dates by a C# `WorkingCalendar`.
- **AI prediction** — secondary provider (Python + FastAPI), built only from raw prediction-context snapshots. Its failure never blocks the rule-based fallback.
- **Final Hybrid** — combines both results in the Application layer.
- **What-if predictions** — run a prediction for a hypothetical order (product, quantity) without touching ERP data.

> This is a team project. See [Team and contributions](#team-and-contributions).

## Architecture

Modular monolith with lightweight Clean Architecture. The source of truth is [`docs/SAD-v1.2.md`](docs/SAD-v1.2.md).

| Service | Technology | Role |
|---|---|---|
| `frontend` | React, Vite, TypeScript, MUI, Recharts | Dashboard, orders, stock, predictions, delivery map |
| `api` | ASP.NET Core Web API (.NET 8) | Composition root, prediction orchestration, JWT auth |
| `postgres` | PostgreSQL | Application database |
| `mock-erp` | ASP.NET Core | Separate read-only ERP API serving seed data |
| `ai-prediction` | Python, FastAPI | AI prediction service |

Backend projects under `src/`: `App.Domain`, `App.Application`, `App.Integration`, `App.Persistence`, `App.Infrastructure`, `App.Api`, `MockErp.Api`.

Dependency rules: Domain has no external dependencies, Application depends on Domain, Integration and Persistence depend on Application, and concrete implementations are registered only in `App.Api`. ERP data is accessed only through `IErpDataProvider`.

## Getting started

Requirements: Docker with Compose.

```bash
docker compose up --build
```

- Frontend: http://localhost:3000
- API / Swagger: http://localhost:5000

Ports and secrets can be overridden through environment variables (see `docker-compose.yml`). The default values are for local development only.

## Tests

```bash
dotnet test
```

```bash
cd frontend && npm install && npm test
```

The .NET solution has about 400 tests across the Domain, Application, Infrastructure, Integration and API projects. The ERP seed converter in `tools/erp-seed-converter` has its own pytest suite.

## Repository layout

```
src/        .NET backend and Mock ERP
tests/      .NET test projects
frontend/   React SPA
ai-prediction/   FastAPI AI service
tools/erp-seed-converter/   Excel to Mock ERP seed converter
docs/       SAD, workflow and handoff documents
```

## Team and contributions

Built by a four-person team: Eren Torman, Pınar, Yusuf Yüceur and Muhammed Ali Özdemir.

Muhammed Ali Özdemir worked on:

- the React + Vite + TypeScript frontend: auth, protected routes, axios client, UI kit and layout, and the UI/UX redesign (theme, dashboard charts, delivery map)
- the isolated Python + FastAPI AI prediction service foundation
- the prediction pipeline: `ErpBatchReader`, `PredictionContextBuilder`, fallback resolvers, `WorkingCalendar`, the prediction calculation service and API endpoint
- the ERP seed converter and its pytest coverage, and the read-only `GET /Products` endpoint

The full history is in `git log`.
