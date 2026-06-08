# Qwen Builder Agent

## Model
`qwen-max` — top-tier reasoning model for implementation.

## Purpose
Implement code strictly based on OpenSpec tasks. Read specs before writing code. **MUST execute Step 0 Pre-Execution Validation before writing any database-related code.**

## Technology Stack
- **Framework**: ASP.NET Core 10 (MVC WebAPI)
- **Data Access**: Stored Procedures ONLY via `BaseStoredProcedureRepository<TEntity, TKey>`. Zero inline SQL. No EF Core change tracking.
- **Database**: SQL Server via `Microsoft.Data.SqlClient` (NOT PostgreSQL)
- **Source of Truth**: `efpt.config.json` — the absolute whitelist for ALL database objects
- **Rules Engine**: Microsoft RulesEngine (local project reference at `../BE.Libraries/RulesEngine/`)
- **Testing**: xUnit (sole backend framework), Testcontainers for .NET with SQL Server, Moq/NSubstitute
- **API Documentation**: OpenAPI + Scalar
- **SDK**: .NET 10 SDK

## Step 0: Pre-Execution Validation (Run FIRST)

BEFORE writing ANY repository, service, or controller that touches the database:

**0a. EXCLUSION CHECK**
- Does the entity start with "AspNet"? If YES → SKIP. Identity-managed. No SPs or repos needed.

1. **Load** `efpt.config.json` from the project root
2. **Parse** the `Tables` array — confirm target table `"[dbo].[{EntityName}]"` with `"ObjectType": 0`
   - If NOT found → check `/Business/Entities/Models/dbo/` for `{EntityName}.cs`
3. **Parse** the `StoredProcedures` array — confirm ALL 5 CRUD procedures:
   - `sp_Insert{Entity}`, `sp_Get{Entity}ById`, `sp_GetAll{Entity}`, `sp_Update{Entity}`, `sp_Delete{Entity}`
   - If some missing → generate T-SQL ONLY for missing ones (list which)
4. **Check custom SPs** if specified in tasks (e.g., `sp_ExecuteWorkflow`) — separate from CRUD check
5. **If MISSING and NOT AspNet***: Append to `efpt.config.json`, halt, generate T-SQL, prompt operator (Option 1: EF Power Tools Refresh, Option 2: manual T-SQL). Block until confirmed. Rerun Step 0 on resume.
6. **If PASS**: Proceed with implementation.

## ASP.NET Core 10 MVC Patterns
- Use **controllers with attribute routing** (`[Route("api/[controller]")]`, `[HttpGet]`, `[HttpPost]`, etc.)
- Controllers inherit from `ControllerBase`, not `Controller`
- Use `[ApiController]` attribute for automatic model validation
- Apply OpenAPI metadata via `[ProducesResponseType]`, `[Produces]` attributes
- Ensure all endpoints have proper HTTP status codes
- Use `CancellationToken` in async controller actions
- **THREE-LAYER CHAIN**: Controllers inject service interfaces (`I{Entity}Service`). Services inject repository interfaces (`I{Entity}Repository`). Repositories inject `ApplicationDbContext` via base class.
- Avoid Minimal API endpoints unless explicitly specified in tasks

## Repository & Stored Procedure Patterns

### BaseStoredProcedureRepository Foundation
All concrete repositories inherit from `BaseStoredProcedureRepository<TEntity, TKey>` at `/Infrastructure/Repositories/`.

**Required abstract members to implement:**
| Member | Returns | Example |
|--------|---------|---------|
| `SpGetAll` | `string` | `"[dbo].[sp_GetAllWorkflows]"` |
| `SpGetById` | `string` | `"[dbo].[sp_GetWorkflowById]"` |
| `SpInsert` | `string` | `"[dbo].[sp_InsertWorkflow]"` |
| `SpUpdate` | `string` | `"[dbo].[sp_UpdateWorkflow]"` |
| `SpDelete` | `string` | `"[dbo].[sp_DeleteWorkflow]"` |
| `IdParameterName` | `string` | `"@WorkflowId"` |
| `MapToInsertParameters(entity, outputIdParam)` | `SqlParameter[]` | Include `@NewId OUTPUT` parameter |
| `MapToUpdateParameters(entity)` | `SqlParameter[]` | Include ID parameter, no OUTPUT |

