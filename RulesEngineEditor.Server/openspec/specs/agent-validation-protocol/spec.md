# Agent Validation Protocol

## Purpose

Define the deterministic pre-execution validation protocol (Step 0) that ALL AI agents MUST execute before making any codebase modifications. This protocol uses `efpt.config.json` as the absolute whitelist source of truth for database objects, prevents hallucination of tables or stored procedures, and provides a deterministic correction routine when required objects are missing.

## Requirements

### Requirement: Step 0 — Pre-Execution Validation Mandate

BEFORE writing any code that interacts with the database (repository, service, controller, or migration), an agent MUST execute the Step 0 validation protocol. This requirement applies to ALL agent roles — Architect, Builder, and Reviewer.

#### Scenario: Builder agent runs Step 0 before generating a repository
- **GIVEN** the Builder agent is tasked with creating a `WorkflowRepository`
- **WHEN** it begins execution
- **THEN** it MUST first check if the entity starts with "AspNet" (skip if so)
- **AND** then load and parse `efpt.config.json` to verify the `[dbo].[Workflows]` table exists in the `Tables` array
- **AND** if not found in whitelist, scan `Business/Entities/Models/dbo/` for `Workflow.cs`
- **AND** verify all 5 CRUD stored procedures (`sp_InsertWorkflow`, `sp_GetWorkflowById`, `sp_GetAllWorkflows`, `sp_UpdateWorkflow`, `sp_DeleteWorkflow`) are declared in the `StoredProcedures` array
- **AND** verify any custom stored procedures specified in the feature tasks (e.g., `sp_ExecuteWorkflow`) are also declared

#### Scenario: Architect agent runs Step 0 before designing repository specs
- **GIVEN** the Architect agent is planning a new feature with database entities
- **WHEN** it creates the proposal and specs
- **THEN** it MUST first verify the target entities exist in `efpt.config.json` and include the validation results in the design document

#### Scenario: Reviewer agent runs Step 0 during audit
- **GIVEN** the Reviewer agent is auditing an implemented feature
- **WHEN** it checks database access patterns
- **THEN** it MUST verify the implementation only uses objects declared in `efpt.config.json` and follows the stored-procedure-only rule

### Requirement: Identity Entity Exclusion

Entities whose names start with "AspNet" (AspNetUsers, AspNetRoles, AspNetRoleClaims, AspNetUserClaims, AspNetUserLogins, AspNetUserRoles, AspNetUserTokens) SHALL be excluded from all Step 0 validation, correction routines, and repository generation.

#### Scenario: Agent skips AspNet entities
- **GIVEN** an agent needs to work with AspNetUsers
- **WHEN** Step 0 validation runs
- **THEN** the agent SHALL recognize the "AspNet" prefix
- **AND** SHALL skip all validation (table check, SP check, correction routine)
- **AND** SHALL NOT generate repositories or stored procedures
- **BECAUSE** these tables are managed by Microsoft.AspNetCore.Identity via `IdentityDbContext<IdentityUser>`, `UserManager`, `RoleManager`, and `SignInManager`

#### Scenario: Correction routine does not fire for Identity entities
- **WHEN** an AspNet* entity is NOT found in the whitelist
- **THEN** the agent SHALL NOT append definitions to efpt.config.json
- **AND** SHALL NOT generate T-SQL scripts
- **AND** SHALL inform the operator: "AspNet tables are Identity-managed. No stored procedures or repositories needed."

### Requirement: efpt.config.json + Models Folder — Three-Source Source of Truth

The `efpt.config.json` file at the project root SHALL serve as the primary whitelist. The `/Business/Entities/Models/dbo/` folder SHALL serve as the secondary source — a Models folder fallback when objects are not found in the whitelist. Agents MUST check both sources deterministically.

