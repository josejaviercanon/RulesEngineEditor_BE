## 1. EF Core Entity Configuration & DbContext Updates

- [x] 1.1 Create `Infrastructure/Data/Configurations/WorkflowDefinitionConfiguration.cs` implementing `IEntityTypeConfiguration<WorkflowDefinition>` with JSON column mapping for `JsonContent`, composite unique index on `(WorkflowName, Version)`, default `Status = "Draft"`, and `datetime2` for `CreatedAt`
- [x] 1.2 Create `Infrastructure/Data/Configurations/WorkflowTestScenarioConfiguration.cs` implementing `IEntityTypeConfiguration<WorkflowTestScenario>` with JSON column mapping for `MockInputJson` and `ExpectedOutputJson`, foreign key to `WorkflowDefinitions`, and `datetime2` for `CreatedAt`
- [x] 1.3 Update `Infrastructure/Data/ApplicationDbContext.cs` to add `DbSet<WorkflowDefinition>` and `DbSet<WorkflowTestScenario>` properties, and uncomment/enable `ApplyConfigurationsFromAssembly()` in `OnModelCreating`
- [x] 1.4 Add using directives for the Business entities namespace in `ApplicationDbContext.cs`
- [x] 1.5 Create and apply an EF Core migration for the workflow tables (`Add-Migration AddWorkflowTables` then `Update-Database`)

## 2. Repository Interface & Implementation

- [x] 2.1 Create `Infrastructure/Repositories/IRulesRepository.cs` interface with methods: `Task<List<WorkflowDefinition>> GetAllWorkflowsAsync()`, `Task<WorkflowDefinition?> GetWorkflowByIdAsync(int id)`, `Task<WorkflowTestScenario> SaveScenarioAsync(WorkflowTestScenario scenario)`
- [x] 2.2 Create `Infrastructure/Repositories/RulesRepository.cs` implementing `IRulesRepository`, injecting `ApplicationDbContext` via primary constructor, using `AsNoTracking()` for read queries, ordering workflows by `WorkflowName` ascending then `Version` descending

## 3. Evaluation Service — Core Evaluation Logic

- [x] 3.1 Create `Business/Services/IRulesEvaluationService.cs` interface with methods: `Task<EvaluationResult> EvaluateAsync(string rulesJson, string factsJson, string? settingsJson, string[]? customTypes, CancellationToken ct)`, `VerificationResult Verify(RuleResultTree resultTree, string? expectedOutputJson)`
- [x] 3.2 Create DTO records in `Business/Services/` for request/response: `EvaluationResult` (with `IsSuccess`, `RuleResultTree`, `ErrorMessage`), `VerificationResult` (with `IsMatch`, `IReadOnlyList<Difference> Differences`), `Difference` (with `Path`, `Expected`, `Actual`, `Message`)
- [x] 3.3 Create `Business/Services/RulesEvaluationService.cs` implementing `IRulesEvaluationService`:
  - Parse `RulesJson` to `Workflow[]` using Newtonsoft.Json (project already references it)
  - Parse `SettingsJson` to `ReSettings` with defaults for missing properties
  - Resolve `CustomTypes` via `Type.GetType()` with fallback to assembly scanning; validate each type is a class (not value type)
  - Instantiate new `RulesEngine.RulesEngine(workflows, reSettings)` per call
  - Execute with `ExecuteAllRulesAsync(workflowName, ruleParameters)`
  - Wrap in try-catch: `JsonException` → return error result, `RuleValidationException` → return error result, `Exception` → return error result
- [x] 3.4 Implement `Verify` method: serialize `RuleResultTree` to JSON, parse `ExpectedOutputJson`, perform recursive structural deep-comparison between JToken trees, returning `VerificationResult` with detailed `Difference` list

## 4. Controller API Implementation

- [x] 4.1 Create DTO records for API requests/responses: `DryRunRequest` (with `[Required] RulesJson`, `[Required] FactsJson`, optional `SettingsJson`, optional `CustomTypes[]`), `SaveScenarioRequest` (with `[Required] WorkflowDefinitionId`, `[Required] ScenarioName`, `[Required] MockInputJson`, optional `ExpectedOutputJson`), `WorkflowSummaryResponse` (with `WorkflowDefinitionId`, `WorkflowName`, `Version`, `Status`, `CreatedAt`, `CreatedBy`)
- [x] 4.2 Create `Controllers/RulesController.cs` deriving `ControllerBase` with `[ApiController]`, `[Route("api/[controller]")]`, injecting `IRulesRepository` and `IRulesEvaluationService` via primary constructor
- [x] 4.3 Implement `GET /api/rules` — calls `repository.GetAllWorkflowsAsync()`, maps to `WorkflowSummaryResponse` DTOs, returns `200 OK`
- [x] 4.4 Implement `POST /api/rules/dry-run` — accepts `DryRunRequest`, calls `evaluationService.EvaluateAsync()`, returns `200 OK` with `EvaluationResult` on success, `400 BadRequest` with error details on failure
- [x] 4.5 Implement `POST /api/rules/scenarios` — accepts `SaveScenarioRequest`, maps to `WorkflowTestScenario` entity, calls `repository.SaveScenarioAsync()`, returns `201 Created` with saved entity

## 5. Dependency Injection Registration

- [x] 5.1 Register `IRulesRepository` → `RulesRepository` as scoped in `Program.cs`
- [x] 5.2 Register `IRulesEvaluationService` → `RulesEvaluationService` as scoped in `Program.cs`
- [x] 5.3 Verify `AddControllers()` is already registered (it is) — no additional controller registration needed

## 6. Build, Migration & Smoke Test

- [x] 6.1 Run `dotnet build` and fix any compilation errors (missing usings, namespace issues, type resolution)
- [x] 6.2 Run `dotnet ef migrations add AddWorkflowTables` to generate migration, verify it includes `WorkflowDefinitions` and `WorkflowTestScenarios` with correct JSON column types
- [x] 6.3 Apply migration and verify tables exist in the database with correct schema (`JsonContent` as `json` type, foreign keys, indexes)
- [x] 6.4 Start the application and verify `GET /api/rules` returns HTTP 200 with empty array (no workflows seeded)
- [x] 6.5 Test `POST /api/rules/dry-run` with a minimal valid RulesEngine workflow JSON and facts JSON, verify `200 OK` response with `RuleResultTree`
- [x] 6.6 Test `POST /api/rules/dry-run` with malformed JSON, verify `400 BadRequest` with error message
- [x] 6.7 Test `POST /api/rules/scenarios` with valid test scenario data, verify `201 Created` with generated `ScenarioId`

## 7. Cleanup & Documentation

- [x] 7.1 Verify all new files use file-scoped namespaces consistent with project conventions
- [x] 7.2 Verify no hardcoded business logic in C# classes — all rule logic comes from JSON
- [x] 7.3 Remove or comment out the WeatherForecastController template if still present (per project docs it "will be removed")
