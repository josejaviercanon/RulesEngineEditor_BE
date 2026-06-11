# Workflow CRUD

## Purpose

The Workflow CRUD capability provides full create, read, update, and delete operations for workflow definitions via REST API endpoints, enabling the frontend editor to manage workflows through persistent storage instead of localStorage mock data.

## ADDED Requirements

### Requirement: GET single workflow by ID
The system SHALL expose a `GET /api/rules/{id}` endpoint that returns the full workflow definition including `JsonContent`.

#### Scenario: Successful retrieval by ID
- **WHEN** a client sends `GET /api/rules/1` for an existing workflow
- **THEN** the system returns HTTP 200 with a `WorkflowDefinitionResponse` containing all fields including `JsonContent`

#### Scenario: Workflow not found
- **WHEN** a client sends `GET /api/rules/999` for a non-existent workflow
- **THEN** the system returns HTTP 404 with an error response

### Requirement: POST create new workflow
The system SHALL expose a `POST /api/rules` endpoint that accepts a `CreateWorkflowRequest` body and persists a new workflow definition, returning the created resource with its generated ID.

#### Scenario: Successful workflow creation
- **WHEN** a client sends `POST /api/rules` with valid `WorkflowName`, `Version`, `JsonContent`, and `Status`
- **THEN** the system persists the workflow with `CreatedAt` set to UTC now, and returns HTTP 201 with a `WorkflowDefinitionResponse` including the generated `WorkflowDefinitionId`

#### Scenario: Duplicate unique constraint violation
- **WHEN** a client sends `POST /api/rules` with a `WorkflowName` and `Version` combination that already exists (violates `UQ_Workflow_Version`)
- **THEN** the system returns HTTP 409 Conflict with an error message indicating the duplicate

#### Scenario: Invalid JsonContent
- **WHEN** a client sends `POST /api/rules` with `JsonContent` that is not valid JSON
- **THEN** the system returns HTTP 400 with an error message indicating invalid JSON

#### Scenario: Missing required fields
- **WHEN** a client sends `POST /api/rules` without `WorkflowName`, `Version`, `JsonContent`, or `Status`
- **THEN** the system returns HTTP 400 with model validation errors

### Requirement: PUT update existing workflow
The system SHALL expose a `PUT /api/rules/{id}` endpoint that accepts an `UpdateWorkflowRequest` body and applies partial updates to an existing workflow definition, returning the updated resource.

#### Scenario: Successful partial update
- **WHEN** a client sends `PUT /api/rules/1` with only `Status` set to "Archived"
- **THEN** the system updates only the `Status` field, preserves all other fields, and returns HTTP 200 with the full `WorkflowDefinitionResponse`

#### Scenario: Successful full update
- **WHEN** a client sends `PUT /api/rules/1` with all fields provided
- **THEN** the system updates all provided fields and returns HTTP 200 with the updated `WorkflowDefinitionResponse`

#### Scenario: Update non-existent workflow
- **WHEN** a client sends `PUT /api/rules/999` for a workflow that does not exist
- **THEN** the system returns HTTP 404

#### Scenario: Update violates unique constraint
- **WHEN** a client sends `PUT /api/rules/1` with a `WorkflowName` and `Version` that matches another workflow
- **THEN** the system returns HTTP 409 Conflict

#### Scenario: Invalid JsonContent on update
- **WHEN** a client sends `PUT /api/rules/1` with `JsonContent` that is not valid JSON
- **THEN** the system returns HTTP 400 with an error message indicating invalid JSON

### Requirement: DELETE workflow by ID
The system SHALL expose a `DELETE /api/rules/{id}` endpoint that removes a workflow definition from the database.

#### Scenario: Successful deletion
- **WHEN** a client sends `DELETE /api/rules/1` for an existing workflow
- **THEN** the system removes the workflow and returns HTTP 204 No Content

#### Scenario: Delete non-existent workflow
- **WHEN** a client sends `DELETE /api/rules/999` for a workflow that does not exist
- **THEN** the system returns HTTP 404