#### Scenario: Agent parses Tables array — not found, checks Models folder
- **WHEN** an agent needs to verify a database table exists
- **THEN** it SHALL first check the `Tables` array in `efpt.config.json` for `"[dbo].[{TableName}]"` with `"ObjectType": 0`
- **AND** if NOT found in whitelist, it SHALL scan `/Business/Entities/Models/dbo/` for a C# entity class matching `{EntityName}.cs`
- **AND** if found in Models: state = TABLE_EXISTS_BUT_NOT_WHITELISTED — suggest EF Core Power Tools Refresh
- **AND** if NOT found in Models either: state = TABLE_DOES_NOT_EXIST — trigger full creation path

#### Scenario: Agent parses StoredProcedures array
- **WHEN** an agent needs to verify a stored procedure exists
- **THEN** it SHALL locate the `StoredProcedures` array in `efpt.config.json` and look for an exact match of `"[dbo].[{ProcedureName}]"` with `"ObjectType": 1`
- **AND** each CRUD set must contain exactly 5 procedures:
  * `sp_Insert[Entity]`
  * `sp_Get[Entity]ById`
  * `sp_GetAll[Entity]`
  * `sp_Update[Entity]`
  * `sp_Delete[Entity]`
- **AND** if only some of the 5 are missing, the agent SHALL generate T-SQL ONLY for the specific missing procedures (not regenerate all 5)

#### Scenario: Scope boundaries enforced
- **WHEN** an agent is planning or implementing a feature
- **THEN** objects declared in neither `efpt.config.json` nor the Models folder SHALL be treated as non-existent
- **AND** the agent SHALL NOT generate code referencing undeclared database objects

### Requirement: 5-Procedure CRUD Checklist

For EVERY entity in `efpt.config.json`, ALL 5 CRUD stored procedures MUST be declared. The agent SHALL verify this checklist before implementation:

| # | Procedure Name Pattern | Purpose |
|---|----------------------|---------|
| 1 | `sp_Insert[Entity]` | Create a new record; returns identity via OUTPUT parameter |
| 2 | `sp_Get[Entity]ById` | Fetch a single record by primary key |
| 3 | `sp_GetAll[Entity]` | Fetch all records (no filter) |
| 4 | `sp_Update[Entity]` | Update an existing record |
| 5 | `sp_Delete[Entity]` | Soft or hard delete by primary key |

#### Scenario: All 5 procedures verified before repository implementation
- **WHEN** an agent creates a concrete repository class (e.g., `WorkflowRepository`)
- **THEN** the agent SHALL have verified that all 5 CRUD procedures for the corresponding entity exist in the `StoredProcedures` array of `efpt.config.json`

### Requirement: Custom Stored Procedures

Non-CRUD stored procedures (domain-specific operations like execution, validation, filtering, aggregation) SHALL have a separate validation path from the standard 5-procedure CRUD check. Custom SPs are feature-dependent and may exist independently of CRUD SPs.

#### Scenario: Custom SP specified in feature tasks
- **WHEN** feature tasks specify a custom stored procedure (e.g., `sp_ExecuteWorkflow`, `sp_ValidateWorkflowJson`, `sp_GetWorkflowsByStatus`)
- **THEN** the agent SHALL check the `StoredProcedures` array for that exact name with `"ObjectType": 1`
- **AND** if missing, SHALL generate T-SQL for that specific procedure only (not trigger the CRUD 5-pack check)
- **AND** SHALL append the definition to the `StoredProcedures` array

#### Scenario: Custom SP exists independently of CRUD SPs
- **WHEN** a custom SP (e.g., `sp_GetWorkflowStatistics`) is in the whitelist
- **THEN** it SHALL satisfy the feature's custom SP requirement regardless of whether CRUD SPs exist
- **AND** the absence of CRUD SPs SHALL NOT block the feature if the feature only requires the custom SP

### Requirement: Agentic Correction Routine

When Step 0 validation fails (a required table or stored procedure is missing from `efpt.config.json`), the agent SHALL execute the following deterministic correction routine. The routine SHALL first check for AspNet* exclusion before taking any action.

