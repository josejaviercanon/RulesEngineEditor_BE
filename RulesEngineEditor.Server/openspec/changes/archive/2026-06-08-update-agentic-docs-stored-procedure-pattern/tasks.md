# Tasks — Update Agentic Docs for Stored Procedure Pattern

## Workstream 1: Specifications

- [x] **Task 1.1**: Create `agent-validation-protocol/spec.md` in the change specs
  - File: `specs/agent-validation-protocol/spec.md`
  - Content: Step 0 pre-execution validation mandate, Identity entity exclusion (AspNet*), efpt.config.json + Models folder three-source lookup, 5-procedure CRUD checklist, Custom Stored Procedures path, Agentic Correction Routine (AspNet-aware, partial-state handling), zero-inline-SQL enforcement
  - Verify: Spec covers all scenarios: AspNet exclusion, Models fallback, custom SPs, partial-state correction

- [x] **Task 1.2**: Create `stored-procedure-repository/spec.md` in the change specs
  - File: `specs/stored-procedure-repository/spec.md`
  - Content: BaseStoredProcedureRepository foundation, Identity entity exclusion scope, abstract member contract, SqlParameter mapping, no-EF-tracking rule, single runtime DbContext (ApplicationDbContext), entity namespace convention (Business.Entities.Models), three-layer injection chain (Controller→Service→Repository), folder organization, DI registration, interface convention, stored procedure naming
  - Verify: Spec covers all abstract members, parameter conventions, exclusion scope, namespace, and three-layer chain

- [x] **Task 1.3**: Create `project-structure/delta.md` amending the existing spec
  - File: `specs/project-structure/delta.md`
  - Content: 4 amendments — repository folder structure, stored-procedure-only constraint, whitelist validation requirement, folder structure diagram update
  - Verify: Delta spec is applicable as amendments to `openspec/specs/project-structure/spec.md`

- [x] **Task 1.4**: Update `design.md` with Decisions 5-10
  - File: `design.md`
  - Content: Decision 5 (AspNet* exclusion), Decision 6 (three-layer chain), Decision 7 (single runtime DbContext), Decision 8 (entity namespace convention), Decision 9 (refined Step 0 three-source lookup), Decision 10 (separate CRUD vs custom SP paths)
  - Verify: All 6 new decisions documented with context, rationale, and alternatives

## Workstream 2: Agent Skill Files

- [x] **Task 2.1**: Update `deepseek-architect.md`
  - File: `.agents/skills/deepseek-architect.md`
  - Changes:
    - Technology Stack: PostgreSQL → SQL Server, add stored-procedure-only, add `BaseStoredProcedureRepository`, add `efpt.config.json` as source of truth, add entity namespace (Business.Entities.Models)
    - OpenSpec Workflow: Restructure as numbered steps with Step 0 entry gate (AspNet exclusion check before any db validation)
    - Step 0: Add AspNet exclusion gate, Models folder fallback, custom SP path
    - Key Architectural Decisions: Add AspNet* exclusion, three-layer chain, single DbContext, namespace convention
  - Verify: Step 0 has exclusion check at top, Models fallback documented

- [x] **Task 2.2**: Update `deepseek-builder.md`
  - File: `.agents/skills/deepseek-builder.md`
  - Changes:
    - Technology Stack: Add single runtime DbContext (ApplicationDbContext), entity namespace (Business.Entities.Models), three-layer chain
    - Step 0 section: Add AspNet exclusion gate, Models folder fallback, custom SP path, partial-state handling
    - Repository Patterns section: Replace "Controllers inject repository interfaces" with "Controllers inject services, services inject repositories"
    - Add namespace convention: `using RulesEngineEditor.Server.Business.Entities.Models;`
    - Testing Requirements: Update for stored procedure repository testing patterns, SQL Server Testcontainers
  - Verify: Three-layer chain documented, namespace convention present, AspNet exclusion in Step 0

- [x] **Task 2.3**: Update `deepseek-reviewer.md`
  - File: `.agents/skills/deepseek-reviewer.md`
  - Changes:
    - Step 0 Audit: Add AspNet exclusion check item, Models folder verification, custom SP audit
    - Code Review: Add three-layer chain compliance check, single DbContext check, namespace convention check
    - Review Checklist: Split CRUD SP check and custom SP check into separate items
  - Verify: AspNet exclusion in Step 0 audit, three-layer chain in Code Review

- [x] **Task 2.4**: Update `qwen-architect.md`
  - File: `.agents/skills/qwen-architect.md`
  - Changes: Mirror deepseek-architect updates (all refinements above + AspNet exclusion + Models fallback)
  - Verify: Qwen architect file matches structure of updated deepseek-architect (model-specific differences only)

