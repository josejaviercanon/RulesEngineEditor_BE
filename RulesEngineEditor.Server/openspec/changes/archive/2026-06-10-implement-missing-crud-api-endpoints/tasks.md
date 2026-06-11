## 1. Repository Interface — IRulesRepository

- [x] 1.1 Add `CreateWorkflowAsync(WorkflowDefinitions, CancellationToken)` method signature
- [x] 1.2 Add `UpdateWorkflowAsync(int id, WorkflowDefinitions, CancellationToken)` method signature (returns nullable `WorkflowDefinitions?`)
- [x] 1.3 Add `DeleteWorkflowAsync(int id, CancellationToken)` method signature (returns `bool`)
- [x] 1.4 Add `GetScenariosAsync(int? workflowId, CancellationToken)` method signature (returns `List<WorkflowTestScenarios>`)
- [x] 1.5 Add `GetScenarioByIdAsync(int id, CancellationToken)` method signature (returns nullable `WorkflowTestScenarios?`)
- [x] 1.6 Add `UpdateScenarioAsync(int id, WorkflowTestScenarios, CancellationToken)` method signature (returns nullable `WorkflowTestScenarios?`)
- [x] 1.7 Add `DeleteScenarioAsync(int id, CancellationToken)` method signature (returns `bool`)

## 2. Repository Implementation — RulesRepository

- [x] 2.1 Implement `CreateWorkflowAsync`: Add entity to DbSet, set `CreatedAt = DateTime.UtcNow` if null, call `SaveChangesAsync`, return entity with generated ID
- [x] 2.2 Implement `UpdateWorkflowAsync`: Load existing entity by ID (with tracking), apply only non-null/non-default fields from input, call `SaveChangesAsync`, return updated entity. Return `null` if not found
- [x] 2.3 Implement `DeleteWorkflowAsync`: Load entity by ID, remove if found, call `SaveChangesAsync`, return `true` if deleted or `false` if not found
- [x] 2.4 Implement `GetScenariosAsync`: Query with optional `workflowId` filter using EF `Where` clause, apply `AsNoTracking()` and `OrderBy(s => s.ScenarioName)`, return as list
- [x] 2.5 Implement `GetScenarioByIdAsync`: Query by primary key with `AsNoTracking()`, return entity or `null`
- [x] 2.6 Implement `UpdateScenarioAsync`: Load existing by ID (with tracking), copy provided fields, preserve existing `WorkflowDefinitionId`, call `SaveChangesAsync`, return updated entity or `null` if not found
- [x] 2.7 Implement `DeleteScenarioAsync`: Load entity by ID, remove if found, call `SaveChangesAsync`, return `true`/`false`

## 3. DTO Records — RulesController

- [x] 3.1 Add `WorkflowDefinitionResponse` record with properties: `WorkflowDefinitionId`, `WorkflowName`, `Version`, `JsonContent`, `Status`, `CreatedAt`, `CreatedBy`
- [x] 3.2 Add `CreateWorkflowRequest` record with `[Required]` properties: `WorkflowName`, `Version`, `JsonContent`, `Status`
- [x] 3.3 Add `UpdateWorkflowRequest` record with all optional properties: `WorkflowName?`, `Version?`, `JsonContent?`, `Status?`
- [x] 3.4 Add `ScenarioResponse` record with properties: `ScenarioId`, `WorkflowDefinitionId`, `ScenarioName`, `MockInputJson`, `ExpectedOutputJson`, `CreatedAt`
- [x] 3.5 Add `UpdateScenarioRequest` record with optional properties: `ScenarioName?`, `MockInputJson?`, `ExpectedOutputJson?`

## 4. Controller Actions — RulesController (Workflow CRUD)

- [x] 4.1 Add `GET /api/Rules/{id}` action: Call `repository.GetWorkflowByIdAsync`, map to `WorkflowDefinitionResponse`, return 200 or 404
- [x] 4.2 Add `POST /api/Rules` action: Validate `JsonContent` with `IsValidJson`, create entity with `CreatedAt = DateTime.UtcNow`, call `repository.CreateWorkflowAsync`, catch `DbUpdateException` for duplicate constraint and return 409, return 201 with `WorkflowDefinitionResponse`
- [x] 4.3 Add `PUT /api/Rules/{id}` action: If `JsonContent` provided, validate with `IsValidJson` and return 400 if invalid. Call `repository.UpdateWorkflowAsync`, return 200 with mapped response, 404 if null, or 409 on duplicate constraint
- [x] 4.4 Add `DELETE /api/Rules/{id}` action: Call `repository.DeleteWorkflowAsync`, return 204 on `true` or 404 on `false`
- [x] 4.5 Add XML doc comments and `[ProducesResponseType]` attributes on all new workflow actions

## 5. Controller Actions — RulesController (Scenario CRUD)

- [x] 5.1 Add `GET /api/Rules/scenarios` action: Accept optional `[FromQuery] int? workflowId`, call `repository.GetScenariosAsync`, map to `List<ScenarioResponse>`, return 200
- [x] 5.2 Add `GET /api/Rules/scenarios/{id}` action: Call `repository.GetScenarioByIdAsync`, map to `ScenarioResponse`, return 200 or 404
- [x] 5.3 Add `PUT /api/Rules/scenarios/{id}` action: Validate `MockInputJson` and `ExpectedOutputJson` if provided using `IsValidJson`, call `repository.UpdateScenarioAsync`, return 200 with mapped response, 404 if null, 400 if invalid JSON
- [x] 5.4 Add `DELETE /api/Rules/scenarios/{id}` action: Call `repository.DeleteScenarioAsync`, return 204 on `true` or 404 on `false`
- [x] 5.5 Add XML doc comments and `[ProducesResponseType]` attributes on all new scenario actions

