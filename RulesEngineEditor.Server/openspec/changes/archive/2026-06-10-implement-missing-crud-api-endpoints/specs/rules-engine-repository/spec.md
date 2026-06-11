# Rules Engine Repository (Delta)

## ADDED Requirements

### Requirement: Repository provides workflow create operation
The system SHALL expose a method on `IRulesRepository` to persist a new `WorkflowDefinitions` entity, setting `CreatedAt` to UTC now if not already provided.

#### Scenario: Create a valid workflow definition
- **WHEN** a caller invokes `CreateWorkflowAsync(workflow)` with a valid `WorkflowDefinitions` entity
- **THEN** the system persists the entity and returns it with the generated `WorkflowDefinitionId`

#### Scenario: Create workflow violates unique constraint
- **WHEN** a caller invokes `CreateWorkflowAsync(workflow)` with a `WorkflowName`/`Version` combination that already exists
- **THEN** the system throws a `DbUpdateException` wrapping the unique constraint violation

### Requirement: Repository provides workflow update operation
The system SHALL expose a method to load and update an existing `WorkflowDefinitions` entity by ID, applying only the fields that are provided (non-null for reference types, non-default for value types). The method SHALL return the updated entity or null if not found.

#### Scenario: Update an existing workflow
- **WHEN** a caller invokes `UpdateWorkflowAsync(id, workflow)` with a valid ID and updated field values
- **THEN** the system loads the existing entity, applies the changes, persists them, and returns the updated entity

#### Scenario: Update a non-existent workflow
- **WHEN** a caller invokes `UpdateWorkflowAsync(id, workflow)` with an ID that does not exist
- **THEN** the system returns `null`

#### Scenario: Update causes unique constraint violation
- **WHEN** the updated `WorkflowName`/`Version` combination conflicts with another workflow
- **THEN** the system throws a `DbUpdateException`

### Requirement: Repository provides workflow delete operation
The system SHALL expose a method to delete a `WorkflowDefinitions` entity by ID, returning a boolean indicating whether the entity was found and deleted.

#### Scenario: Delete an existing workflow
- **WHEN** a caller invokes `DeleteWorkflowAsync(id)` with a valid ID
- **THEN** the system removes the entity from the database and returns `true`

#### Scenario: Delete a non-existent workflow
- **WHEN** a caller invokes `DeleteWorkflowAsync(id)` with an ID that does not exist
- **THEN** the system returns `false`

### Requirement: Repository provides scenario query operations
The system SHALL expose methods to list all scenarios (optionally filtered by `WorkflowDefinitionId`) and to retrieve a single scenario by ID.

#### Scenario: List all scenarios
- **WHEN** a caller invokes `GetScenariosAsync(workflowId: null)`
- **THEN** the system returns all `WorkflowTestScenarios` entities ordered by `ScenarioName`

#### Scenario: List scenarios filtered by workflow ID
- **WHEN** a caller invokes `GetScenariosAsync(workflowId: 1)`
- **THEN** the system returns only scenarios where `WorkflowDefinitionId == 1` ordered by `ScenarioName`

#### Scenario: No scenarios match filter
- **WHEN** a caller invokes `GetScenariosAsync(workflowId: 999)` and no scenarios exist for that workflow
- **THEN** the system returns an empty collection

#### Scenario: Get scenario by ID
- **WHEN** a caller invokes `GetScenarioByIdAsync(id)` with a valid scenario ID
- **THEN** the system returns the matching `WorkflowTestScenarios` entity

#### Scenario: Get scenario by non-existent ID
- **WHEN** a caller invokes `GetScenarioByIdAsync(id)` with an ID that does not exist
- **THEN** the system returns `null`

### Requirement: Repository provides scenario update operation
The system SHALL expose a method to load and update an existing `WorkflowTestScenarios` entity by ID, applying only the fields that are provided. The `WorkflowDefinitionId` SHALL NOT be updatable.

#### Scenario: Update an existing scenario
- **WHEN** a caller invokes `UpdateScenarioAsync(id, scenario)` with updated fields
- **THEN** the system loads the existing entity, applies only the provided fields (excluding `WorkflowDefinitionId`), persists, and returns the updated entity

#### Scenario: Update a non-existent scenario
- **WHEN** a caller invokes `UpdateScenarioAsync(id, scenario)` with an ID that does not exist
- **THEN** the system returns `null`

### Requirement: Repository provides scenario delete operation
The system SHALL expose a method to delete a `WorkflowTestScenarios` entity by ID, returning a boolean indicating success.

#### Scenario: Delete an existing scenario
- **WHEN** a caller invokes `DeleteScenarioAsync(id)` with a valid ID
- **THEN** the system removes the entity and returns `true`

#### Scenario: Delete a non-existent scenario
- **WHEN** a caller invokes `DeleteScenarioAsync(id)` with an ID that does not exist
- **THEN** the system returns `false`

### Requirement: Repository write operations use change tracking
The system SHALL use EF Core change tracking (NOT `AsNoTracking()`) for create, update, and delete operations to enable proper entity state management and persistence.

#### Scenario: Create operation uses tracked entities
- **WHEN** `CreateWorkflowAsync` adds a new entity via `DbSet<T>.Add()`
- **THEN** the entity is tracked by the change tracker and persisted via `SaveChangesAsync`

#### Scenario: Update operation uses tracked entities
- **WHEN** `UpdateWorkflowAsync` or `UpdateScenarioAsync` loads an entity for update
- **THEN** the entity is tracked and modifications are persisted via `SaveChangesAsync`

#### Scenario: Read queries remain no-tracking
- **WHEN** `GetScenariosAsync` or `GetScenarioByIdAsync` is invoked
- **THEN** the query uses `AsNoTracking()` to avoid change tracking overhead

### Requirement: New repository methods have unit test coverage
The system SHALL have unit tests verifying the behavior of all new repository methods using an in-memory database or mocked `DbContext`.

#### Scenario: CreateWorkflowAsync persists and returns entity with ID
- **WHEN** `CreateWorkflowAsync` is called with a valid workflow entity
- **THEN** the entity is persisted and returned with a non-zero `WorkflowDefinitionId`

#### Scenario: UpdateWorkflowAsync applies partial updates
- **WHEN** `UpdateWorkflowAsync` is called with only `Status` changed
- **THEN** only the `Status` field is modified; other fields remain unchanged

#### Scenario: UpdateWorkflowAsync returns null for missing ID
- **WHEN** `UpdateWorkflowAsync` is called with a non-existent ID
- **THEN** the method returns `null`

#### Scenario: DeleteWorkflowAsync removes entity
- **WHEN** `DeleteWorkflowAsync` is called with an existing ID
- **THEN** the entity is removed from the database and the method returns `true`

#### Scenario: GetScenariosAsync with null workflowId returns all
- **WHEN** `GetScenariosAsync(null)` is called with scenarios for multiple workflows
- **THEN** all scenarios are returned ordered by `ScenarioName`

#### Scenario: GetScenariosAsync with workflowId filters correctly
- **WHEN** `GetScenariosAsync(1)` is called
- **THEN** only scenarios with `WorkflowDefinitionId == 1` are returned

#### Scenario: UpdateScenarioAsync does not change WorkflowDefinitionId
- **WHEN** `UpdateScenarioAsync` is called with an entity that has a different `WorkflowDefinitionId`
- **THEN** the original `WorkflowDefinitionId` is preserved in the database

#### Scenario: DeleteScenarioAsync returns false for missing ID
- **WHEN** `DeleteScenarioAsync` is called with a non-existent ID
- **THEN** the method returns `false`
