# Project Structure

## Purpose

Defines the single-project Clean Architecture folder layout, modern C# language conventions, strongly-typed ID patterns, and dependency injection wiring standards for .NET 10 ASP.NET Core MVC Web API projects.

## Requirements

### Requirement: Single-Project Clean Architecture folder layout

The template SHALL organize source code using root-level folder boundaries that enforce DDD layer separation within a single .NET 10 ASP.NET Core MVC Web API project. The folder structure SHALL be:

- `/Business/Entities` — Rich domain models with private setters, strongly-typed IDs, and embedded business rules.
- `/Business/Services` — Business/domain service interfaces and implementations operating across multiple entities. Includes aggregate service interfaces such as `IProductService` for cross-entity orchestration.
- `/Infrastructure/Abstractions` — Implementations of external system interfaces (e.g., file storage, email, time providers).
- `/Infrastructure/Data` — EF Core `DbContext`, `IEntityTypeConfiguration<T>` classes, and save interceptors. Uses `Microsoft.EntityFrameworkCore.SqlServer`.
- `/Infrastructure/Identity` — ASP.NET Core Identity configuration, Passkey/WebAuthn setup via `IdentityPasskeyOptions`.
- `/Infrastructure/Repositories/[Entity]/` — Concrete stored procedure repositories organized by entity, each inheriting from `BaseStoredProcedureRepository<TEntity, TKey>`. Each entity subfolder contains:
  - `I{Entity}Repository.cs` — Interface extending `IStoredProcedureRepository<TEntity, TKey>`
  - `{Entity}Repository.cs` — Concrete implementation
- `/Middleware` — Global error handling via .NET 10 `IExceptionHandler`.
- `/Controllers` — Traditional MVC API Controllers inheriting from `ControllerBase`.

#### Scenario: Business layer has zero framework dependencies
- **WHEN** a developer inspects any file under `/Business/Entities` or `/Business/Services`
- **THEN** the file SHALL NOT contain `using` directives referencing `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.Mvc`, or any Infrastructure-layer namespace

#### Scenario: Controllers only reference Business layer
- **WHEN** a Controller action method processes a request
- **THEN** it SHALL invoke service interfaces from `/Business/Services` and SHALL NOT directly reference `ApplicationDbContext` or EF Core types

#### Scenario: Infrastructure depends on Business abstractions
- **WHEN** an Infrastructure class (e.g., repository, service implementation) is created
- **THEN** it SHALL implement an interface defined in `/Business/Services` or `/Business/Abstractions`

#### Scenario: Repository folder structure follows convention
- **WHEN** a concrete repository is created for the `Workflow` entity
- **THEN** it SHALL be placed at `./Infrastructure/Repositories/Workflows/WorkflowRepository.cs`
- **AND** the folder name SHALL be the pluralized entity name

The `/Infrastructure/Repositories/` folder structure SHALL be:

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

### Requirement: efpt.config.json — Absolute Source of Truth

The `efpt.config.json` file SHALL serve as the authoritative whitelist for all database objects. ALL agents SHALL execute the Step 0 Pre-Execution Validation protocol before modifying any database-related code.

#### Scenario: Every agent verifies the whitelist before acting
- **WHEN** an agent begins a task that involves database objects
- **THEN** it SHALL load `efpt.config.json`, parse the `Tables` and `StoredProcedures` arrays, and verify all required objects exist
- **AND** if objects are missing, it SHALL execute the Agentic Correction Routine defined in the `agent-validation-protocol` specification

### Requirement: Modern C# language features

All code files in the template SHALL use C# 12+ language features exclusively:

- File-scoped namespaces (`namespace X.Y;` — no braces)
- `required` properties on request DTOs and configuration models
- Primary constructors on services, controllers, and configurations
- `record` types for immutable request DTOs
- `readonly record struct` for strongly-typed IDs
- Collection expressions (`[]` instead of `new List<T>()`)

#### Scenario: All files use file-scoped namespaces
- **WHEN** any `.cs` file is created in the template
- **THEN** it SHALL use file-scoped namespace syntax without braces

#### Scenario: Request DTOs are immutable record types
- **WHEN** a request DTO is defined for a controller action (e.g., `CreateProductRequest`)
- **THEN** it SHALL be declared as `public record` with data annotation validation attributes

### Requirement: Strongly-typed IDs for aggregate roots

Each aggregate root entity SHALL define a `readonly record struct` strongly-typed ID. EF Core 10 SHALL use value converters to map these IDs to/from database columns.

#### Scenario: Entity ID is a distinct type, not a primitive
- **WHEN** an entity's identity is accessed
- **THEN** it SHALL be of the entity's ID type (e.g., `ProductId`), not `Guid` or `int` directly

#### Scenario: EF Core persists strongly-typed IDs via value converter
- **WHEN** a new entity is saved to the database
- **THEN** the strongly-typed ID SHALL be converted to its underlying primitive (`Guid`, `int`, etc.) by a registered EF Core value converter

### Requirement: Dependency injection wiring

All services, controllers, and DbContext SHALL be registered via .NET's built-in `IServiceCollection` extensions in `Program.cs` or via convention-based registration.

#### Scenario: Business services are DI-registered
- **WHEN** the application starts
- **THEN** each interface-implementation pair in `/Business/Services` SHALL be registered in DI with the appropriate lifetime (Scoped for stateful services, Singleton for stateless services)

#### Scenario: DbContext is registered with SQL Server provider
- **WHEN** the application starts
- **THEN** `ApplicationDbContext` SHALL be registered with `UseSqlServer()` and the connection string from configuration
