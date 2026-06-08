# Backend Architecture – ASP.NET Core Web API

## Purpose
Provide validation and execution services for workflows defined in Microsoft RulesEngine JSON schema.  
Technologies: ASP.NET Core 10, SQL Server (via Microsoft.Data.SqlClient), `BaseStoredProcedureRepository<TEntity, TKey>` pattern, Microsoft RulesEngine library, xUnit.

---

## Current Project Structure
RulesEngineEditor.Server/
├── RulesEngineEditor.Server.csproj
├── Program.cs
├── Controllers/
│   └── WeatherForecastController.cs
├── Business/
│   ├── Entities/          — Domain models with strongly-typed IDs
│   └── Services/          — Business service interfaces and implementations
├── Infrastructure/
│   ├── Data/              — EF Core DbContext, configurations, interceptors
│   ├── Identity/          — ASP.NET Core Identity + Passkey configuration
│   └── Repositories/      — **Stored procedure repository layer**
│       ├── BaseStoredProcedureRepository.cs
│       ├── IStoredProcedureRepository.cs
│       └── [Entity]/      — Concrete repositories per entity
├── Middleware/
│   └── GlobalExceptionHandler.cs
├── docs/
│   ├── BE.AgentRoles.md
│   ├── BE.Architecture.md
│   ├── BE.DebugGuide.md
│   └── BE.SkillsAudit.md
├── Properties/
├── appsettings.json
├── appsettings.Development.json
└── efpt.config.json        — **Absolute whitelist source of truth**

---

## Source of Truth — efpt.config.json

The file `efpt.config.json` at the project root is the authoritative whitelist for ALL database objects. It is maintained by EF Core Power Tools and serves as:

- **Tables Array**: Every database table the application can interact with
- **StoredProcedures Array**: Every stored procedure available for data access

**Rule**: Any database object NOT declared in `efpt.config.json` does not exist from the application's perspective. AI agents MUST execute Step 0 Pre-Execution Validation before any code modification (see Agent Validation Protocol below).

---

## Stored Procedure Repository Pattern

All data access uses stored procedures exclusively through `BaseStoredProcedureRepository<TEntity, TKey>`:

| Layer | Component | Purpose |
|-------|-----------|---------|
| Interface | `/Infrastructure/Repositories/IStoredProcedureRepository.cs` | Generic CRUD interface (`GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`) |
| Base Class | `/Infrastructure/Repositories/BaseStoredProcedureRepository.cs` | Abstract base implementing CRUD via `EXEC` calls with `.AsNoTracking()` on reads and `ExecuteSqlRawAsync()` on mutations |
| Concrete Repos | `/Infrastructure/Repositories/{Entity}/{Entity}Repository.cs` | Per-entity implementations mapping abstract members to stored procedures |

### Key Constraints
- **Zero Inline SQL**: No `SELECT`, `INSERT`, `UPDATE`, `DELETE` strings in C# code
- **No EF Core Change Tracking**: All `FromSqlRaw` calls use `.AsNoTracking()`; mutations use `ExecuteSqlRawAsync()` directly
- **Controllers inject repository interfaces**, not `ApplicationDbContext`

### Required Stored Procedures Per Entity
| Procedure | Signature |
|-----------|-----------|
| `sp_Insert{Entity}` | Inserts record, returns identity via `@NewId OUTPUT` |
| `sp_Get{Entity}ById` | Fetches single record by primary key |
| `sp_GetAll{Entity}` | Fetches all records |
| `sp_Update{Entity}` | Updates existing record |
| `sp_Delete{Entity}` | Deletes record by primary key |

---

## Agent Validation Protocol

Before ANY database-related code is written, ALL agents MUST execute:

### Step 0: Pre-Execution Validation
1. **Load** `efpt.config.json` from project root
2. **Parse** the `Tables` array — verify target table exists
3. **Parse** the `StoredProcedures` array — verify ALL 5 CRUD procedures
4. **If missing**: Execute Agentic Correction Routine (append to file, halt, prompt operator)
5. **If pass**: Proceed with implementation

---

## Architecture Boundaries

