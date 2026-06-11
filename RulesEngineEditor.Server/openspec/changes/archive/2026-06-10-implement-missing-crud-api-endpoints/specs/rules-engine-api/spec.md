# Rules Engine API (Delta)

## ADDED Requirements

### Requirement: GET single workflow by ID endpoint
The system SHALL expose a `GET /api/rules/{id}` endpoint that returns a full workflow definition including `JsonContent`.

#### Scenario: Retrieve existing workflow
- **WHEN** a client sends `GET /api/rules/{id}` for a valid workflow ID
- **THEN** the system returns HTTP 200 with a `WorkflowDefinitionResponse` containing `WorkflowDefinitionId`, `WorkflowName`, `Version`, `JsonContent`, `Status`, `CreatedAt`, and `CreatedBy`

#### Scenario: Workflow not found
- **WHEN** a client sends `GET /api/rules/999` for an ID that does not exist
- **THEN** the system returns HTTP 404

### Requirement: Create workflow via POST endpoint
The system SHALL expose a `POST /api/rules` endpoint that accepts a `CreateWorkflowRequest` body with `WorkflowName`, `Version`, `JsonContent`, and `Status`, and SHALL persist a new workflow definition with `CreatedAt` set to UTC now.

#### Scenario: Successful workflow creation
- **WHEN** a client sends `POST /api/rules` with all required fields and valid JSON in `JsonContent`
- **THEN** the system returns HTTP 201 Created with a `WorkflowDefinitionResponse` including the generated `WorkflowDefinitionId`

#### Scenario: JsonContent is invalid JSON
- **WHEN** a client sends `POST /api/rules` with `JsonContent` that is not valid JSON
- **THEN** the system returns HTTP 400 with an error message

#### Scenario: Duplicate unique constraint violation
- **WHEN** a client sends `POST /api/rules` with a `WorkflowName` and `Version` combination that already exists
- **THEN** the system returns HTTP 409 Conflict with an error message

### Requirement: Update workflow via PUT endpoint
The system SHALL expose a `PUT /api/rules/{id}` endpoint that accepts an `UpdateWorkflowRequest` body with optional fields and SHALL apply partial updates to the existing workflow.

#### Scenario: Successful partial update
- **WHEN** a client sends `PUT /api/rules/{id}` with a subset of fields
- **THEN** the system updates only the provided fields and returns HTTP 200 with the updated `WorkflowDefinitionResponse`

#### Scenario: Update non-existent workflow
- **WHEN** a client sends `PUT /api/rules/999` for a non-existent ID
- **THEN** the system returns HTTP 404

#### Scenario: Update violates unique constraint
- **WHEN** a client sends `PUT /api/rules/{id}` changing `WorkflowName`/`Version` to match another workflow
- **THEN** the system returns HTTP 409 Conflict

### Requirement: Delete workflow via DELETE endpoint
The system SHALL expose a `DELETE /api/rules/{id}` endpoint that removes a workflow definition.

#### Scenario: Successful deletion
- **WHEN** a client sends `DELETE /api/rules/{id}` for an existing workflow
- **THEN** the system returns HTTP 204 No Content

#### Scenario: Delete non-existent workflow
- **WHEN** a client sends `DELETE /api/rules/999` for a non-existent ID
- **THEN** the system returns HTTP 404

### Requirement: List scenarios with optional workflow filter
The system SHALL expose a `GET /api/rules/scenarios` endpoint that returns all scenarios, optionally filtered by `workflowId` query parameter.

#### Scenario: List all scenarios
- **WHEN** a client sends `GET /api/rules/scenarios`
- **THEN** the system returns HTTP 200 with a JSON array of `ScenarioResponse` objects ordered by `ScenarioName`

#### Scenario: Filter by workflow ID
- **WHEN** a client sends `GET /api/rules/scenarios?workflowId=1`
- **THEN** the system returns HTTP 200 with only scenarios matching `WorkflowDefinitionId = 1`

