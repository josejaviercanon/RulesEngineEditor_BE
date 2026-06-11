# Scenario CRUD

## Purpose

The Scenario CRUD capability provides full create, read, update, and delete operations for workflow test scenarios via REST API endpoints, including optional filtering by workflow definition ID. This enables the frontend editor to manage test scenarios through persistent storage.

## ADDED Requirements

### Requirement: GET list all scenarios with optional workflow filter
The system SHALL expose a `GET /api/rules/scenarios` endpoint that returns all test scenarios, optionally filtered by a `workflowId` query parameter.

#### Scenario: List all scenarios
- **WHEN** a client sends `GET /api/rules/scenarios` without query parameters
- **THEN** the system returns HTTP 200 with a JSON array of all `ScenarioResponse` objects, ordered by `ScenarioName`

#### Scenario: Filter scenarios by workflow ID
- **WHEN** a client sends `GET /api/rules/scenarios?workflowId=1`
- **THEN** the system returns HTTP 200 with a JSON array of `ScenarioResponse` objects where `WorkflowDefinitionId` equals 1

#### Scenario: No scenarios match filter
- **WHEN** a client sends `GET /api/rules/scenarios?workflowId=999` and no scenarios exist for that workflow
- **THEN** the system returns HTTP 200 with an empty JSON array

### Requirement: GET single scenario by ID
The system SHALL expose a `GET /api/rules/scenarios/{id}` endpoint that returns a single test scenario by its primary key.

#### Scenario: Successful retrieval
- **WHEN** a client sends `GET /api/rules/scenarios/1` for an existing scenario
- **THEN** the system returns HTTP 200 with a `ScenarioResponse` object containing all fields

#### Scenario: Scenario not found
- **WHEN** a client sends `GET /api/rules/scenarios/999` for a non-existent scenario
- **THEN** the system returns HTTP 404

### Requirement: PUT update existing scenario
The system SHALL expose a `PUT /api/rules/scenarios/{id}` endpoint that accepts an `UpdateScenarioRequest` body and applies partial updates to an existing scenario. The `WorkflowDefinitionId` SHALL NOT be updatable.

#### Scenario: Successful partial update
- **WHEN** a client sends `PUT /api/rules/scenarios/1` with only `ScenarioName` set to a new name
- **THEN** the system updates only the `ScenarioName`, preserves `WorkflowDefinitionId` and other fields, and returns HTTP 200 with the updated `ScenarioResponse`

#### Scenario: Successful update of JSON fields
- **WHEN** a client sends `PUT /api/rules/scenarios/1` with new `MockInputJson` and `ExpectedOutputJson`
- **THEN** the system validates both are valid JSON and returns HTTP 200 with the updated `ScenarioResponse`

#### Scenario: Update non-existent scenario
- **WHEN** a client sends `PUT /api/rules/scenarios/999` for a scenario that does not exist
- **THEN** the system returns HTTP 404

#### Scenario: Invalid JSON fields on update
- **WHEN** a client sends `PUT /api/rules/scenarios/1` with `MockInputJson` containing invalid JSON
- **THEN** the system returns HTTP 400 with an error message indicating invalid JSON

### Requirement: DELETE scenario by ID
The system SHALL expose a `DELETE /api/rules/scenarios/{id}` endpoint that removes a test scenario from the database.

#### Scenario: Successful deletion
- **WHEN** a client sends `DELETE /api/rules/scenarios/1` for an existing scenario
- **THEN** the system removes the scenario and returns HTTP 204 No Content

#### Scenario: Delete non-existent scenario
- **WHEN** a client sends `DELETE /api/rules/scenarios/999` for a scenario that does not exist
- **THEN** the system returns HTTP 404
