## Why

The project has already established a `BaseStoredProcedureRepository<TEntity, TKey>` in `/Infrastructure/Repositories/`, an `efpt.config.json` whitelist as the authoritative source of truth for database objects, and an architectural guide specifying stored-procedure-only mutations with no inline SQL and no EF Core change tracking. However, the existing agentic documentation — the agent skill files (`.agents/skills/deepseek-*.md`, `.agents/skills/qwen-*.md`), the backend documentation (`docs/BE.*.md`), and the OpenSpec workflow documentation — all still reference the **old EF Core + PostgreSQL + LINQ paradigm** rather than the **current SQL Server + stored procedures + repository pattern**.

This gap causes AI agents to:
- Generate code that violates the architectural constraints (inline SQL, EF tracking, DbContext injection into controllers)
- Reference PostgreSQL packages (`Npgsql.EntityFrameworkCore.PostgreSQL`) that are not in the project's `.csproj`
- Skip the `efpt.config.json` whitelist validation before making changes
- Produce implementations that require refactoring to comply with the stored-procedure-only rule

The documentation must be updated to match the actual architecture and be **optimized for agent parsing** — deterministic, machine-readable, with unambiguous validation steps and checklists that agents can execute programmatically.

## What Changes

### 1. Agent Skill Files — All 6 Files Updated
All six agent skill files (3 Deepseek + 3 Qwen) will be updated to reflect the current architecture:

| Agent File | Key Changes |
|---|---|
| `deepseek-architect.md` | Add stored-procedure-only constraint, efpt.config.json whitelist validation, SQL Server stack, repository pattern |
| `deepseek-builder.md` | Replace EF Core DbContext patterns with `BaseStoredProcedureRepository` patterns, add concrete repository implementation guidance, SQL Server stored procedure patterns, Step 0 validation |
| `deepseek-reviewer.md` | Add stored procedure compliance checklist, no-inline-SQL check, efpt.config.json whitelist verification, SQL Server patterns |
| `qwen-architect.md` | Same updates as deepseek-architect (mirror pattern) |
| `qwen-builder.md` | Same updates as deepseek-builder (mirror pattern) |
| `qwen-reviewer.md` | Same updates as deepseek-reviewer (mirror pattern) |

### 2. OpenSpec Workflow Documentation — Agent-Optimized
The OpenSpec workflow sections within each agent skill file will be restructured into **deterministic, numbered step procedures** that agents can execute sequentially without ambiguity. A new **Step 0: Pre-Execution Validation** protocol will be codified as a mandatory first step before any codebase modification.

A new specification (`agent-validation-protocol`) will formally document:
- How to parse `efpt.config.json` as the absolute whitelist
- How to verify tables and stored procedures exist
- The Agentic Correction Routine when objects are missing

### 3. Backend Documentation — 4 Files Updated

| Doc File | Key Changes |
|---|---|
| `BE.Architecture.md` | Replace PostgreSQL/LINQ with SQL Server/stored procedures; document repository folder structure; document efpt.config.json as source of truth; document agent validation protocol |
| `BE.AgentRoles.md` | Update technology stack references; add stored procedure responsibilities; document Step 0 validation as agent mandate |
| `BE.DebugGuide.md` | Replace PostgreSQL debugging with SQL Server stored procedure debugging; add efpt.config.json validation debugging |
| `BE.SkillsAudit.md` | Update technology stack references from PostgreSQL to SQL Server; reassess skills against stored-procedure architecture |

### 4. New Specifications Created

| Spec | Purpose |
|---|---|
| `agent-validation-protocol/spec.md` | Formal specification for the deterministic pre-execution validation (Step 0) and Agentic Correction Routine |
| `stored-procedure-repository/spec.md` | Formal specification for the `BaseStoredProcedureRepository<TEntity, TKey>` pattern, abstract members, parameter mapping, and implementation conventions |

### 5. Existing Spec Updated

| Spec | Changes |
|---|---|
| `project-structure/spec.md` (delta) | Add `/Infrastructure/Repositories/[Entity]/` folder structure; add stored-procedure-only constraint; add no-EF-tracking rule; add whitelist validation requirement |

## Capabilities

### Updated Capabilities
- `project-structure`: Repository folder structure and stored-procedure-only constraint added to existing spec
- `deepseek-architect`, `deepseek-builder`, `deepseek-reviewer`, `qwen-architect`, `qwen-builder`, `qwen-reviewer`: All agent skill files updated with current architecture
- `BE.Architecture`, `BE.AgentRoles`, `BE.DebugGuide`, `BE.SkillsAudit`: All backend docs updated

### New Capabilities
- `agent-validation-protocol`: Formal spec for efpt.config.json whitelist parsing, Step 0 pre-execution validation, and Agentic Correction Routine
- `stored-procedure-repository`: Formal spec for the `BaseStoredProcedureRepository<TEntity, TKey>` pattern including abstract member contracts, `SqlParameter` mapping, and implementation conventions

## Impact

- **No source code changes** — this change is documentation-only, updating agent skill files, backend docs, and OpenSpec specifications
- **6 agent skill files updated** — Deepseek and Qwen agents will have accurate, deterministic instructions
- **4 backend docs updated** — architecture, roles, debug guide, and skills audit reflect current reality
- **2 new specs created** — agent validation protocol and stored procedure repository pattern
- **1 existing spec updated** — project-structure gains repository folder structure
- **Breaking change to agent behavior**: After this update, agents MUST execute Step 0 validation before any code modification. Previously, agents could skip whitelist validation entirely.