- [x] **Task 2.5**: Update `qwen-builder.md`
  - File: `.agents/skills/qwen-builder.md`
  - Changes: Mirror deepseek-builder updates (all refinements above + three-layer chain + namespace)
  - Verify: Qwen builder file matches structure of updated deepseek-builder (model-specific differences only)

- [x] **Task 2.6**: Update `qwen-reviewer.md`
  - File: `.agents/skills/qwen-reviewer.md`
  - Changes: Mirror deepseek-reviewer updates (all refinements above + AspNet exclusion audit)
  - Verify: Qwen reviewer file matches structure of updated deepseek-reviewer (model-specific differences only)

## Workstream 3: Backend Documentation

- [x] **Task 3.1**: Update `docs/BE.Architecture.md`
  - File: `docs/BE.Architecture.md`
  - Changes:
    - Technology: PostgreSQL/SQL → SQL Server
    - Project Structure: Add `/Infrastructure/Repositories/[Entity]/` with nested structure
    - Add section: "Source of Truth — efpt.config.json" documenting the whitelist
    - Add section: "Stored Procedure Repository Pattern" documenting the data access layer
    - Add section: "Agent Validation Protocol" documenting Step 0
    - Add section: "Architecture Boundaries" documenting Identity Domain (AspNet* exclusion) vs Business Domain (stored procedure managed)
    - Add three-layer injection chain (Controller→Service→Repository)
    - Remove PostgreSQL-specific references
    - Update Testing Strategy for stored procedure repository testing
  - Verify: Architecture Boundaries section present, AspNet exclusion documented, three-layer chain present

- [x] **Task 3.2**: Update `docs/BE.AgentRoles.md`
  - File: `docs/BE.AgentRoles.md`
  - Changes:
    - Overview: Update technology stack to SQL Server, stored procedures, repository pattern
    - Agent Responsibilities: Add three-layer chain responsibility, AspNet exclusion awareness, namespace convention
    - Add section: "Step 0 Pre-Execution Validation" as mandatory agent behavior
    - Add section: "Entity Namespace Convention" pointing to `RulesEngineEditor.Server.Business.Entities.Models`
    - Add section: "Architecture Boundaries — Identity vs Business" documenting AspNet* exclusion
  - Verify: Namespace convention documented, AspNet exclusion documented, Step 0 present

- [x] **Task 3.3**: Update `docs/BE.DebugGuide.md`
  - File: `docs/BE.DebugGuide.md`
  - Changes:
    - Common Issues: Add stored procedure execution errors, efpt.config.json validation failures
    - Debugging Steps: Add SQL Server connection troubleshooting, stored procedure debugging with SSMS/Azure Data Studio
    - Replace "PostgreSQL Connection Troubleshooting" section with "SQL Server Connection Troubleshooting"
    - Replace "EF Core Migration Debugging" with "Stored Procedure Debugging" covering execution plan analysis, parameter inspection
    - Add section: "efpt.config.json Validation" covering JSON parsing verification
  - Verify: PostgreSQL troubleshooting removed, SQL Server troubleshooting added, stored procedure debugging documented

- [x] **Task 3.4**: Update `docs/BE.SkillsAudit.md`
  - File: `docs/BE.SkillsAudit.md`
  - Changes:
    - Update technology stack references from PostgreSQL to SQL Server
    - Update coverage gaps: Remove PostgreSQL/Npgsql gap, add SQL Server/Stored Procedure coverage assessment
    - Update recommendations for stored procedure patterns
  - Verify: Technology references consistent with sql-server-stored-procedure architecture

## Cross-Cutting Verification

- [x] **Task 4.1**: Cross-document consistency check
  - Verify: All 6 agent skill files reference SQL Server (not PostgreSQL)
  - Verify: All 6 agent skill files include refined Step 0 with AspNet exclusion gate
  - Verify: All 6 agent skill files include Models folder fallback in Step 0
  - Verify: Builder files document three-layer injection chain (Controller→Service→Repository)
  - Verify: All files reference entity namespace `RulesEngineEditor.Server.Business.Entities.Models`
  - Verify: All 4 backend docs reference SQL Server and stored procedure repository pattern
  - Verify: BE.Architecture.md has "Architecture Boundaries" section with AspNet exclusion
  - Verify: BE.AgentRoles.md has namespace convention and AspNet exclusion sections
  - Verify: No document references "Npgsql", "PostgreSQL" positively (negative references only)
  - Verify: efpt.config.json is referenced as the source of truth in all appropriate documents

- [x] **Task 4.2**: Verify all updated files exist and have correct content
  - Check: All 6 agent skill files updated with AspNet exclusion in Step 0
  - Check: All 4 backend docs updated with refined architecture
  - Check: 2 specs updated (agent-validation-protocol + stored-procedure-repository) with all 6 refinements
  - Check: 1 delta spec created (project-structure)
  - Check: design.md updated with Decisions 5-10
  - Check: No positive PostgreSQL references remain in any project documentation