#### Scenario: Missing table detected — correction routine triggered
- **WHEN** the target table is NOT found in the `Tables` array
- **THEN** the agent SHALL:
  1. **FIRST**: Check if entity name starts with "AspNet"
     - If YES: DO NOT append, DO NOT generate T-SQL. Inform operator: "Identity-managed table. No stored procedures or repositories needed." STOP.
     - If NO: continue.
  2. **Check Models folder**: Scan `/Business/Entities/Models/dbo/` for `{EntityName}.cs`
     - If FOUND in Models: state = TABLE_EXISTS_BUT_NOT_WHITELISTED
       → Suggest **Option 1**: Run EF Core Power Tools "Refresh" to regenerate `efpt.config.json`
       → Operator can append manually if preferred
     - If NOT in Models: state = TABLE_DOES_NOT_EXIST
       → Proceed with full creation (steps 3-8)
  3. Append the missing table definition to the `Tables` array:
     ```json
     { "Name": "[dbo].[{EntityName}]", "ObjectType": 0 }
     ```
  4. Append all 5 missing stored procedure definitions to the `StoredProcedures` array:
     ```json
     { "Name": "[dbo].[sp_Insert{EntityName}]", "ObjectType": 1 }
     ```
  5. HALT all further code execution
  6. Generate the required T-SQL CREATE TABLE and CREATE PROCEDURE scripts
  7. Present the operator with exactly 2 options:
     - **Option 1**: Run EF Core Power Tools "Refresh" from Visual Studio, confirm completion, then resume
     - **Option 2**: Execute the generated T-SQL scripts manually on the database server, confirm completion, then resume
  8. BLOCK all further agent execution until the operator confirms one of the two options
  9. Upon resumption, ALWAYS re-run Step 0 validation before continuing

#### Scenario: Missing stored procedure detected — partial-state handling
- **WHEN** any of the 5 CRUD stored procedures is NOT found in the `StoredProcedures` array
- **THEN** the agent SHALL:
  1. **FIRST**: Check if entity name starts with "AspNet" — if YES, STOP (Identity-managed, no SPs needed)
  2. **LIST** exactly which specific SPs are missing (e.g., "2 of 5 missing: sp_InsertWorkflow, sp_DeleteWorkflow")
  3. **Generate T-SQL ONLY for the missing procedures** — do NOT regenerate procedures that already exist
  4. **Append** the missing SP definitions to the `StoredProcedures` array
  5. HALT, present operator with Options 1 and 2 (as above)
  6. BLOCK until operator confirms
  7. Rerun Step 0 on resume

#### Scenario: Custom SP missing — separate path from CRUD check
- **WHEN** a custom stored procedure (e.g., `sp_ExecuteWorkflow`) is NOT found
- **THEN** the agent SHALL:
  1. Generate T-SQL for that specific custom procedure
  2. Append the definition to the `StoredProcedures` array
  3. HALT, prompt operator
- **AND** the procedure SHALL NOT trigger the CRUD 5-pack validation for the same entity

### Requirement: Zero Inline SQL

The protocol SHALL enforce that NO inline SQL queries are written in C# code. All database mutations SHALL execute exclusively through stored procedures via `BaseStoredProcedureRepository<TEntity, TKey>`.

#### Scenario: Repository methods use EXEC calls, not inline SQL
- **WHEN** a repository method performs a database operation
- **THEN** it SHALL use `FromSqlRaw($"EXEC {SpName} ...")` or `ExecuteSqlRawAsync($"EXEC {SpName} ...")` only
- **AND** SHALL NOT contain embedded `SELECT`, `INSERT`, `UPDATE`, or `DELETE` SQL strings

#### Scenario: Reviewer flags inline SQL as violation
- **WHEN** the Reviewer agent finds inline SQL in a repository, service, or controller
- **THEN** it SHALL flag it as a CRITICAL violation and block the implementation
