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