### Requirement: GET single scenario by ID endpoint
The system SHALL expose a `GET /api/rules/scenarios/{id}` endpoint that returns a single scenario.

#### Scenario: Retrieve existing scenario
- **WHEN** a client sends `GET /api/rules/scenarios/{id}` for a valid scenario ID
- **THEN** the system returns HTTP 200 with a `ScenarioResponse`

#### Scenario: Scenario not found
- **WHEN** a client sends `GET /api/rules/scenarios/999` for a non-existent ID
- **THEN** the system returns HTTP 404

### Requirement: Update scenario via PUT endpoint
The system SHALL expose a `PUT /api/rules/scenarios/{id}` endpoint that accepts an `UpdateScenarioRequest` body and SHALL apply partial updates. The `WorkflowDefinitionId` SHALL NOT be modified.

#### Scenario: Successful partial update
- **WHEN** a client sends `PUT /api/rules/scenarios/{id}` with new field values and valid JSON for JSON fields
- **THEN** the system returns HTTP 200 with the updated `ScenarioResponse`

#### Scenario: Invalid JSON fields
- **WHEN** a client sends `PUT /api/rules/scenarios/{id}` with `MockInputJson` that is not valid JSON
- **THEN** the system returns HTTP 400

#### Scenario: Update non-existent scenario
- **WHEN** a client sends `PUT /api/rules/scenarios/999`
- **THEN** the system returns HTTP 404

### Requirement: Delete scenario via DELETE endpoint
The system SHALL expose a `DELETE /api/rules/scenarios/{id}` endpoint that removes a scenario.

#### Scenario: Successful deletion
- **WHEN** a client sends `DELETE /api/rules/scenarios/{id}` for an existing scenario
- **THEN** the system returns HTTP 204 No Content

#### Scenario: Delete non-existent scenario
- **WHEN** a client sends `DELETE /api/rules/scenarios/999`
- **THEN** the system returns HTTP 404

### Requirement: New controller actions have unit test coverage
The system SHALL have unit tests verifying the behavior of all new controller actions.

#### Scenario: GetWorkflowById returns workflow or 404
- **WHEN** `IRulesRepository.GetWorkflowByIdAsync` returns a workflow or null
- **THEN** the controller SHALL return 200 with `WorkflowDefinitionResponse` or 404

#### Scenario: CreateWorkflow returns 201 on success
- **WHEN** `IRulesRepository.CreateWorkflowAsync` saves a new workflow
- **THEN** the controller SHALL return 201 Created with `WorkflowDefinitionResponse`

#### Scenario: CreateWorkflow returns 400 on invalid JSON
- **WHEN** the `CreateWorkflowRequest` contains invalid `JsonContent`
- **THEN** the controller SHALL return 400 BadRequest

#### Scenario: CreateWorkflow returns 409 on duplicate
- **WHEN** `IRulesRepository.CreateWorkflowAsync` throws `DbUpdateException` for unique constraint violation
- **THEN** the controller SHALL return 409 Conflict

#### Scenario: UpdateWorkflow returns 200 on successful update
- **WHEN** `IRulesRepository.UpdateWorkflowAsync` returns an updated workflow
- **THEN** the controller SHALL return 200 with `WorkflowDefinitionResponse`

#### Scenario: UpdateWorkflow returns 404 when workflow not found
- **WHEN** `IRulesRepository.UpdateWorkflowAsync` returns null
- **THEN** the controller SHALL return 404

#### Scenario: DeleteWorkflow returns 204 on successful deletion
- **WHEN** `IRulesRepository.DeleteWorkflowAsync` returns true
- **THEN** the controller SHALL return 204 No Content

#### Scenario: DeleteWorkflow returns 404 when workflow not found
- **WHEN** `IRulesRepository.DeleteWorkflowAsync` returns false
- **THEN** the controller SHALL return 404

