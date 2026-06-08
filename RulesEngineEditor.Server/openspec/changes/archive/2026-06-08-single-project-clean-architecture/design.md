## Context

This design covers a production-ready Single-Project Clean Architecture template for ASP.NET Core MVC Web API targeting .NET 10. The template enforces Domain-Driven Design (DDD) boundaries within a single project using root-level folder separation (`/Business`, `/Infrastructure`, `/Controllers`) rather than multi-project solutions. The driving requirement is to provide teams with a reference architecture that combines traditional MVC API Controllers with domain service orchestration, native ASP.NET Core Identity with Passkey/WebAuthn, and EF Core 10 with SQL Server — without the overhead of solution-level project references.

The template must be created as additive scaffolding within the existing `RulesEngineEditor.Server` project and serve as a reusable reference for future API development.

## Goals / Non-Goals

**Goals:**

- Establish a root-level folder layout (`/Business`, `/Infrastructure`, `/Middleware`, `/Controllers`) that enforces Clean Architecture dependency rules (Business → nothing, Infrastructure → Business, Controllers → Business).
- Provide rich domain entities in `/Business/Entities` with private setters, strongly-typed IDs, and embedded business rules.
- Implement domain services in `/Business/Services` as interface/implementation pairs operating across entities.
- Configure EF Core 10 `ApplicationDbContext` with SQL Server provider, entity configurations, and save interceptors in `/Infrastructure/Data`.
- Wire native .NET 10 ASP.NET Core Identity with Passkey/WebAuthn via `IdentityPasskeyOptions`, exposed as Web API endpoints.
- Implement global error handling using .NET 10 `IExceptionHandler` in `/Middleware`.
- Deliver a fully implemented "Products" feature (Entity → Services → EF Config → Controller) as a working reference.
- Use traditional MVC API Controllers (`ControllerBase`, `[ApiController]`) — no Minimal APIs.
- Use modern C# features: file-scoped namespaces, `required` properties, primary constructors, `record` types for request DTOs.

**Non-Goals:**

- NOT a multi-project solution — everything lives in one `.csproj`.
- NOT Minimal APIs — all endpoints are traditional MVC Controller actions.
- NOT a full production deployment — no Docker, CI/CD, or Azure deployment configuration.
- NOT a comprehensive Identity UI — only the Identity configuration and API endpoint mapping, not Razor Pages.
- NOT unit or integration tests — test projects are out of scope for this template.

## Decisions

### Decision 1: Single-Project over Multi-Project Clean Architecture

**Context**: Traditional Clean Architecture guidance recommends separate projects for Domain, Application, Infrastructure, and Presentation to enforce compile-time dependency inversion. Multi-project solutions introduce solution-level complexity, longer build times, and NuGet package versioning overhead.

**Decision**: Use root-level folder boundaries within a single ASP.NET Core Web API project. Dependency rules are enforced through convention, code analysis (SonarQube/Roslyn analyzers), and code review rather than project references.

**Rationale**: The target audience is teams who want Clean Architecture principles without multi-project overhead. Single-project reduces cognitive load for new teams, simplifies refactoring, and avoids the "dependency hell" of cross-project versioning. The folder structure mirrors multi-project naming conventions, making extraction to separate projects straightforward if needed later.

**Alternatives Considered**:
- **Multi-project solution**: Rejected for the reasons above — this template is opinionated about simplicity.
- **One folder per layer with feature folders inside**: Rejected — feature folders (e.g., `Products/Business`, `Products/Infrastructure`) obscure the layer boundaries that Clean Architecture depends on.

### Decision 2: Traditional MVC API Controllers over Minimal APIs

**Context**: .NET 6+ introduced Minimal APIs as a lightweight alternative to MVC Controllers. Minimal APIs reduce boilerplate but lack built-in support for filters, model binding attributes, and controller-level organization.

**Decision**: Use `ControllerBase`-derived classes with `[ApiController]` and action method attributes (`[HttpGet]`, `[HttpPost]`, etc.).

**Rationale**: The template targets teams building production APIs where controller-level authorization, model validation, OpenAPI/Scalar metadata, and action filters are critical. MVC Controllers provide self-documenting action signatures, built-in model binding with `[FromBody]`/`[FromRoute]`, and easier organization by resource. This aligns with enterprise conventions.

### Decision 3: Domain Service Orchestration over CQRS

