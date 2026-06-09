## Context

The RulesEngine Editor is a .NET 10 ASP.NET Core Web API following Clean Architecture with three root-level folders: `/Business` (entities, services), `/Infrastructure` (data, repositories, identity), and `/Controllers` (API endpoints). The project uses SQL Server via EF Core, ASP.NET Core Identity with Passkey/WebAuthn, and has a project reference to Microsoft RulesEngine v6.0 — but the library is currently unused.

Two database tables already exist as EF Core Power Tools scaffolded models: `WorkflowDefinitions` (WorkflowDefinitionId, WorkflowName, Version, JsonContent, Status, CreatedAt, CreatedBy) and `WorkflowTestScenarios` (ScenarioId, WorkflowDefinitionId, ScenarioName, MockInputJson, ExpectedOutputJson, CreatedAt). However, these tables are not yet registered in the runtime `ApplicationDbContext` and have no EF Core migration.

**Entity naming note**: The scaffolded entity class names use plural form (`WorkflowDefinitions`, `WorkflowTestScenarios`) — matching the database table names. New code (repository interfaces, configurations, DbSets) follows the existing scaffolded naming for consistency.

The existing architecture follows an opinionated pattern: controllers inject domain services via primary constructors, services delegate to repositories, and data access uses stored procedures through `BaseStoredProcedureRepository<TEntity, TKey>`. However, the stored procedure pattern has only one concrete implementation (`UserRepository`) which diverges from the pattern by directly querying the scaffolded context.

The new RulesEngine Evaluation Service must integrate into this architecture while remaining stateless — each evaluation request must create a fresh `Microsoft.RulesEngine.RulesEngine` instance to avoid shared state across requests.

## Goals / Non-Goals

**Goals:**

- Implement a stateless `IRulesEvaluationService` that instantiates `Microsoft.RulesEngine.RulesEngine` per evaluation request using caller-provided workflow JSON and `ReSettings`.
- Support all RulesEngine v6 features: nested workflows, rule hierarchies, rule actions (Success/Failure), LINQ-based expressions, `RuleResultTree` inspection.
- Support dynamic `CustomTypes` registration via reflection from API request parameters.
- Provide a `Verify` method that structurally compares `RuleResultTree` output against `ExpectedOutputJson`.
- Expose REST API endpoints: `GET /api/rules` (list workflows), `POST /api/rules/dry-run` (execute rules with facts/settings), `POST /api/rules/scenarios` (save test inputs/outputs).
- Register `WorkflowDefinitions` and `WorkflowTestScenarios` as EF Core entities in `ApplicationDbContext` with JSON column mapping via `IEntityTypeConfiguration<T>` classes.
- Use existing DI patterns: scoped service registration, primary constructor injection.
- Return structured error responses for malformed JSON, invalid workflows, and runtime evaluation failures.

**Non-Goals:**

- NOT a stateful rules engine — no caching, no pre-compilation, no global rule registration.
- NOT a workflow authoring UI — the API serves as a back-end for an existing front-end editor.
- NOT a stored procedure-based repository — uses EF Core direct queries (`DbSet` with LINQ) rather than `BaseStoredProcedureRepository` for simplicity, consistent with the `UserRepository` pattern that already does direct `DbContext` queries.
- NOT a full CRUD API for workflow definitions — only read (`GET /api/rules`) and scenario save (`POST /api/rules/scenarios`) endpoints.
- NOT implementing rule version comparison or workflow promotion workflows.
- NOT implementing authentication/authorization on rules endpoints (deferred to a future change).

## Decisions

### Decision 1: Stateless Engine Instantiation (Per-Request) over Singleton

**Context**: Microsoft RulesEngine can be instantiated with a list of `Workflow` objects and reused across evaluations. A singleton approach would pre-load and cache workflows for performance.

**Decision**: Instantiate a new `RulesEngine` instance on every evaluation call. Do NOT cache or globally register workflows.

**Rationale**: The RulesEngine Lab use case requires rapid iteration where rules change frequently. Caching would require cache invalidation when workflows are updated in the database, adding complexity and potential for stale evaluations. Per-request instantiation guarantees the latest workflow JSON is always used, even if it was updated milliseconds before the request. The RulesEngine library is designed for fast instantiation (parser compilation is the main cost, not construction).

**Alternatives Considered**:
- **Singleton with cache invalidation**: Rejected — adds cache infrastructure and race conditions. Not justified for the expected request volume (low-to-moderate, lab/tool usage).
- **MemoryCache with sliding expiration**: Rejected — still requires invalidation logic and adds statefulness to a service explicitly designed to be stateless.

### Decision 2: EF Core Direct Queries over Stored Procedures

**Context**: The project's documented architecture mandates stored procedures through `BaseStoredProcedureRepository<TEntity, TKey>`. However, the existing `UserRepository` already diverges from this pattern by using direct `DbContext` queries.

**Decision**: Use direct EF Core queries (`DbContext.Set<T>().ToListAsync()`, `.FirstOrDefaultAsync()`) in the `RulesRepository` rather than stored procedures.

**Rationale**: The workflow tables are simple CRUD tables with no complex query logic that would benefit from stored procedures. The stored procedure pattern adds significant boilerplate (abstract SP name properties, parameter mapping methods) for no benefit. Following the `UserRepository` precedent maintains consistency with what's already in the codebase. If the stored procedure pattern is enforced later, `RulesRepository` can be migrated.

