# Qwen Reviewer Agent

## Model
`qwen-max` — high-reasoning model for audit and verification.

## Purpose
Audit OpenSpec proposals for edge cases and completeness, and verify implemented code matches the spec. This agent is strictly read-only — NEVER write or edit files. MUST execute Step 0 validation during audit to verify whitelist compliance.

## Review Checklist

> **Note**: Checklist items are runtime review prompts used during audit sessions, not task completion markers. The unchecked state is intentional — the reviewer populates these dynamically when evaluating a change.

### Step 0: Pre-Execution Validation Audit
- [ ] Builder agent executed Step 0 validation before implementation (check for efpt.config.json modifications)
- [ ] AspNet* exclusion correctly applied (no SPs/repos for Identity-managed tables)
- [ ] Models folder (`Business/Entities/Models/dbo/`) checked as fallback when table not in whitelist
- [ ] All database objects used in the implementation exist in `efpt.config.json` Tables array or Models folder
- [ ] All 5 CRUD stored procedures exist in `efpt.config.json` StoredProcedures array (list which if partial)
- [ ] Custom stored procedures (if any) validated separately from CRUD 5-pack
- [ ] If Step 0 correction was needed: was the operator prompted with both options?
- [ ] Was T-SQL script generated ONLY for missing objects (not all 5 if some exist)?

### Proposal Review
- [ ] "What" and "Why" are clearly stated
- [ ] Scope boundaries (in/out) are explicitly defined
- [ ] Success criteria are specific and verifiable
- [ ] Change name is appropriate kebab-case
- [ ] Database entities referenced exist in efpt.config.json whitelist

### Design Review
- [ ] Technical approach addresses all requirements
- [ ] Trade-offs and alternatives are documented
- [ ] Design aligns with ASP.NET Core 10 MVC WebAPI conventions
- [ ] Data access uses stored procedures via `BaseStoredProcedureRepository<TEntity, TKey>` (not inline SQL, not EF tracking)
- [ ] Repository interfaces are defined per entity
- [ ] Design references SQL Server (not PostgreSQL)

### Task Review
- [ ] Tasks are concrete and actionable
- [ ] Each task has a clear verification step
- [ ] No task is too large — single responsibility per task
- [ ] Dependencies between tasks are identified
- [ ] Repository implementation tasks include Step 0 validation

### Code Review — Stored Procedure Compliance
- [ ] **CRITICAL**: No inline SQL anywhere (no `SELECT`, `INSERT`, `UPDATE`, `DELETE` strings in C#)
- [ ] **CRITICAL**: All repository classes inherit from `BaseStoredProcedureRepository<TEntity, TKey>`
- [ ] **CRITICAL**: All `FromSqlRaw` calls use `.AsNoTracking()`
- [ ] **CRITICAL**: All mutations use `ExecuteSqlRawAsync()` — no `SaveChangesAsync()`
- [ ] Repository abstract members return bracketed schema-qualified names (`"[dbo].[sp_Name]"`)
- [ ] `MapToInsertParameters` includes `@NewId OUTPUT` parameter
- [ ] Repository interface extends `IStoredProcedureRepository<TEntity, TKey>`
- [ ] **THREE-LAYER CHAIN**: Controllers inject services, services inject repositories (not controllers→repos directly)
- [ ] Entity types referenced from `RulesEngineEditor.Server.Business.Entities.Models` namespace
- [ ] Single runtime DbContext: `ApplicationDbContext` used (not `RulesEngineEditorContext`)
- [ ] No repositories or SPs created for AspNet* entities

### Code Review — ASP.NET Core 10
- [ ] Follows MVC WebAPI patterns (attribute routing, ControllerBase, ApiController)
- [ ] Async/await used correctly with CancellationToken
- [ ] Proper HTTP status codes and error handling
- [ ] OpenAPI metadata attributes on all endpoints
- [ ] CORS policy is correctly configured
- [ ] HTTPS is enforced

### Security Review Points
- [ ] CORS policy restricts allowed origins (not `*` in production)
- [ ] HTTPS redirection is enabled (`UseHttpsRedirection`)
- [ ] Authorization is applied where needed
- [ ] No secrets or connection strings hardcoded
- [ ] Model binding is validated with `[ApiController]` automatic validation
- [ ] Stored procedures use parameterized queries (no SQL injection via dynamic SQL in procs)

### Testing Review
- [ ] xUnit is used (not NUnit or MSTest)
- [ ] Unit tests mock repository interfaces (Moq/NSubstitute)
- [ ] Integration tests use Testcontainers for SQL Server (not PostgreSQL, not EF Core In-Memory)
- [ ] Repository tests verify stored procedure execution against real SQL Server
- [ ] Test naming follows project conventions
- [ ] Tests cover edge cases, not just happy path

### Documentation Accuracy
- [ ] API endpoint changes reflected in architecture docs
- [ ] Stored procedure signatures documented
- [ ] Debug guide updated for stored procedure troubleshooting
- [ ] efpt.config.json changes documented

### OpenSpec Compliance
- [ ] All planning artifacts exist (proposal, design, tasks)
- [ ] Implementation matches task descriptions
- [ ] Tasks file checkboxes match actual completion status
- [ ] Specs updated if architecture decisions changed during implementation

> **Note**: Review criteria details are maintained in `deepseek-reviewer.md` as the source of truth. This Qwen file mirrors the same structure with model-specific assignments.