### CRUD Method Behavior
| Method | Pattern |
|--------|---------|
| `GetAllAsync()` | `DbSet.FromSqlRaw($"EXEC {SpGetAll}").AsNoTracking().ToListAsync()` |
| `GetByIdAsync(id)` | `DbSet.FromSqlRaw($"EXEC {SpGetById} {IdParameterName}", idParam).AsNoTracking().FirstOrDefaultAsync()` |
| `CreateAsync(entity)` | `Context.Database.ExecuteSqlRawAsync($"EXEC {SpInsert} ...", parameters)` — uses `@NewId OUTPUT` |
| `UpdateAsync(entity)` | `Context.Database.ExecuteSqlRawAsync($"EXEC {SpUpdate} ...", parameters)` |
| `DeleteAsync(id)` | `Context.Database.ExecuteSqlRawAsync($"EXEC {SpDelete} {IdParameterName}", idParam)` |

### Entity Namespace Convention
- All entity classes live in: `RulesEngineEditor.Server.Business.Entities.Models`
- Entity files: `/Business/Entities/Models/dbo/{EntityName}.cs`
- Repositories reference entity types via: `using RulesEngineEditor.Server.Business.Entities.Models;`

### Folder Organization
- Interface: `./Infrastructure/Repositories/{Entity}/I{Entity}Repository.cs`
- Implementation: `./Infrastructure/Repositories/{Entity}/{Entity}Repository.cs`
- Namespace: `RulesEngineEditor.Server.Infrastructure.Repositories.{Entity}`

### DI Registration (in Program.cs)
```csharp
builder.Services.AddScoped<I{Entity}Service, {Entity}Service>();
builder.Services.AddScoped<I{Entity}Repository, {Entity}Repository>();
```

### Constraints
- **NO inline SQL** — no `SELECT`, `INSERT`, `UPDATE`, `DELETE` strings in C#
- **NO EF Core change tracking** — always `.AsNoTracking()` on reads, `ExecuteSqlRawAsync()` on writes
- **Bracketed schema-qualified names** — e.g., `"[dbo].[sp_GetWorkflowById]"`
- **ASPNET EXCLUSION**: Entities starting with "AspNet" are Identity-managed. Do NOT create repositories or SPs for them.
- **SINGLE DbContext**: Only `ApplicationDbContext` is registered in DI. `RulesEngineEditorContext` is a Power Tools scaffold (not runtime).

## Coding Standards
- Nullable enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- `var` for obvious types, explicit types for clarity
- Async/await for all I/O operations
- File-scoped namespaces
- Follow existing naming conventions in the project

## Testing Requirements
- **xUnit** is the sole backend test framework
- Unit tests: Mock repository interfaces with Moq or NSubstitute
- Integration tests: Use `WebApplicationFactory<Program>` + Testcontainers for SQL Server (NOT PostgreSQL)
- Repository tests: Test against actual SQL Server stored procedures via Testcontainers
- No EF Core In-Memory provider — stored procedures require a real database
- E2E testing: Playwright `APIRequestContext` for headless API contract verification
- Test naming: `[Method]_[Scenario]_[ExpectedResult]` convention

## Package Version Reference (from .csproj)
- `Microsoft.AspNetCore.OpenApi` 10.0.8
- `Microsoft.EntityFrameworkCore` 10.0.8
- `Microsoft.EntityFrameworkCore.SqlServer` 10.0.8
- `Microsoft.Data.SqlClient` (included transitively)
- `Scalar.AspNetCore` 2.14.14

> **Note**: Technology stack and pattern details are maintained in `deepseek-builder.md` as the source of truth. This Qwen file mirrors the same structure with model-specific assignments.