## 6. OpenAPI Specification

- [x] 6.1 Update `Api/v1.yaml`: Add new paths for workflow CRUD (`/api/Rules/{id}` GET/PUT/DELETE, `/api/Rules` POST) and scenario CRUD (`/api/Rules/scenarios` GET, `/api/Rules/scenarios/{id}` GET/PUT/DELETE)
- [x] 6.2 Update `Api/v1.yaml`: Add new schema definitions for `WorkflowDefinitionResponse`, `CreateWorkflowRequest`, `UpdateWorkflowRequest`, `ScenarioResponse`, `UpdateScenarioRequest`
- [x] 6.3 Regenerate `Api/v1.json` to match the updated `v1.yaml`

## 7. Unit Tests — Repository Methods

- [x] 7.1 Create test class for `CreateWorkflowAsync`: Test successful creation with ID assignment, test UTC CreatedAt is set
- [x] 7.2 Add tests for `UpdateWorkflowAsync`: Test full update, test partial update (only Status changed preserves other fields), test returns null for missing ID
- [x] 7.3 Add tests for `DeleteWorkflowAsync`: Test successful deletion returns true, test missing ID returns false
- [x] 7.4 Add tests for `GetScenariosAsync`: Test returns all scenarios when no filter, test returns filtered results with workflowId, test returns empty list for non-matching filter
- [x] 7.5 Add tests for `GetScenarioByIdAsync`: Test returns correct scenario, test returns null for missing ID
- [x] 7.6 Add tests for `UpdateScenarioAsync`: Test partial update, test WorkflowDefinitionId is preserved (not overwritten), test returns null for missing ID
- [x] 7.7 Add tests for `DeleteScenarioAsync`: Test successful deletion, test returns false for missing ID
- [x] 7.8 Add test for unique constraint violation scenario: Verify `DbUpdateException` is thrown on duplicate `(WorkflowName, Version)`

## 8. Unit Tests — Controller Actions

- [x] 8.1 Add tests for `GetWorkflowById`: Test returns 200 with `WorkflowDefinitionResponse` when found, test returns 404 when not found, test propagates cancellation token
- [x] 8.2 Add tests for `CreateWorkflow`: Test returns 201 Created on success, test returns 400 for invalid JSON, test returns 409 for duplicate constraint, test propagates cancellation token
- [x] 8.3 Add tests for `UpdateWorkflow`: Test returns 200 on successful update, test returns 400 for invalid JSON, test returns 404 when not found, test returns 409 for duplicate
- [x] 8.4 Add tests for `DeleteWorkflow`: Test returns 204 No Content on success, test returns 404 when not found
- [x] 8.5 Add tests for `GetScenarios`: Test returns 200 with all scenarios, test returns filtered results with workflowId
- [x] 8.6 Add tests for `GetScenarioById`: Test returns 200 when found, test returns 404 when not found
- [x] 8.7 Add tests for `UpdateScenario`: Test returns 200 on success, test returns 400 for invalid JSON, test returns 404 when not found
- [x] 8.8 Add tests for `DeleteScenario`: Test returns 204 on success, test returns 404 when not found

## 9. Integration Tests — Controller Endpoints

- [x] 9.1 Add test: `GET /api/Rules/{id}` returns 200 with correct JSON schema containing `JsonContent`
- [x] 9.2 Add test: `GET /api/Rules/{id}` returns 404 for non-existent workflow
- [x] 9.3 Add test: `POST /api/Rules` returns 201 Created with `Location` header and generated ID
- [x] 9.4 Add test: `POST /api/Rules` returns 400 for invalid JSON in `JsonContent`
- [x] 9.5 Add test: `PUT /api/Rules/{id}` returns 200 with updated fields reflected in response
- [x] 9.6 Add test: `PUT /api/Rules/{id}` returns 404 for non-existent workflow
- [x] 9.7 Add test: `DELETE /api/Rules/{id}` returns 204 with empty body
- [x] 9.8 Add test: `DELETE /api/Rules/{id}` returns 404 for non-existent workflow
- [x] 9.9 Add test: `GET /api/Rules/scenarios` returns 200 with JSON array and correct content type
- [x] 9.10 Add test: `GET /api/Rules/scenarios?workflowId=1` returns only scenarios for that workflow
- [x] 9.11 Add test: `GET /api/Rules/scenarios/{id}` returns 200 or 404
- [x] 9.12 Add test: `PUT /api/Rules/scenarios/{id}` does not change `WorkflowDefinitionId`
- [x] 9.13 Add test: `PUT /api/Rules/scenarios/{id}` returns 400 for invalid JSON in `MockInputJson`
- [x] 9.14 Add test: `DELETE /api/Rules/scenarios/{id}` returns 204 and subsequent GET returns 404

## 10. Verification

- [x] 10.1 Run all unit tests and ensure they pass
- [x] 10.2 Run all integration tests and ensure they pass
- [x] 10.3 Verify existing tests still pass (no regressions)
- [x] 10.4 Verify the application compiles with no warnings
- [x] 10.5 Verify OpenAPI spec is valid (no schema errors) by checking v1.yaml parses correctly
