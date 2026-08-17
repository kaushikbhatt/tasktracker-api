# TaskTracker API (.NET 10 + EF Core + SQLite)

A REST API for managing TaskItem records used by operations staff. Built with ASP.NET Core controllers, EF Core, and SQLite. Designed with robust domain rules, efficient filtering/sorting/paging, and consistent error handling.

## Prerequisites
- .NET 10 SDK
- Node.js LTS (only if you want to run the optional React web UI)
- A modern browser for Swagger UI

## Quick Start (recommended)
- Unzip the archive.
- Place the provided SQLite file at: TaskTracker/tasktracker.db (same folder as TaskTracker.Api.csproj).Default it has been  placed already under TaskTracker/.
- Run the API in Development:
  - Visual Studio/VS Code: run the https profile. Your browser should auto-open at /swagger.
  - Or browse manually to:
    - https://localhost:7148/swagger
    - or http://localhost:5038/swagger

**Important: Use the provided pre-seeded database (tasktracker.db) to see realistic performance with ≥ 42,536 TaskItems immediately.**

## (Re)Generating seed data
- Use the included seeder (it auto-applies migrations):
  - From the solution root:
    - `dotnet run --project TaskTracker.Seeder -- --db ./TaskTracker/tasktracker.db --count 42536`

**Important: The API auto-applies EF Core migrations on startup in Development. No separate “database update” step is required when using the seeder or the provided DB.**

## API usage (via Swagger UI)
Open /swagger and try:
- **List TaskItems**: filter by stage, urgencyLevelId, deadlineFrom/to; sort by urgency or deadline; page and pageSize; response includes totalCount.
- **Create**: POST /task-items
- **Update (full)**: PUT /task-items/{id}
- **Patch (partial)**: PATCH /task-items/{id}
- **Soft delete**: DELETE /task-items/{id}
- **Restore**: POST /task-items/{id}/restore
- **Change stage (idempotent)**: POST /task-items/{id}/stage { targetStage }
- **Reopen (Finished → InProgress)**: POST /task-items/{id}/reopen
- **List urgency levels**: GET /urgency-levels

## Validation & errors
- **Validation (400)**: `{ "errorCode": "VALIDATION_FAILED", "message": "...", "errors": { "Field": ["reason"] } }`
- **Not found (404)**: `{ "errorCode": "TASK_ITEM_NOT_FOUND", "message": "TaskItem '<id>' was not found." }`
- **Conflict (409)** (e.g., duplicate active title): `{ "errorCode": "DUPLICATE_ACTIVE_TITLE", "message": "..." }`
- **Unexpected (500)**: `{ "errorCode": "UNEXPECTED_ERROR", "traceId": "..." }`

## Ports & URLs
- HTTPS: https://localhost:7148
- HTTP: http://localhost:5038
- Swagger UI: `/swagger` (Development only)

## Web UI (optional, React)
- Location: `tasktracker-web`
- Purpose: A simple internal dashboard to try the API with real data

Setup (first time on a new laptop):
- Install Node.js LTS from nodejs.org
- In a terminal:
  - `cd tasktracker-web`
  - `npm install`
  - Optional: create `.env.local` only if you need to override the API URL. By default the web app uses `https://localhost:7148`:
    - `VITE_API_BASE_URL=https://localhost:7148`
  - Start the dev server:
    - `npm run dev`
  - Open: http://localhost:5173

Features in the web UI:
- Filters: Stage, Urgency, Deadline range, Include deleted
- Sorting: urgency or deadline
- Paging with totalCount
- Actions per row: Change Stage, Reopen (if Finished), Delete (soft), Restore (only for deleted items)
- Create/Edit Task with validation

Notes:
- CORS is enabled in Development for http://localhost:5173 (and https) in Program.cs.
- If you see CORS errors, ensure the API is running, then restart the React dev server.
- If HTTPS calls fail from the browser, trust the dev certificate once:
  - `dotnet dev-certs https --trust`

## Tests
- Run all unit tests:
  - `dotnet test`

## Notes & assumptions
- **Server-side timestamps**: CreatedAtUtc and UpdatedAtUtc are set by the server; client values are ignored.
- **Stage is not edited via PUT/PATCH**; use **Change Stage** or **Reopen**.
- **Title uniqueness is case-insensitive among active items**; deleted items do not block reuse.
- **Migrations**: auto-applied on startup in Development.
- **Swagger**: enabled in Development only.

## Troubleshooting
- If HTTPS doesn’t open locally:
  - Run once: `dotnet dev-certs https --trust` or use http URL.
- If you see “no such table: TaskItems”:
  - Ensure you used the **pre-seeded DB** or ran the **seeder** command above.
- If Swagger doesn’t auto-open:
  - Visit https://localhost:7148/swagger or http://localhost:5038/swagger manually.

## Project layout (brief)
- TaskTracker (API): controllers, DI, middleware, Swagger
- TaskTracker.Application: services, DTOs, validation, exceptions
- TaskTracker.Domain: entities (TaskItem, UrgencyLevel), enums (TaskStage)
- TaskTracker.Infrastructure: DbContext, EF configurations, migrations, repository
- TaskTracker.Seeder: console app generating ≥ 42,536 TaskItems
- TaskTracker.Tests: unit tests for TaskItemService
- tasktracker-web: optional React UI (Vite + React + TypeScript)
