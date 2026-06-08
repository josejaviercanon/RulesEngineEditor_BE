## Why

Modern .NET projects targeting .NET 10 lack a production-ready, opinionated single-project Clean Architecture template that enforces DDD boundaries without the overhead of multi-project solutions. Teams need a reference architecture that combines traditional MVC API Controllers with service orchestration, native ASP.NET Core Identity with Passkey/WebAuthn, and EF Core 10 with SQL Server — all within a single, well-organized Web API project. This change establishes that template with full code stubs and a concrete "Products" feature as an implementation guide.

## What Changes

- **Single-Project Clean Architecture layout**: Root-level `/Business`, `/Infrastructure`, `/Controllers` folders enforcing DDD layer boundaries inside a single ASP.NET Core MVC Web API project.
- **Traditional MVC API Controllers**: Standard `ControllerBase`-derived controllers with `[ApiController]` and `[Authorize]` attributes — no Minimal APIs.
- **Domain service orchestration**: Controllers inject business services directly from `/Business/Services`, keeping controllers thin and logic in domain services.
- **Rich domain entities**: Entities in `/Business/Entities` with private setters, strongly-typed IDs, and embedded business rules.
- **Domain services**: Business logic operating across entities in `/Business/Services` via interface/implementation pairs.
- **EF Core 10 with SQL Server**: `ApplicationDbContext` with entity configurations and interceptors in `/Infrastructure/Data`, using `Microsoft.EntityFrameworkCore.SqlServer`.
- **Native .NET 10 Identity with Passkey/WebAuthn**: ASP.NET Core Identity endpoints mapped via Web API infrastructure, using native `IdentityPasskeyOptions` — no third-party packages.
- **Global error handling**: .NET 10 `IExceptionHandler` middleware in `/Middleware`.
- **Concrete "Products" feature**: Fully implemented Product entity, price service, product service orchestration, EF Core configuration, and `ProductsController` with an `[Authorize]` POST endpoint.

## Capabilities

### New Capabilities
- `project-structure`: Single-project Clean Architecture folder layout with `/Business`, `/Infrastructure`, `/Controllers` boundaries under a .NET 10 ASP.NET Core MVC Web API project.
- `products-feature`: Complete "Products" feature implementation spanning all layers — rich entity, domain service, product service, EF Core config, and API controller.
- `identity-authentication`: Native .NET 10 ASP.NET Core Identity with Passkey/WebAuthn configuration via `IdentityPasskeyOptions`, mapped as Web API endpoints.

### Modified Capabilities

*(None — this is a new template, no existing specs to modify.)*

## Impact

- **New project template** will be created within the existing `RulesEngineEditor.Server` solution or generated as a standalone reference template.
- **Dependencies added**: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (native), `Microsoft.AspNetCore.Authentication.JwtBearer` (native).
- **No breaking changes** to existing code — this is additive scaffolding.
- **Documentation**: Architecture decisions and patterns will be documented in the design artifact for downstream teams to adopt.
