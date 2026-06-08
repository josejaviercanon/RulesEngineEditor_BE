# Project Structure — Delta Specification

## Purpose

This delta specification amends the existing `project-structure/spec.md` to add the `/Infrastructure/Repositories` folder structure, stored-procedure-only data access constraint, no-EF-tracking rule, and whitelist validation requirement.

## Amendments

### Amendment 1: Add Repository Folder Structure

The following entry SHALL be added to the Single-Project Clean Architecture folder layout in Requirement: "Single-Project Clean Architecture folder layout":

- `/Infrastructure/Repositories/[Entity]/` — Concrete stored procedure repositories organized by entity, each inheriting from `BaseStoredProcedureRepository<TEntity, TKey>`. Each entity subfolder contains:
  - `I{Entity}Repository.cs` — Interface extending `IStoredProcedureRepository<TEntity, TKey>`
  - `{Entity}Repository.cs` — Concrete implementation

### Amendment 2: Add Stored-Procedure-Only Data Access Constraint

A new requirement SHALL be added:

### Requirement: Stored Procedure Repository Pattern — Data Access

ALL data access operations that mutate or query database records SHALL use stored procedures exclusively via `BaseStoredProcedureRepository<TEntity, TKey>`. The following rules SHALL apply:

#### Scenario: Inline SQL is forbidden
- **WHEN** inspecting any `.cs` file in the project
- **THEN** it SHALL NOT contain embedded `SELECT`, `INSERT`, `UPDATE`, `DELETE`, or raw SQL strings for database operations
- **AND** all database operations SHALL execute through calls to stored procedures

#### Scenario: EF Core change tracking is disabled for repository operations
- **WHEN** a repository method queries data
- **THEN** it SHALL use `.AsNoTracking()` on all `FromSqlRaw` queries
- **WHEN** a repository method mutates data
- **THEN** it SHALL use `Context.Database.ExecuteSqlRawAsync()` directly, bypassing the EF Core change tracker

#### Scenario: Controllers depend on service interfaces, not DbContext or repositories
- **WHEN** a Controller class has a data dependency
- **THEN** it SHALL depend on a service interface (e.g., `IWorkflowService`), NOT on `ApplicationDbContext` or a repository interface directly
- **AND** the service SHALL depend on the repository interface (e.g., `IWorkflowRepository`)
- **AND** the Controller SHALL NOT have a `using` directive for `Microsoft.EntityFrameworkCore` or `Infrastructure.Repositories`

### Amendment 3: Add Whitelist Validation Requirement

A new requirement SHALL be added:

### Requirement: efpt.config.json — Absolute Source of Truth

The `efpt.config.json` file SHALL serve as the authoritative whitelist for all database objects. ALL agents SHALL execute the Step 0 Pre-Execution Validation protocol before modifying any database-related code.

#### Scenario: Every agent verifies the whitelist before acting
- **WHEN** an agent begins a task that involves database objects
- **THEN** it SHALL load `efpt.config.json`, parse the `Tables` and `StoredProcedures` arrays, and verify all required objects exist
- **AND** if objects are missing, it SHALL execute the Agentic Correction Routine defined in the `agent-validation-protocol` specification

### Amendment 4: Update Folder Structure Diagram

The folder structure diagram in Requirement: "Single-Project Clean Architecture folder layout" SHALL be updated to include:

```
/Infrastructure/Repositories/
├── BaseStoredProcedureRepository.cs
├── IStoredProcedureRepository.cs
├── Workflows/
│   ├── IWorkflowRepository.cs
│   └── WorkflowRepository.cs
└── Rules/
    ├── IRuleRepository.cs
    └── RuleRepository.cs
```
