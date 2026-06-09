# Rules Engine Repository

## Purpose

The Rules Engine Repository provides data access for workflow definitions and test scenarios via EF Core, following the existing repository pattern with read-only queries for workflow definitions and persistence for test scenarios.

## Requirements

### Requirement: Repository provides read access to all workflow definitions
The system SHALL expose an `IRulesRepository` interface with a method to retrieve all workflow definitions from the database, including their JSON content, metadata, and status.

#### Scenario: Retrieve all workflows
- **WHEN** a caller invokes `GetAllWorkflowsAsync()`
- **THEN** the system returns a collection of all `WorkflowDefinition` entities from the database, ordered by `WorkflowName` ascending then `Version` descending

#### Scenario: No workflows exist
- **WHEN** a caller invokes `GetAllWorkflowsAsync()` and the database contains no workflow definitions
- **THEN** the system returns an empty collection without throwing an exception

### Requirement: Repository provides read access to a single workflow definition
The system SHALL expose a method to retrieve a single workflow definition by its primary key, including its JSON content.

#### Scenario: Retrieve workflow by ID
- **WHEN** a caller invokes `GetWorkflowByIdAsync(id)` with a valid `WorkflowDefinitionId`
- **THEN** the system returns the matching `WorkflowDefinition` entity

#### Scenario: Workflow does not exist
- **WHEN** a caller invokes `GetWorkflowByIdAsync(id)` with an ID that does not exist in the database
- **THEN** the system returns `null`

### Requirement: Repository persists test scenarios with input and expected output
The system SHALL expose a method to save a new `WorkflowTestScenario` entity containing scenario name, mock input JSON, expected output JSON, and a foreign key reference to a workflow definition.

#### Scenario: Save a valid test scenario
- **WHEN** a caller invokes `SaveScenarioAsync(scenario)` with a valid `WorkflowTestScenario` entity
- **THEN** the system persists the entity to the database and returns the saved entity with its generated `ScenarioId`

#### Scenario: Save scenario referencing non-existent workflow
- **WHEN** a caller invokes `SaveScenarioAsync(scenario)` with a `WorkflowDefinitionId` that does not exist in the database
- **THEN** the system throws a `DbUpdateException` with a foreign key constraint violation

### Requirement: Repository queries are read-only for workflow definitions
The system SHALL execute all workflow definition read queries with `AsNoTracking()` to avoid change tracking overhead.

#### Scenario: Read workflow definitions without tracking
- **WHEN** `GetAllWorkflowsAsync()` or `GetWorkflowByIdAsync()` is invoked
- **THEN** the underlying EF Core query uses `AsNoTracking()` and the returned entities are not tracked by the change tracker

### Requirement: EF Core entity configuration maps JSON columns correctly
The system SHALL define `IEntityTypeConfiguration<WorkflowDefinition>` and `IEntityTypeConfiguration<WorkflowTestScenario>` classes that map `JsonContent`, `MockInputJson`, and `ExpectedOutputJson` properties to SQL Server JSON column type using `[Column(TypeName = "json")]` or equivalent Fluent API configuration.

#### Scenario: JSON columns are mapped in entity configuration
- **WHEN** EF Core generates a migration or creates the database schema
- **THEN** the `JsonContent`, `MockInputJson`, and `ExpectedOutputJson` columns are created with the SQL Server `json` data type

#### Scenario: Entity configurations are auto-discovered
- **WHEN** `ApplicationDbContext.OnModelCreating` is invoked
- **THEN** the `WorkflowDefinitionConfiguration` and `WorkflowTestScenarioConfiguration` classes are applied via `ApplyConfigurationsFromAssembly()`
