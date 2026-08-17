# AI Usage

## Purpose
 I used AI to speed up drafting, explore options, and sanity‑check ideas. I reviewed every suggestion and made the final design and code decisions.

## Tools/models
- Claude Sonnet (Anthropic Claude 3.5 Sonnet), via the Claude app
- Continue.dev (VS Code extension) connected to Azure OpenAI (model label "GPT 5.0")

## Where AI helped
- Architecture discussion: propose a clean layered design (API → Application → Domain → Infrastructure), controller-based REST, service layer, and a specific repository (not generic)
- API/server wiring: outline TaskItems endpoints and a minimal, dev-only Swagger UI; shape exception middleware for consistent 400/404/409/500 responses
- Unit tests: suggest critical scenarios for TaskItemService (title uniqueness, forward-only stage transitions and idempotency, reopen rules, soft delete/restore, paging validation)
- React web UI (optional): scaffold and refine patterns for a Vite + React + TypeScript app using Ant Design, including
  - Axios client and typed DTOs aligned with the API
  - List with filters (stage, urgency, deadline range, include deleted), sort (urgency/deadline), paging
  - Create/Edit modal with validation rules, and professional notifications/confirmations
  - Row actions: change stage (idempotent), reopen, soft delete, restore
  - Practical UX details (router “/home”, sentinel values for “All/(default)”, show Restore only when includeDeleted && isDeleted)


## Where AI did not help
- Final architecture and repository pattern choices
- Core business logic in TaskItemService (stage rules, soft delete/restore, case‑insensitive uniqueness)
- EF Core configuration (indexes, filtered unique index, NOCASE collation) and repository correctness
- Final review and integration of code and tests

## Representative prompts I used
- "Review the requirements and suggest a clean architecture As a senior .NET architect."
- "Review the project and propose unit tests for TaskItemService As a senior .NET architect."
- "Generate TaskItems controller endpoints based on the requirements As a senior .NET architect."
- "As a senior front-end engineer, review the TaskTracker API with requirements and generate a minimal, maintainable React web app."


## Ownership
I treated AI output as suggestions. I kept what fit, rewrote where needed, and discarded the rest. Final architecture, design choices, and code are mine.