#### Scenario: GetScenarios returns filtered or unfiltered list
- **WHEN** called with or without `workflowId` parameter
- **THEN** the controller SHALL return 200 with the appropriate list of `ScenarioResponse`

#### Scenario: GetScenarioById returns scenario or 404
- **WHEN** `IRulesRepository.GetScenarioByIdAsync` returns a scenario or null
- **THEN** the controller SHALL return 200 or 404

#### Scenario: UpdateScenario returns 200 on success
- **WHEN** `IRulesRepository.UpdateScenarioAsync` returns updated scenario
- **THEN** the controller SHALL return 200 with `ScenarioResponse`

#### Scenario: UpdateScenario returns 404 when scenario not found
- **WHEN** `IRulesRepository.UpdateScenarioAsync` returns null
- **THEN** the controller SHALL return 404

#### Scenario: UpdateScenario returns 400 on invalid JSON
- **WHEN** `MockInputJson` or `ExpectedOutputJson` is not valid JSON
- **THEN** the controller SHALL return 400

#### Scenario: DeleteScenario returns 204 on success
- **WHEN** `IRulesRepository.DeleteScenarioAsync` returns true
- **THEN** the controller SHALL return 204 No Content

#### Scenario: DeleteScenario returns 404 when not found
- **WHEN** `IRulesRepository.DeleteScenarioAsync` returns false
- **THEN** the controller SHALL return 404

### Requirement: Infrastructure tests verify new endpoints
The system SHALL have infrastructure tests using `WebApplicationFactory` to verify HTTP semantics and response shapes for all new endpoints.

#### Scenario: GET /api/rules/{id} returns 200 with correct schema
- **WHEN** a client sends `GET /api/rules/1` for an existing workflow
- **THEN** the response SHALL be HTTP 200 with `Content-Type: application/json` and contain `JsonContent`

#### Scenario: GET /api/rules/{id} returns 404 for missing workflow
- **WHEN** a client sends `GET /api/rules/999`
- **THEN** the response SHALL be HTTP 404

#### Scenario: POST /api/rules returns 201 with created workflow
- **WHEN** a client sends `POST /api/rules` with valid request body
- **THEN** the response SHALL be HTTP 201 with a `Location` header and a JSON body containing the generated `WorkflowDefinitionId`

#### Scenario: POST /api/rules with invalid JSON returns 400
- **WHEN** a client sends `POST /api/rules` with `JsonContent` containing malformed JSON
- **THEN** the response SHALL be HTTP 400

#### Scenario: PUT /api/rules/{id} returns 200 with updated workflow
- **WHEN** a client sends `PUT /api/rules/1` with updated fields
- **THEN** the response SHALL be HTTP 200 with the updated `WorkflowDefinitionResponse`

#### Scenario: DELETE /api/rules/{id} returns 204
- **WHEN** a client sends `DELETE /api/rules/1`
- **THEN** the response SHALL be HTTP 204 with no body

#### Scenario: GET /api/rules/scenarios returns 200 with array
- **WHEN** a client sends `GET /api/rules/scenarios`
- **THEN** the response SHALL be HTTP 200 with `Content-Type: application/json` and a JSON array

#### Scenario: GET /api/rules/scenarios with workflowId filter returns filtered results
- **WHEN** a client sends `GET /api/rules/scenarios?workflowId=1`
- **THEN** the response SHALL be HTTP 200 containing only scenarios for that workflow

#### Scenario: PUT /api/rules/scenarios/{id} does not change WorkflowDefinitionId
- **WHEN** a client sends `PUT /api/rules/scenarios/1` with a request body
- **THEN** the updated scenario SHALL retain its original `WorkflowDefinitionId`

#### Scenario: DELETE /api/rules/scenarios/{id} returns 204
- **WHEN** a client sends `DELETE /api/rules/scenarios/1`
- **THEN** the response SHALL be HTTP 204 with no body
