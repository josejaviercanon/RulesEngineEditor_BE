# Rules Engine API

## Purpose

The Rules Engine API exposes RESTful endpoints for listing workflow definitions, executing rules via dry-run evaluation, and saving test scenarios. It follows existing MVC controller conventions and provides HTTP semantics for all operations.

## Requirements

### Requirement: List all workflow definitions via GET endpoint
The system SHALL expose a `GET /api/rules` endpoint that returns a list of all stored workflow definitions with their metadata.

#### Scenario: Successful retrieval of workflows
- **WHEN** a client sends `GET /api/rules`
- **THEN** the system returns HTTP 200 with a JSON array of workflow definitions, each containing `WorkflowDefinitionId`, `WorkflowName`, `Version`, `Status`, `CreatedAt`, and `CreatedBy` fields

#### Scenario: No workflows exist
- **WHEN** a client sends `GET /api/rules` and no workflows are stored
- **THEN** the system returns HTTP 200 with an empty JSON array

### Requirement: Dry-run evaluation via POST endpoint
The system SHALL expose a `POST /api/rules/dry-run` endpoint that accepts a request body containing workflow JSON, facts JSON, and optional settings JSON, and SHALL return the evaluation result tree.

#### Scenario: Successful dry-run evaluation
- **WHEN** a client sends `POST /api/rules/dry-run` with a valid request body containing `RulesJson`, `FactsJson`, and `SettingsJson`
- **THEN** the system returns HTTP 200 with a JSON body containing the `RuleResultTree` and a boolean `IsSuccess` summary

#### Scenario: Missing required fields in dry-run request
- **WHEN** a client sends `POST /api/rules/dry-run` without `RulesJson` or `FactsJson`
- **THEN** the system returns HTTP 400 with model validation errors indicating the missing required fields

#### Scenario: Invalid rules JSON in dry-run request
- **WHEN** a client sends `POST /api/rules/dry-run` with `RulesJson` that is not valid JSON
- **THEN** the system returns HTTP 400 with an error message describing the JSON parse failure

#### Scenario: RulesEngine validation error during dry-run
- **WHEN** a client sends `POST /api/rules/dry-run` with valid JSON that fails RulesEngine workflow validation
- **THEN** the system returns HTTP 400 with the validation error details from `RuleValidationException`

#### Scenario: Dry-run with CustomTypes
- **WHEN** a client sends `POST /api/rules/dry-run` with a `CustomTypes` array containing valid assembly-qualified type names
- **THEN** the system resolves the types and includes them in the `ReSettings.CustomTypes` for evaluation

### Requirement: Save test scenario via POST endpoint
The system SHALL expose a `POST /api/rules/scenarios` endpoint that accepts a test scenario containing a workflow definition reference, scenario name, mock input JSON, and expected output JSON.

#### Scenario: Successfully save a test scenario
- **WHEN** a client sends `POST /api/rules/scenarios` with a valid request body containing `WorkflowDefinitionId`, `ScenarioName`, `MockInputJson`, and `ExpectedOutputJson`
- **THEN** the system returns HTTP 201 Created with the saved scenario object including its generated `ScenarioId`

#### Scenario: Save scenario with missing required fields
- **WHEN** a client sends `POST /api/rules/scenarios` without `WorkflowDefinitionId`, `ScenarioName`, or `MockInputJson`
- **THEN** the system returns HTTP 400 with model validation errors

#### Scenario: Save scenario with invalid JSON fields
- **WHEN** a client sends `POST /api/rules/scenarios` with `MockInputJson` or `ExpectedOutputJson` containing invalid JSON strings
- **THEN** the system returns HTTP 400 with a JSON parse error message

### Requirement: Dry-run request supports CustomTypes array
The system SHALL accept an optional `CustomTypes` string array in the `POST /api/rules/dry-run` request body for dynamic type registration.

#### Scenario: CustomTypes provided and valid
- **WHEN** the request body includes `"CustomTypes": ["SomeNamespace.SomeType, SomeAssembly"]`
- **THEN** the type is resolved and added to the RulesEngine `ReSettings` before evaluation

#### Scenario: CustomTypes not provided
- **WHEN** the request body does NOT include `CustomTypes`
- **THEN** the system evaluates rules without custom types (using only built-in types)

### Requirement: Controller follows existing MVC conventions
The system SHALL implement the `RulesController` as a traditional MVC controller deriving from `ControllerBase` with `[ApiController]` and `[Route("api/[controller]")]` attributes, using primary constructor injection for dependencies.

#### Scenario: Controller route matches convention
- **WHEN** a client sends `GET /api/rules`
- **THEN** the request is routed to `RulesController` and returns workflow definitions

#### Scenario: Controller uses primary constructor injection
- **WHEN** `RulesController` is instantiated by the DI container
- **THEN** dependencies (`IRulesRepository`, `IRulesEvaluationService`) are injected via the primary constructor