**Alternatives Considered**:
- **Full stored procedure pattern**: Rejected — excessive boilerplate for simple CRUD on two tables.
- **Generic repository with EF Core**: Rejected — over-abstracts a simple data access layer. An explicit `IRulesRepository` interface makes the contract clear.

### Decision 3: Dual-Mode Evaluation (Dry-Run accepts inline JSON)

**Context**: The `POST /api/rules/dry-run` endpoint could either reference a stored workflow by ID or accept inline workflow JSON.

**Decision**: Accept inline `RulesJson`, `FactsJson`, and `SettingsJson` in the request body. Do NOT require a stored workflow ID.

**Rationale**: The dry-run use case is for testing rules before saving them. Forcing a save-before-test workflow would be counterproductive. Accepting inline JSON allows the front-end editor to send rule definitions directly for evaluation without persisting them first. The `GET /api/rules` endpoint separately serves the "load saved workflow" use case.

### Decision 4: Verify Method Uses Structural Comparison (not exact JSON match)

**Context**: RulesEngine outputs a `RuleResultTree` — a hierarchical structure of rule evaluation results. Comparing this to `ExpectedOutputJson` could be simple string comparison or structural comparison.

**Decision**: Implement a structural deep-comparison that walks both the `RuleResultTree` (deserialized to JToken) and the `ExpectedOutputJson` tree, comparing node-by-node. Arrays are compared in order by index. Extra properties in actual output are ignored (only expected properties are checked). Missing expected properties are reported as differences.

**Rationale**: Exact JSON string comparison is fragile — different Newtonsoft/System.Text.Json serialization settings produce different whitespace, property ordering, and casing. Structural comparison produces reliable pass/fail results for test scenario validation. Array ordering is preserved because RulesEngine output typically has deterministic ordering; ignoring extra properties in actual output allows the engine to include metadata without breaking comparisons.

**Alternatives Considered**:
- **Exact string match**: Rejected — too fragile for real-world use.
- **FluentAssertions JSON comparison**: Considered but adds a NuGet dependency. A custom implementation is lightweight and avoids coupling to an assertion library in production code.

### Decision 5: CustomTypes via Reflection with Assembly Load

**Context**: RulesEngine supports custom types for LINQ expressions (e.g., `customer.Tier == "Premium"` where `customer` is a custom type). These types must be registered in `ReSettings.CustomTypes` before evaluation.

**Decision**: Accept a `CustomTypes` array of assembly-qualified type names in the API request. Use `Type.GetType()` with fallback to `Assembly.Load()` for types not in the current AppDomain. Validate that each resolved type is a reference type (class) before adding to `ReSettings.CustomTypes`.

**Rationale**: This is the same mechanism RulesEngine uses internally via `CustomTypeProvider`. Requiring assembly-qualified names gives API consumers full control over which types to register without the server needing to know about them at compile time. Validation prevents accidentally registering value types or primitives that would break expression compilation.

### Decision 6: Error Handling via try-catch with ProblemDetails

**Context**: RulesEngine can throw `RuleValidationException` for invalid workflow definitions and various runtime exceptions during evaluation. Malformed JSON in request bodies is also a concern.

**Decision**: Wrap all evaluation logic in try-catch blocks. Catch `RuleValidationException` → return 400 with validation details. Catch `JsonException` → return 400 with parse error details. Catch `Exception` → return 500 with generic error (avoid leaking internal details). The existing `GlobalExceptionHandler` middleware already handles unhandled exceptions generically; service-level try-catch provides domain-specific error messages.

**Rationale**: The existing `IExceptionHandler` middleware catches unhandled exceptions generically. Service-level try-catch allows domain-specific error messages (e.g., "Rule at index 2 has an invalid expression: ...") that the middleware would not produce.

## Risks / Trade-offs

- **[Risk] Per-request RulesEngine instantiation may be slow for complex workflows**: The RulesEngine library compiles expressions on construction. Large workflows with many rules could cause noticeable latency.
  → **Mitigation**: Monitor response times. If performance becomes an issue, add `TimeProvider`-based timing and consider a compile-once-cache-many pattern with explicit cache invalidation APIs.

- **[Risk] Reflection-based CustomTypes could load arbitrary assemblies**: `Assembly.Load()` with user-provided assembly names is a security concern if the API is exposed externally.
  → **Mitigation**: The RulesEngine Editor is an internal tool behind authentication. If exposed externally, restrict `CustomTypes` to a pre-approved assembly allowlist.

- **[Risk] Structural comparison may produce false positives/negatives**: Deep JSON comparison with normalization rules (ignore extra properties, array ordering) could miss meaningful differences or flag irrelevant ones.
  → **Trade-off**: Accepted as better than exact string comparison. The Verify method returns a detailed diff structure so consumers can inspect disagreements.

- **[Risk] Dry-run accepts arbitrary JSON that may evaluate against production data**: The `POST /api/rules/dry-run` endpoint accepts facts JSON that the RulesEngine evaluates against. Malformed rules could be computationally expensive.
  → **Mitigation**: Add a configurable timeout for evaluation. Use `CancellationToken` propagation from the HTTP request.

- **[Trade-off] No stored procedure pattern for RulesRepository**: Diverges from the documented architecture pattern.
  → **Accepted**: The existing `UserRepository` already diverges. If the stored procedure pattern is enforced project-wide, `RulesRepository` can be refactored to match.