### Identity Domain (ASP.NET Core Managed)
Tables: `AspNetUsers`, `AspNetRoles`, `AspNetRoleClaims`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserRoles`, `AspNetUserTokens`

These tables are managed entirely by Microsoft.AspNetCore.Identity:
- Schema via `ApplicationDbContext` (`IdentityDbContext<IdentityUser>`)
- Data access via `UserManager<T>`, `RoleManager<T>`, `SignInManager<T>`
- Auth flow via bearer tokens, cookies, and Passkey/WebAuthn
- **NO custom repositories** for these entities
- **NO stored procedures** written against Identity tables
- Agents MUST skip AspNet* entities during Step 0 validation

### Business Domain (Stored Procedure Managed)
All non-AspNet entities use `BaseStoredProcedureRepository<TEntity, TKey>`:
- Zero inline SQL — all mutations via stored procedures
- No EF Core change tracking — `.AsNoTracking()` on reads
- Three-layer injection chain: **Controller → Service → Repository**
- Entity models from namespace `RulesEngineEditor.Server.Business.Entities.Models`

### DbContext Architecture
- `ApplicationDbContext` (`Infrastructure/Data/`) — **sole DI-registered runtime context**. Inherits `IdentityDbContext<IdentityUser>`. Used by both Identity (via framework) and repositories (via base class).
- `RulesEngineEditorContext` (`Business/Entities/Models/`) — Power Tools scaffold. Provides entity type shapes and stored procedure interfaces. **NOT registered in DI**.

---

## Planned Architecture (Future State)
WorkflowController.cs, SchemaController.cs, Services/, and additional Repositories/ will be added as workflow features are implemented.

| Component | Purpose | Status |
|-----------|---------|--------|
| `WeatherForecastController.cs` | Template/scaffold controller | Present (will be removed) |
| `WorkflowController.cs` | Endpoints for workflow validation & execution | Planned |
| `SchemaController.cs` | Schema metadata & validation utilities | Planned |
| `RulesEngineService.cs` | Wraps Microsoft RulesEngine library | Planned |
| `ValidationService.cs` | JSON schema checks & error reporting | Planned |
| `IWorkflowRepository / WorkflowRepository` | Stored procedure repository for Workflows | Planned |

---

## Current Configuration
- **CORS**: Policy named `allowAll` — allows any origin (development only; restrict in production)
- **OpenAPI**: Mapped at `/openapi/v1.json` (development only)
- **Scalar UI**: Available at `/scalar/v1` (development only)
- **Database**: SQL Server via `Microsoft.Data.SqlClient` (connection string in `appsettings.json`)
- **HTTPS**: Redirection enabled (`UseHttpsRedirection`)
- **Auth**: ASP.NET Core Identity with bearer token authentication + Passkey/WebAuthn support

---

## API Endpoints (Current)
- `GET /` — Returns "RulesEngine Editor Web API." status message
- `GET /WeatherForecast` — Template endpoint (to be replaced)
- `POST /register` — Register new user (Identity endpoint)
- `POST /login` — Authenticate and receive bearer token (Identity endpoint)
- `POST /api/passkey/register-options` — Create passkey registration challenge (requires Administrator role)
- `POST /api/passkey/register-verify` — Verify and save passkey (requires Administrator role)
- `POST /api/passkey/login-options` — Get passkey login challenge (anonymous)
- `POST /api/passkey/login-verify` — Authenticate via passkey (anonymous)

---

## API Endpoints (Planned)
- `POST /api/workflows/validate` — Validates workflow JSON against RulesEngine schema
- `POST /api/workflows/execute` — Executes workflow (dry-run or real execution)
- `GET /api/workflows/{id}` — Retrieves stored workflow definition via stored procedure

---

## Integration Points
- **UI.React** (planned)  
  - Sends JSON payloads to `/validate` and `/execute`.  
  - Receives validation results and execution outcomes.

- **Shared Libraries**  
  - `RulesEngine` (at `../BE.Libraries/RulesEngine/`) ensures schema compliance across backend and CLI tools.

---

## Testing Strategy
- **Unit Tests (xUnit)**  
  - Validate `RulesEngineService` methods.  
  - Mock repository interfaces for isolated tests (Moq/NSubstitute).

- **Integration Tests (xUnit)**  
  - Use `WebApplicationFactory` to spin up in-memory API.  
  - Use Testcontainers for SQL Server (not EF Core In-Memory, not PostgreSQL).  
  - Repository tests verify stored procedure execution against real SQL Server.

- **API Contract Testing**  
  - Use Playwright's native `APIRequestContext` for headless API contract verification if needed.  
  - For exploratory/manual validation, use Scalar at `/scalar/v1`.

---

## Human-in-the-Loop
- Review stored procedure T-SQL scripts before executing on production database.  
- Approve `efpt.config.json` changes after EF Core Power Tools Refresh.  
- Manually debug RulesEngine exceptions during development.  
- Approve agent-generated backend code via pull requests.  
- Validate API responses against expected schema manually when needed.

