## Context

The project currently has an established `BaseStoredProcedureRepository<TEntity, TKey>` infrastructure and an `efpt.config.json` whitelist, but all agent documentation (skill files, backend docs) still references the old EF Core + PostgreSQL + LINQ paradigm. This mismatch causes agents to generate non-compliant code that must be manually corrected.

The work is organized into four parallel workstreams:
1. **Spec creation** — formalize the agent validation protocol and stored procedure repository pattern as OpenSpec documents
2. **Agent skill file updates** — update all 6 agent skill files (Deepseek + Qwen) to reflect the actual architecture
3. **Backend documentation updates** — update `BE.Architecture.md`, `BE.AgentRoles.md`, `BE.DebugGuide.md`, `BE.SkillsAudit.md`
4. **Cross-cutting optimization for agents** — restructure all documentation to be deterministic, machine-parseable, with numbered steps and explicit checklists

## Goals / Non-Goals

**Goals:**
- Formalize the Agent Validation Protocol (Step 0) as a formal OpenSpec specification
- Formalize the Stored Procedure Repository Pattern as a formal OpenSpec specification
- Update the `project-structure` spec to include repository folder structure
- Update all 6 agent skill files to reflect stored-procedure-only, SQL Server, efpt.config.json whitelist
- Update all 4 backend documentation files to match the current architecture
- Restructure documentation for deterministic agent parsing (numbered steps, checklists, JSON-parsing procedures)
- Optimize OpenSpec workflow sections for agent execution — clear entry point, step order, halt conditions

**Non-Goals:**
- No source code changes to repositories, controllers, services, or entities
- No changes to `efpt.config.json` content (that is handled by the Agentic Correction Routine)
- No changes to `opencode.json` agent model assignments
- No changes to `.csproj` packages
- No creation of new agent skill files (only updating existing 6)
- No changes to global `docs/` repo-root documentation (only backend `docs/BE.*.md` files)

## Decisions

### Decision 1: Spec-Driven Documentation for Agent Optimization — Not Free-Form

**Context**: Agent documentation could be written in free-form prose, which relies on the LLM's ability to extract relevant constraints from narrative text. This is fragile — agents may miss critical constraints buried in paragraphs.

**Decision**: All agent-facing documentation (skill files, backend docs, specs) SHALL use deterministic structures:
- **Numbered steps** for procedures (e.g., "Step 0.1: Load efpt.config.json")
- **Tables** for parameter/property mappings
- **Checkboxes** for review criteria
- **JSON code blocks** for configuration examples
- **Explicit "SHALL" / "SHALL NOT"** language for constraints
- **Scenario blocks** with GIVEN/WHEN/THEN structure for behavioral specifications

**Rationale**: Deterministic structures reduce the token-attention dilution that occurs when agents process free-form text. Numbered steps provide unambiguous execution order. Tables make parameter mappings machine-readable. GIVEN/WHEN/THEN scenarios create verifiable behavioral contracts.

### Decision 2: All 6 Agent Skill Files Updated — Not Just Deepseek

**Context**: The project uses both Deepseek (v4-flash, v4-pro) and Qwen (plus, max) model families. Deepseek files were created first and are the "source of truth." Qwen files mirror Deepseek structure.

**Decision**: All 6 files will be updated simultaneously with identical structural changes. Deepseek files get the full technology stack and architectural details; Qwen files mirror Deepseek structure with model-specific substitutions (model name, permissions).

**Rationale**: If only Deepseek files are updated, Qwen agents will continue generating non-compliant code. Both model families must have accurate instructions. Mirroring ensures consistency — there is no "better" or "worse" set of instructions between model families.

### Decision 3: Parallel Workstream Execution

| Workstream | Files | Dependencies |
|---|---|---|
| Spec creation (WS1) | `agent-validation-protocol/spec.md`, `stored-procedure-repository/spec.md`, `project-structure/delta.md` | None |
| Agent skill files (WS2) | 6 files in `.agents/skills/` | Follows content defined in specs |
| Backend docs (WS3) | 4 files in `docs/` | None |
| Cross-cutting optimization | Applied across all files | Follows content defined in WS1 |

All workstreams can execute in parallel since they update different files. Cross-cutting optimization (deterministic structures) is applied during each file update, not as a separate pass.

### Decision 4: SQL Server Replaces PostgreSQL in All Documentation

**Context**: The project's `.csproj` uses `Microsoft.EntityFrameworkCore.SqlServer` (not `Npgsql.EntityFrameworkCore.PostgreSQL`). The `efpt.config.json` targets SQL Server. However, existing documentation universally references PostgreSQL.

**Decision**: All documentation will be updated to reference SQL Server as the database provider. PostgreSQL references will be removed. Npgsql package references will be replaced with `Microsoft.Data.SqlClient`.

