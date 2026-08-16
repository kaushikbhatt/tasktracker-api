# Design Decisions and Rationale

## What we built (short overview)
- A clean REST API for TaskItem with:
  - Create, Get, List (with filters/sort/paging + totalCount), Update (PUT), Patch (partial), Soft Delete, Restore, Change Stage (idempotent), Reopen.
  - Consistent error responses with stable error codes.
  - A large data seed path (≥ 42,536 TaskItems) for realistic testing.

## Architecture and patterns (what we chose and why)
- Layered architecture (API → Application → Domain → Infrastructure)
  - Why: Clear responsibilities. Controllers handle HTTP. The service holds business rules. EF Core stays in Infrastructure. This makes the system readable, testable, and easy to change.
  - Not chosen: Vertical Slice/MediatR feature-by-feature architecture. For a single-entity slice, it adds indirection with little gain. We kept it simple and explicit.

- Controllers (not Minimal APIs-only)
  - Why: Controllers provide clear structure for routes, filters, and versioning, and match common team standards.
  - Not chosen: Minimal APIs for everything. They’re fine, but controllers give better structure for this API.

- Service layer pattern (TaskItemService)
  - Why: Centralizes domain rules (title uniqueness, stage movement, soft delete/restore, validation boundaries). Enables fast unit tests without HTTP/EF involved.
  - Not chosen: Put rules in controllers (mixes concerns) or EF interceptors (too implicit for business logic).

- Specific Repository pattern (ITaskItemRepository)
  - Why: A focused repository exposes exactly what we need: server-side filter/sort/page with totalCount, title checks, urgency helpers, SaveChanges. It encloses EF/SQLite details but stays simple.
  - Not chosen: Generic Repository + Unit of Work. EF Core already acts as a unit of work; generic repos add ceremony and leaky abstractions without helping this slice.

- DTOs + FluentValidation + Optional<T> for PATCH
  - Why: Clear input validation and safe partial updates (we distinguish "field omitted" vs "set to null").
  - Not chosen: JSON Patch documents. More complex than needed and harder for clients to validate.

## Business rules (how we applied them)
- Title uniqueness among active items (case-insensitive)
  - Enforced by a filtered unique index on Title where IsDeleted = 0 using SQLite COLLATE NOCASE. The service pre-check uses the same collation so clients get a friendly 409 in the common case; the DB protects under concurrency.
  - Not chosen: Case-sensitive uniqueness. It confuses users ("Task A" vs "task a").

- Stage changes are forward-only; "Reopen" is the one deliberate backward move
  - Normal updates can only move forward. Backward moves raise a conflict. Reopen moves Finished → InProgress on purpose.
  - Stage is not editable via PUT/PATCH to prevent accidental regressions.

- PUT vs PATCH
  - PUT replaces editable fields (title, notes, urgency, deadline). PATCH only updates fields sent (Optional<T> implements this safely). Stage remains out-of-band via dedicated actions.

- Soft delete and restore
  - Delete sets IsDeleted and DeletedAtUtc; restore clears both and re-checks title uniqueness among active items so a restored title cannot collide.

- Consistent error responses
  - 400 validation, 404 not found, 409 conflicts, 500 unexpected, all with stable errorCode and clear messages. DB unique violations are mapped to DUPLICATE_ACTIVE_TITLE.

## Data model and constraints (high level)
- TaskItem: Id, Title (NOCASE), Notes, Stage, UrgencyLevelId, Deadline, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc.
- UrgencyLevel: Id, Name, SortOrder, IsActive (seeded Low/Medium/High; adding a fourth is a data change, not code).
- Indexes: Stage, UrgencyLevelId, Deadline, (IsDeleted, DeletedAtUtc), and a unique index on Title for active items only.

## List endpoint and performance
- Server-side IQueryable with AsNoTracking:
  - Filters: stage, urgency, deadline range, includeDeleted.
  - Sorting: by urgency (via UrgencyLevel.SortOrder) or deadline, with stable tie-breakers (UpdatedAtUtc, Id) to keep paging deterministic.
  - Paging and totalCount done in the database.
  - Include(UrgencyLevel) only when materializing the page (not for count).
- Comfortable for ~tens of thousands of rows and beyond. If growth or reporting complexity increases:
  - Project directly to DTOs to reduce materialization
  - Use compiled queries for hot paths
  - Consider denormalizing urgency sort value on TaskItem if "sort by urgency" becomes very hot
  - Consider read models/materialized views for heavy reporting
- Not chosen: In-memory filtering. It’s wasteful and slow.

## Seeding and reviewer experience
- Seeder (TaskTracker.Seeder) generates ≥ 42,536 TaskItems (batched, deterministic) and auto-migrates schema.
- For the ZIP: include a pre-seeded TaskTracker/tasktracker.db so reviewers see real data instantly. Also document how to regenerate via the seeder.

## What we did not build (on purpose)
- Authentication/Authorization: out of scope; a house pattern will be applied later.
- 90-day purge job: acknowledged but not implemented; that’s an ops/scheduling concern.
- Stage dwell-time reporting: needs state transition history; intentionally deferred.
- Generic repository/UoW & advanced concurrency tokens: not needed for this slice; the unique index + service rules cover the important conflicts.

## Testing approach
- Focused unit tests for TaskItemService (uniqueness, forward-only stages, reopen, soft delete/restore edge cases, paging validation). Fast and high-signal.