# Stored Procedure Repository Pattern

## Purpose

Define the `BaseStoredProcedureRepository<TEntity, TKey>` pattern for all data access. This specification establishes the abstract base class contract, the required abstract members, the `SqlParameter` mapping conventions, folder structure, and implementation rules for concrete repositories.

## Requirements

### Requirement: BaseStoredProcedureRepository Foundation

ALL concrete repositories SHALL inherit from `BaseStoredProcedureRepository<TEntity, TKey>` located at `/Infrastructure/Repositories/BaseStoredProcedureRepository.cs`. The base class SHALL provide CRUD methods that use stored procedures exclusively via `FromSqlRaw` and `ExecuteSqlRawAsync`.

#### Scenario: Repository inherits from BaseStoredProcedureRepository
- **WHEN** a concrete repository is created (e.g., `WorkflowRepository`)
- **THEN** it SHALL inherit from `BaseStoredProcedureRepository<Workflow, int>` and implement ALL abstract members

#### Scenario: All CRUD operations use stored procedures
- **WHEN** `GetAllAsync()` is called
- **THEN** it SHALL execute `EXEC {SpGetAll}` via `DbSet.FromSqlRaw(...).AsNoTracking().ToListAsync()`
- **WHEN** `GetByIdAsync(id)` is called
- **THEN** it SHALL execute `EXEC {SpGetById} @Id` via `DbSet.FromSqlRaw(...).AsNoTracking().FirstOrDefaultAsync()`
- **WHEN** `CreateAsync(entity)` is called
- **THEN** it SHALL execute `EXEC {SpInsert} @Param1, @Param2 OUTPUT` via `Context.Database.ExecuteSqlRawAsync(...)`
- **WHEN** `UpdateAsync(entity)` is called
- **THEN** it SHALL execute `EXEC {SpUpdate} @Param1, @Param2` via `Context.Database.ExecuteSqlRawAsync(...)`
- **WHEN** `DeleteAsync(id)` is called
- **THEN** it SHALL execute `EXEC {SpDelete} @Id` via `Context.Database.ExecuteSqlRawAsync(...)`

### Requirement: Abstract Member Contract

Each concrete repository SHALL implement the following abstract members from `BaseStoredProcedureRepository<TEntity, TKey>`:

| Abstract Member | Return Type | Purpose |
|---|---|---|
| `SpGetAll` | `string` | Stored procedure name for fetching all records (e.g., `"[dbo].[sp_GetAllWorkflows]"`) |
| `SpGetById` | `string` | Stored procedure name for fetching by ID (e.g., `"[dbo].[sp_GetWorkflowById]"`) |
| `SpInsert` | `string` | Stored procedure name for inserting (e.g., `"[dbo].[sp_InsertWorkflow]"`) |
| `SpUpdate` | `string` | Stored procedure name for updating (e.g., `"[dbo].[sp_UpdateWorkflow]"`) |
| `SpDelete` | `string` | Stored procedure name for deleting (e.g., `"[dbo].[sp_DeleteWorkflow]"`) |
| `IdParameterName` | `string` | Parameter name for the ID parameter (e.g., `"@WorkflowId"`) |
| `MapToInsertParameters` | `SqlParameter[]` | Maps entity properties to INSERT procedure parameters; MUST include an OUTPUT parameter `@NewId` for the identity column |
| `MapToUpdateParameters` | `SqlParameter[]` | Maps entity properties to UPDATE procedure parameters; MUST include the ID parameter |

#### Scenario: Abstract members use bracketed schema-qualified names
- **WHEN** a concrete repository implements `SpGetAll`
- **THEN** the return value SHALL be `"[dbo].[sp_GetAll{EntityName}]"` (bracketed, schema-qualified)
- **AND** `IdParameterName` SHALL return `"@{EntityName}Id"` (e.g., `"@WorkflowId"`)

#### Scenario: MapToInsertParameters includes @NewId OUTPUT
- **WHEN** `MapToInsertParameters` is called
- **THEN** the returned array SHALL include a `SqlParameter("@NewId", SqlDbType.Int) { Direction = ParameterDirection.Output }` parameter
- **AND** the array SHALL NOT contain duplicate parameter names

#### Scenario: MapToUpdateParameters includes the ID parameter
- **WHEN** `MapToUpdateParameters` is called
- **THEN** the returned array SHALL include a `SqlParameter` for `IdParameterName` with the entity's ID value
- **AND** SHALL NOT include an OUTPUT parameter

### Requirement: Scope — Identity Entities Excluded

Entities with names starting with "AspNet" (AspNetUsers, AspNetRoles, AspNetRoleClaims, AspNetUserClaims, AspNetUserLogins, AspNetUserRoles, AspNetUserTokens) SHALL NOT have concrete repository implementations. These tables are managed by Microsoft.AspNetCore.Identity and accessed through `UserManager<T>`, `RoleManager<T>`, and `SignInManager<T>`.

#### Scenario: No repository created for Identity entities
- **WHEN** an agent encounters an AspNet* entity
- **THEN** it SHALL NOT create a repository, interface, or stored procedures
- **AND** it SHALL reference the `agent-validation-protocol` specification for the Identity Entity Exclusion rule
- **AND** it SHALL inform the operator: "AspNet tables are Identity-managed. Access via UserManager/RoleManager/SignInManager."

