## Why

The RulesEngine Editor currently has no backend API to dynamically store, retrieve, and execute rules workflows. The Microsoft RulesEngine v6.0 library is already referenced in the project but completely unused. Teams need a stateless evaluation service that loads workflow JSON from SQL Server at runtime and executes it without requiring backend recompilation — enabling rapid rule iteration and testing without deployment cycles. This capability is foundational for the RulesEngine Lab feature where business users can author, validate, and dry-run rules before promoting them to production.

## What Changes

- **New `IRulesRepository` interface and EF Core implementation**: Provides data access for `WorkflowDefinitions` and `WorkflowTestScenarios` tables, enabling CRUD operations on stored workflows and their test scenarios.
- **New `IRulesEvaluationService` and `RulesEvaluationService`**: A stateless service that instantiates a fresh `Microsoft.RulesEngine.RulesEngine` instance per evaluation request using provided workflow JSON and `ReSettings`. Supports workflow execution, nested workflows, rule actions (Success/Failure), LINQ-based expressions, and `RuleResultTree` inspection. Includes a `Verify` method that performs structural deep-comparison between `RuleResultTree` output and expected JSON.
- **New `RulesController`**: Exposes `GET /api/rules` (list all workflows), `POST /api/rules/dry-run` (execute rules against provided facts/settings), and `POST /api/rules/scenarios` (save test inputs and expected outputs).
- **Dynamic `CustomTypes` support**: Allows API consumers to register custom types via reflection for use in rule expressions, avoiding hardcoded business logic in C# classes.
- **Robust error handling**: Malformed JSON, invalid workflow definitions, and runtime evaluation errors are caught and returned as structured problem details.
- **EF Core configuration for workflow tables**: Entity configurations for `WorkflowDefinitions` (with JSON column mapping) and `WorkflowTestScenarios` using `IEntityTypeConfiguration<T>` classes.
- **Adds `WorkflowDefinitions` and `WorkflowTestScenarios` to `ApplicationDbContext`**: These tables exist as Power Tools scaffold models but are not yet part of the runtime EF Core context or migrations.

## Capabilities

### New Capabilities

- `rules-engine-repository`: Data access layer for workflow definitions and test scenarios — includes `IRulesRepository` interface, EF Core implementation, and entity configurations with JSON column mapping for `JsonContent`, `MockInputJson`, and `ExpectedOutputJson` fields.
- `rules-engine-evaluation`: Stateless evaluation service that instantiates `Microsoft.RulesEngine.RulesEngine` per request with provided workflow JSON and `ReSettings`. Supports nested workflows, rule actions, LINQ expressions, `RuleResultTree` inspection, dynamic `CustomTypes`, and structural output verification.
- `rules-engine-api`: REST API endpoints — `GET /api/rules`, `POST /api/rules/dry-run`, and `POST /api/rules/scenarios` — with proper HTTP semantics, model validation, and error handling.
- `rules-engine-verification`: Deep comparison logic that verifies `RuleResultTree` output against `ExpectedOutputJson` for test scenario validation, returning structured pass/fail results with diffs.

### Modified Capabilities

*(None — this is entirely new functionality. No existing specs change their requirements.)*

## Impact

- **New files in `Infrastructure/Repositories/`**: `IRulesRepository`, `RulesRepository` implementation
- **New files in `Infrastructure/Data/Configurations/`**: `WorkflowDefinitionConfiguration`, `WorkflowTestScenarioConfiguration` (populates currently empty folder)
- **New files in `Business/Services/`**: `IRulesEvaluationService`, `RulesEvaluationService` with `Evaluate` and `Verify` methods
- **New files in `Controllers/`**: `RulesController`
- **Modified `Infrastructure/Data/ApplicationDbContext.cs`**: Added `DbSet<WorkflowDefinition>` and `DbSet<WorkflowTestScenario>` properties; `OnModelCreating` updated to apply configurations
- **Modified `Program.cs`**: Register `IRulesRepository`, `IRulesEvaluationService`, and `RulesController` in DI
- **Dependencies**: No new NuGet packages needed — `Microsoft.RulesEngine` v6.0 is already referenced
- **Database**: Workflow tables already exist in the schema (via EF Power Tools models); an EF Core migration will be needed to register them with `ApplicationDbContext`
- **No breaking changes** to existing controllers, services, or endpoints
