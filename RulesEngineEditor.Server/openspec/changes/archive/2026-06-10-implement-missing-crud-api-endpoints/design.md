## Context

The RulesEngineEditor server currently exposes three API endpoints: `GET /api/Rules` (list workflows), `POST /api/Rules/dry-run` (evaluate rules), and `POST /api/Rules/scenarios` (save a scenario). The React frontend calls these via an axios client that expects full CRUD — but for missing endpoints, it falls back to `localStorage` mock data. This results in workflows and scenarios that are not persisted across sessions or devices.

The backend uses:
- **.NET 10** ASP.NET Core Web API with `[ApiController]` and MVC controller pattern
- **EF Core** with auto-generated entities (EF Core Power Tools) — entity classes must NOT be modified manually
- **SQL Server** with JSON column types for `JsonContent`, `MockInputJson`, `ExpectedOutputJson`
- A repository pattern (`IRulesRepository` / `RulesRepository`)
- Primary constructor injection for controllers and repositories
- `[AllowAnonymous]` on RulesController (auth deferred to future phase)
- Existing specs: `rules-engine-api` (9 reqs), `rules-engine-repository` (5 reqs)

The database has a unique constraint `UQ_Workflow_Version` on `(WorkflowName, Version)` that must be respected.

## Goals / Non-Goals

**Goals:**
- Add 8 new REST endpoints: workflow CRUD (GET/{id}, POST, PUT/{id}, DELETE/{id}) and scenario CRUD (GET, GET/{id}, PUT/{id}, DELETE/{id})
- Add 7 new repository methods to support these endpoints
- Follow existing architectural patterns: MVC controller → repository interface → EF Core implementation
- Use existing JSON validation helper (`IsValidJson`) consistently
- Return proper HTTP status codes (200, 201, 204, 400, 404, 409)
- Maintain `[AllowAnonymous]` — auth is out of scope
- Add unit and integration test coverage for all new code paths
- Update OpenAPI specs (v1.yaml, v1.json) with new endpoints and schemas

**Non-Goals:**
- Modifying auto-generated entity classes (`WorkflowDefinitions`, `WorkflowTestScenarios`)
- Adding authentication or authorization (already deferred)
- Changing the database schema or running EF migrations
- Modifying existing endpoints or their response shapes
- Adding pagination, sorting, or filtering beyond the single `workflowId` query parameter for scenarios
- Implementing soft-delete — deletions are hard deletes
- Implementing versioning strategy for workflows (the `Version` integer field is manually managed by clients)

## Decisions

### Decision 1: Keep DTO records in the controller file
**Choice**: Define new DTO records (`WorkflowDefinitionResponse`, `CreateWorkflowRequest`, `UpdateWorkflowRequest`, `ScenarioResponse`, `UpdateScenarioRequest`) in `RulesController.cs` alongside the existing DTOs (`WorkflowSummaryResponse`, `DryRunRequest`, `SaveScenarioRequest`).
**Rationale**: All existing DTOs live at the bottom of `RulesController.cs`. Consistency avoids premature abstraction. These DTOs are controller-specific request/response types, not shared domain models.
**Alternatives considered**: Separate DTO files in a `Models/` or `DTOs/` folder. Rejected because it deviates from the established pattern and adds file-navigation overhead for this small set of types.

### Decision 2: Repository update methods use "load-then-modify" pattern
**Choice**: For `UpdateWorkflowAsync(id, workflow)` and `UpdateScenarioAsync(id, scenario)`, load the existing entity from the database (WITH tracking), copy provided fields, call `SaveChangesAsync`, and return the updated entity. Return `null` if the entity does not exist.
**Rationale**: This pattern:
- Enables partial updates (only fields present in the request are applied)
- Keeps the change tracker automatically detecting which columns to update
- Avoids `context.Update()` which would overwrite all fields and risk losing data on concurrent changes
- Follows the standard EF Core approach for REST API updates
**Alternatives considered**: Using `context.Update()` + `context.Entry(e).State = Modified` — rejected because it marks ALL columns as modified, even unchanged ones. Using `ExecuteUpdateAsync` — rejected because it requires raw SQL-like expressions and doesn't support conditional partial updates cleanly with nullable DTOs.