**Rationale**: Documentation must match the actual codebase. Misleading database references cause agents to generate code with wrong packages, wrong connection strings, and wrong type mappings (Npgsql-specific types like `NpgsqlConnection` vs. `SqlConnection`).

### Decision 5: Identity vs Business Entity Boundary (AspNet* Exclusion)

**Context**: `efpt.config.json` contains AspNet* tables (AspNetUsers, AspNetRoles, etc.) generated by EF Core Power Tools. These are managed by ASP.NET Core Identity's internal data layer and should not have custom repositories or stored procedures.

**Decision**: Any entity whose name starts with "AspNet" is classified as IDENTITY-MANAGED and excluded from Step 0 validation, repository generation, and stored procedure creation. Agents check for the "AspNet" prefix before any validation step.

**Rationale**: ASP.NET Core Identity's `UserManager`, `RoleManager`, and `SignInManager` handle all data access for these tables internally. Writing stored procedures or repositories against them would conflict with the framework's change tracking and migration management.

### Decision 6: Three-Layer Injection Chain

**Decision**: Controllers inject service interfaces, NOT repository interfaces. The full chain is:
```
Controller → IService → IRepository → ApplicationDbContext (via DbContext base)
```
Services orchestrate business logic across entities. Repositories handle data access exclusively.

This corrects the earlier decision that placed repositories directly in controllers.

**Rationale**: Services provide the business logic layer that the `project-structure` spec requires. Controllers stay thin — validate HTTP, call service, return result. This keeps EF Core dependencies (`Microsoft.EntityFrameworkCore`) out of controllers entirely.

### Decision 7: Single Runtime DbContext

**Decision**: `ApplicationDbContext` in `/Infrastructure/Data/` is the sole DI-registered runtime context. `RulesEngineEditorContext` in `/Business/Entities/Models/` is a Power Tools scaffold providing entity type shapes at design time. It is NOT registered in DI and NOT used at runtime.

**Rationale**: A single registered context simplifies DI wiring and ensures all data access (Identity + business) goes through the same connection. The scaffold context provides type definitions and procedure interfaces that can be cross-pollinated into `ApplicationDbContext.Procedures.cs`.

### Decision 8: Entity Namespace Convention

**Decision**: ALL entity classes live in namespace `RulesEngineEditor.Server.Business.Entities.Models`, physically in `/Business/Entities/Models/dbo/`. This is the single source of truth for entity shapes, column names, C# types, and nullability.

**Rationale**: Agents need a single, deterministic location to find entity metadata for T-SQL generation and parameter mapping. Multiple entity sources would create ambiguity.

### Decision 9: Refined Step 0 — Three-Source Lookup

**Decision**: Step 0 validation checks three sources in order:
1. `efpt.config.json` Tables/StoredProcedures arrays (primary whitelist)
2. `/Business/Entities/Models/dbo/` (Models folder fallback — C# entity classes)
3. Actual database objects (optional runtime check)

This replaces the previous binary (whitelist only) check. Partial states are now handled: a table may exist in Models but not in the whitelist, or some CRUD SPs may exist while others are missing.

**Rationale**: Binary checking caused the Agentic Correction Routine to fire for every new entity, even when the entity class already existed. The Models fallback allows agents to distinguish between "entity exists but whitelist is stale" (Power Tools Refresh needed) and "entity genuinely does not exist" (full creation needed).

### Decision 10: Separate CRUD vs Custom SP Paths

**Decision**: CRUD stored procedures (5-pack) and custom stored procedures (domain-specific operations like `sp_ExecuteWorkflow`, `sp_ValidateWorkflowJson`, `sp_GetWorkflowStatistics`) have separate validation and generation paths. Custom SPs are validated individually against the whitelist and do not trigger the CRUD 5-pack check.

**Rationale**: Custom SPs serve different purposes — execution, validation, aggregation, filtering — that don't require the full CRUD set. A feature that needs only `sp_ExecuteWorkflow` should not be blocked because `sp_InsertWorkflow` doesn't exist yet.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Agent skill files become stale if architecture changes | Cross-reference specs as source of truth; agent skill files summarize, specs provide details |
| Qwen files diverge from Deepseek files in future updates | Include cross-reference note in Qwen files pointing to Deepseek as source of truth for technology stack details |
| PostgreSQL→SQL Server change surprises developers not involved in this decision | Document rationale in design.md and in BE.Architecture.md changelog |
| Step 0 validation adds execution time to every agent task | Validation is a quick JSON file parse (< 1s); cost is negligible compared to preventing hallucinated database objects |
| Agents may skip Step 0 if not explicitly reminded | Architect skill file makes Step 0 the FIRST numbered step in OpenSpec Workflow section; Reviewer checklist includes Step 0 compliance check |