**Context**: CQRS with a message bus (e.g., MediatR) adds indirection for simple CRUD operations. Commands, validators, and handlers spread business logic across multiple types even for straightforward create/update operations.

**Decision**: Controllers inject domain services directly from `/Business/Services`. Each service is an interface/implementation pair (e.g., `IProductService` / `ProductService`) with methods that accept strongly-typed request DTOs and return results. Request validation uses `[ApiController]` automatic model validation with `[Required]`, `[StringLength]`, and other data annotation attributes on the DTOs.

**Rationale**: Direct service invocation eliminates the MediatR infrastructure overhead (pipeline behaviors, handler registration, request/response types) while preserving Clean Architecture boundaries. Controllers remain thin — they validate the HTTP request via model binding, call one service method, and return the response. Business logic stays in the service layer where it belongs.

**Alternatives Considered**:
- **CQRS with MediatR**: Rejected — the indirection is unnecessary for this template's scope. Teams can add MediatR later if they need cross-cutting pipeline behaviors.
- **Logic in controllers**: Rejected — violates separation of concerns and makes testing harder.

### Decision 4: Native ASP.NET Core Identity with Passkey/WebAuthn

**Context**: .NET 10 introduces native Passkey/WebAuthn support via `IdentityPasskeyOptions`. Previously, WebAuthn required third-party libraries.

**Decision**: Use the built-in `Microsoft.AspNetCore.Identity` with `AddIdentityApiEndpoints()` and configure Passkey support through `IdentityPasskeyOptions`. Map Identity endpoints as Web API routes.

**Rationale**: Eliminates third-party dependencies for authentication. The native implementation is maintained by Microsoft, receives security patches automatically, and follows .NET 10 conventions. This aligns with the template's philosophy of minimizing external dependencies.

### Decision 5: EF Core 10 with SQL Server and Entity Configurations

**Context**: EF Core supports multiple configuration styles: data annotations, fluent API in `OnModelCreating`, and separate `IEntityTypeConfiguration<T>` classes.

**Decision**: Use `Microsoft.EntityFrameworkCore.SqlServer` as the database provider. Use separate `IEntityTypeConfiguration<T>` classes in `/Infrastructure/Data/Configurations` applied via `modelBuilder.ApplyConfigurationsFromAssembly()`.

**Rationale**: SQL Server is the standard enterprise database on Azure and on-premises. Using `IEntityTypeConfiguration<T>` classes keeps entity configuration out of the DbContext and domain entities. Configurations are auto-discovered, making the pattern extensible. Fluent API is preferred over data annotations to keep entities free of persistence concerns.

### Decision 6: Entity-Level Strongly-Typed IDs

**Context**: DDD recommends strongly-typed IDs (e.g., `ProductId` instead of `Guid` or `int`) to prevent primitive obsession and ID confusion across aggregates.

**Decision**: Each aggregate root gets a `record struct` or `readonly record struct` ID type (e.g., `public readonly record struct ProductId(Guid Value)`). EF Core 10 value converters map these to/from the database.

**Rationale**: Compile-time type safety prevents passing a `CustomerId` where a `ProductId` is expected. `readonly record struct` provides zero-overhead value semantics. EF Core value converters handle persistence transparently.

## Risks / Trade-offs

- **[Risk] Single-project boundaries are convention-based, not compiler-enforced**: A developer could accidentally reference EF Core from a Business entity.
  → **Mitigation**: Add a Roslyn analyzer or architecture test (e.g., NetArchTest) in CI that enforces no references from `/Business` to `/Infrastructure` or `Microsoft.EntityFrameworkCore`.

- **[Risk] .NET 10 Identity Passkey support is new and may have breaking changes**: As a preview/early-release feature, the API surface may change.
  → **Mitigation**: Isolate Identity configuration into `/Infrastructure/Identity` so that upgrade impacts are contained to one folder.

- **[Risk] Strongly-typed IDs add complexity for simple CRUD operations**: Value converters and custom ID types increase boilerplate.
  → **Trade-off**: Accepted in exchange for domain clarity. This template is opinionated toward DDD; teams building simple CRUD APIs can opt for primitive IDs.

- **[Risk] Single-project doesn't scale to very large codebases** (100+ entities).
  → **Trade-off**: This template targets small-to-medium APIs. Teams outgrowing it can extract `/Business` and `/Infrastructure` into separate projects using the existing folder structure as the project boundary.