### Requirement: No EF Core Change Tracking

EF Core's native change tracking SHALL be explicitly disabled for all stored procedure operations. The `BaseStoredProcedureRepository` base class SHALL call `.AsNoTracking()` on all `FromSqlRaw` queries.

#### Scenario: GetAllAsync uses AsNoTracking
- **WHEN** `GetAllAsync()` executes
- **THEN** the query SHALL append `.AsNoTracking()` before `.ToListAsync()`
- **AND** the DbContext SHALL not track returned entities

#### Scenario: Create/Update/Delete bypass the change tracker
- **WHEN** `CreateAsync()`, `UpdateAsync()`, or `DeleteAsync()` executes
- **THEN** they SHALL use `Context.Database.ExecuteSqlRawAsync()` directly, bypassing `DbSet` and the change tracker entirely

### Requirement: Folder Organization

Concrete repositories SHALL be organized in entity-specific subfolders under `/Infrastructure/Repositories/`.

#### Scenario: Repository file path follows convention
- **WHEN** a concrete repository is created for the `Workflow` entity
- **THEN** it SHALL be placed at `./Infrastructure/Repositories/Workflows/WorkflowRepository.cs`
- **AND** the folder name SHALL be the pluralized entity name

#### Scenario: Repository namespace follows convention
- **WHEN** inspecting the namespace of `WorkflowRepository`
- **THEN** it SHALL be `RulesEngineEditor.Server.Infrastructure.Repositories.Workflows`

### Requirement: Runtime DbContext

There is exactly ONE runtime DbContext: `ApplicationDbContext` in `/Infrastructure/Data/`. It SHALL be registered in DI via `AddDbContext<ApplicationDbContext>(UseSqlServer)`. The `RulesEngineEditorContext` in `/Business/Entities/Models/` is an EF Core Power Tools scaffold providing entity type shapes at design time and is NOT registered in DI.

#### Scenario: Repository injects ApplicationDbContext
- **WHEN** a concrete repository is instantiated
- **THEN** it SHALL receive `ApplicationDbContext` injected via the `BaseStoredProcedureRepository` base class constructor parameter `DbContext context`
- **AND** the entity type `TEntity` SHALL be referenced from namespace `RulesEngineEditor.Server.Business.Entities.Models`

### Requirement: Entity Namespace Convention

ALL entity classes SHALL live in namespace `RulesEngineEditor.Server.Business.Entities.Models`, physically located in folder `/Business/Entities/Models/dbo/`. This is the single source of truth for entity shapes, column names, types, and nullability — used by both `ApplicationDbContext` and repository implementations.

#### Scenario: Repository references entity from Business.Entities.Models
- **WHEN** a concrete repository is created for the `Workflow` entity
- **THEN** it SHALL use `using RulesEngineEditor.Server.Business.Entities.Models;`
- **AND** the entity class `Workflow` SHALL be from `dbo/Workflow.cs` in that namespace

### Requirement: Three-Layer Injection Chain

Data access follows a three-layer injection chain: Controller → IService → IRepository → ApplicationDbContext.

#### Scenario: Controller depends on service, not repository
- **WHEN** a Controller class has a data dependency
- **THEN** it SHALL inject a service interface (e.g., `IWorkflowService`), NOT a repository interface
- **AND** the service SHALL inject the repository interface (e.g., `IWorkflowRepository`)
- **AND** the repository SHALL inject `ApplicationDbContext` via the base class

### Requirement: Dependency Injection Registration

Concrete repositories SHALL be registered in the DI container in `Program.cs` using the `AddScoped` lifetime. Services SHALL also be registered with `AddScoped`.

#### Scenario: Repository registered as scoped service
- **WHEN** `Program.cs` configures services
- **THEN** each concrete repository SHALL be registered as:
  ```csharp
  builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
  ```
- **AND** each service SHALL be registered as:
  ```csharp
  builder.Services.AddScoped<IWorkflowService, WorkflowService>();
  ```
- **AND** the interface SHALL be co-located with the implementation

### Requirement: Interface Per Repository

Each concrete repository SHALL have a corresponding interface that extends `IStoredProcedureRepository<TEntity, TKey>`.

#### Scenario: Repository interface inherits from base interface
- **WHEN** inspecting `IWorkflowRepository`
- **THEN** it SHALL inherit from `IStoredProcedureRepository<Workflow, int>`
- **AND** it SHALL be placed in the same folder as the implementation

### Requirement: Stored Procedure Naming Convention

All stored procedures SHALL follow the naming convention: `sp_{Action}{EntityName}` with PascalCase entity name.

#### Scenario: Procedure names match convention
- **WHEN** a stored procedure is created for the `Workflow` entity
- **THEN** its name SHALL match one of: `sp_InsertWorkflow`, `sp_GetWorkflowById`, `sp_GetAllWorkflows`, `sp_UpdateWorkflow`, `sp_DeleteWorkflow`
- **AND** the entity name portion SHALL exactly match the C# entity class name