### Decision 3: Scenario list/grid reads use AsNoTracking
**Choice**: `GetScenariosAsync` and `GetScenarioByIdAsync` use `AsNoTracking()` for read queries, consistent with existing workflow read methods (`GetAllWorkflowsAsync`, `GetWorkflowByIdAsync`).
**Rationale**: These are read-only operations consumed by the API layer for serialization. Tracking is unnecessary overhead. The existing pattern is already established.

### Decision 4: UpdateScenarioAsync preserves WorkflowDefinitionId
**Choice**: The `UpdateScenarioAsync` method ignores any `WorkflowDefinitionId` on the incoming entity and preserves the existing value from the database.
**Rationale**: Scenarios "belong to" a workflow. Changing the parent workflow would violate the domain model's intent. The frontend `UpdateScenarioRequest` DTO does not include `WorkflowDefinitionId`, but the repository operates on entities. The controller ensures the DTO-to-entity mapping is correct, and the repository enforces immutability of the foreign key.
**Alternatives considered**: Letting the controller handle it exclusively. Rejected because defense-in-depth at the repository layer prevents bugs if the method is reused.

### Decision 5: Unique constraint violations return 409 Conflict
**Choice**: Catch `DbUpdateException` in the controller for create/update operations and check for unique constraint violation on `UQ_Workflow_Version`, returning HTTP 409.
**Rationale**: 409 Conflict is the semantically correct HTTP status for a request that cannot be completed due to a conflict with the current state of the resource. Matching the specific index `UQ_Workflow_Version` in the exception message avoids masking other `DbUpdateException` cases.
**Alternatives considered**: Pre-checking existence with a separate query. Rejected because it introduces a race condition (TOCTOU) and adds an unnecessary round-trip to the database. Letting the exception propagate as 500. Rejected because it's not informative to the client.

### Decision 6: CreatedAt is server-managed for create operations
**Choice**: The controller sets `CreatedAt = DateTime.UtcNow` on new entities before passing to the repository. The repository also provides a safety default.
**Rationale**: Consistent with the existing `SaveScenarioAsync` pattern in the controller. All datetime values use UTC to avoid timezone issues. The repository acts as a secondary safety net.

### Decision 7: OpenAPI spec updates — full rewrite of affected sections
**Choice**: Add all new paths and schemas to both `Api/v1.yaml` and `Api/v1.json`, maintaining the existing structure and content.
**Rationale**: The frontend references these specs. Keeping them in sync ensures API documentation accuracy. YAML is the source of truth; JSON is the generated/consumable version.

## Risks / Trade-offs

**[Risk] Concurrent updates could lose data** → Mitigation: The load-then-modify pattern applies a last-write-wins strategy. For a local developer tool with single-user workflows, this is acceptable. If multi-user concurrency becomes a requirement in the future, add an `ETag` or `RowVersion` column and use optimistic concurrency with `[ConcurrencyCheck]`.

**[Risk] Missing CreatedAt on legacy records** → Mitigation: `CreatedAt` is `DateTime?` (nullable). The server sets it on new creates. Existing records without it will return `null`. The frontend already handles `null` dates.

**[Risk] Hard deletes are irreversible** → Mitigation: The frontend should show a confirmation dialog before deleting. A future phase could implement soft-delete via a `IsDeleted` flag if needed. For a development/lab tool, hard deletes are appropriate.

**[Risk] No auth — open endpoints** → Mitigation: `[AllowAnonymous]` is intentional for the lab/development use case. This change does NOT introduce auth — it maintains the existing posture. Auth will be added in a subsequent phase without changing these endpoint signatures.

## Migration Plan

1. Deploy the updated backend with new endpoints. Existing endpoints remain unchanged.
2. The frontend's `apiClient.js` already has methods that call the new endpoints. If the backend returns success, it uses the backend data. If it returns 404/500, it falls back to `localStorage`. This means the deployment is backward-compatible — the frontend gracefully degrades until all endpoints are live.
3. No database migrations are required. All new operations use the existing schema.
4. Rollback: Reverting the deployment restores the previous state where the frontend uses `localStorage` fallbacks for the missing CRUD operations.

## Open Questions

None. All design decisions have been resolved.
