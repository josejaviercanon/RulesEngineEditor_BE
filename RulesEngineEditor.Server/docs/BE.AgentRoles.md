# Backend Agent Roles – ASP.NET Core API

## Overview
The backend project validates and executes workflows using Microsoft RulesEngine, with SQL Server stored procedures as the data access layer.
Technologies: ASP.NET Core 10, SQL Server, `BaseStoredProcedureRepository<TEntity, TKey>` pattern, Microsoft RulesEngine library, xUnit.

## Agent Responsibilities

### All Agents — Step 0 Pre-Execution Validation
ALL agents MUST execute the Step 0 validation protocol before any database-related work:
1. Load `efpt.config.json` — the absolute whitelist of database objects
2. Verify target tables exist in the `Tables` array
3. Verify ALL 5 CRUD stored procedures exist in the `StoredProcedures` array
4. If objects are missing: append to `efpt.config.json`, halt, generate T-SQL, prompt operator

### Role-Specific Responsibilities
- **Architect Agent**: Plan features, define service + repository interfaces, design stored procedure signatures (CRUD 5-pack + custom SPs), write OpenSpec specs. Must enforce AspNet* exclusion and three-layer chain. Permissions: `edit=allow` (specs only), `bash=deny`.
- **Builder Agent**: Implement concrete repositories inheriting from `BaseStoredProcedureRepository<TEntity, TKey>`, implement service classes, create stored procedures, wire DI chain (Controller→Service→Repository). Skip AspNet* entities entirely. Permissions: `edit=allow`, `bash=allow`.
- **Reviewer Agent**: Audit stored procedure compliance, verify AspNet* exclusion, confirm three-layer injection chain, check no inline SQL, verify efpt.config.json whitelist adherence. Permissions: `edit=deny`, `bash=deny`.

### Architecture Boundaries — Identity vs Business
- **Identity Domain**: AspNet* tables managed by Microsoft.AspNetCore.Identity. Accessed via `UserManager`, `RoleManager`, `SignInManager`. NO repositories, NO stored procedures.
- **Business Domain**: All other tables use `BaseStoredProcedureRepository<TEntity, TKey>` with stored procedures only. Three-layer chain: Controller→Service→Repository.

### Entity Namespace Convention
All entity classes live in namespace:
```
RulesEngineEditor.Server.Business.Entities.Models
```
Physical location: `/Business/Entities/Models/dbo/{EntityName}.cs`

This namespace provides:
- C# type shapes for table schemas (column names, types, nullability)
- The structural blueprint for T-SQL generation and parameter mapping
- The authoritative source used by `ApplicationDbContext` and all repositories

## Agent Model Assignments

### Deepseek Models
- **Architect** → `deepseek/deepseek-v4-flash` (fast, cheap model for planning)
  - Permissions: `edit=allow`, `bash=deny` (writes spec files only)
- **Builder** → `deepseek/deepseek-v4-pro` (top-tier coding model)
  - Permissions: `edit=allow`, `bash=allow` (writes code, runs tests)
- **Reviewer** → `deepseek/deepseek-v4-pro` (high-reasoning model for audits)
  - Permissions: `edit=deny`, `bash=deny` (strictly read-only)

### Qwen Models
- **Architect** → `qwen/qwen-plus` (fast, cost-effective model for planning)
  - Permissions: `edit=allow`, `bash=deny` (writes spec files only)
- **Builder** → `qwen/qwen-max` (top-tier coding model)
  - Permissions: `edit=allow`, `bash=allow` (writes code, runs tests)
- **Reviewer** → `qwen/qwen-max` (high-reasoning model for audits)
  - Permissions: `edit=deny`, `bash=deny` (strictly read-only)

## Agent Skill Files
Detailed agent-specific context files are maintained in `.agents/skills/`:
- `.agents/skills/deepseek-architect.md` — architecture context for Deepseek planning agent
- `.agents/skills/deepseek-builder.md` — repository + stored procedure patterns for Deepseek implementation agent
- `.agents/skills/deepseek-reviewer.md` — stored procedure compliance checklist for Deepseek auditing agent
- `.agents/skills/qwen-architect.md` — architecture context for Qwen planning agent
- `.agents/skills/qwen-builder.md` — repository + stored procedure patterns for Qwen implementation agent
- `.agents/skills/qwen-reviewer.md` — stored procedure compliance checklist for Qwen auditing agent

## Agentic Correction Routine
When Step 0 validation fails, agents follow this deterministic procedure:
1. Append missing table/procedure definitions to `efpt.config.json`
2. HALT all further execution
3. Generate T-SQL `CREATE PROCEDURE` script
4. Present operator with: Option 1 (EF Power Tools Refresh) or Option 2 (manual T-SQL execution)
5. BLOCK until operator confirms completion
6. Rerun Step 0 on resume

## Human Revision Points
- Review stored procedure T-SQL scripts before executing on production database.
- Approve `efpt.config.json` changes after EF Core Power Tools Refresh.
- Debug RulesEngine exceptions during development.
- Validate API responses with Scalar at `/scalar/v1` or Postman.

## Debug Guide
- Run solution with `dotnet run` or `dotnet watch`.
- Use Visual Studio 2026 breakpoints in controllers/repositories.
- Debug stored procedures directly in SSMS or Azure Data Studio.
- Use Scalar at `/scalar/v1` for API exploration in development mode.
