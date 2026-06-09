## ADDED Requirements

### Requirement: GET /api/rules endpoint has unit test coverage
The system SHALL have unit tests verifying the behavior of `RulesController.GetAllWorkflows`.

#### Scenario: GetAllWorkflows returns workflows when repository returns data
- **WHEN** `IRepository.GetAllWorkflowsAsync` returns a list of `WorkflowDefinitions`
- **THEN** the controller SHALL return `OkObjectResult` with a `List<WorkflowSummaryResponse>` containing the mapped items

#### Scenario: GetAllWorkflows returns empty list when no workflows exist
- **WHEN** `IRepository.GetAllWorkflowsAsync` returns an empty list
- **THEN** the controller SHALL return `OkObjectResult` with an empty `List<WorkflowSummaryResponse>`

#### Scenario: GetAllWorkflows propagates cancellation token
- **WHEN** the cancellation token is cancelled before the repository returns
- **THEN** the controller SHALL throw `OperationCanceledException`

### Requirement: POST /api/rules/dry-run endpoint has unit test coverage
The system SHALL have unit tests verifying the behavior of `RulesController.DryRun`.

#### Scenario: DryRun returns evaluation result on success
- **WHEN** `IRulesEvaluationService.EvaluateAsync` returns a successful `EvaluationResult`
- **THEN** the controller SHALL return `OkObjectResult` with the `EvaluationResult`

#### Scenario: DryRun returns BadRequest when evaluation fails
- **WHEN** `IRulesEvaluationService.EvaluateAsync` returns an `EvaluationResult` with `IsSuccess == false`
- **THEN** the controller SHALL return `BadRequestObjectResult` with an `error` property containing the `ErrorMessage`

#### Scenario: DryRun propagates cancellation token
- **WHEN** the cancellation token is cancelled during evaluation
- **THEN** the controller SHALL throw `OperationCanceledException`

### Requirement: POST /api/rules/scenarios endpoint has unit test coverage
The system SHALL have unit tests verifying the behavior of `RulesController.SaveScenario`.

#### Scenario: SaveScenario returns 201 with saved scenario
- **WHEN** `IRulesRepository.SaveScenarioAsync` saves a scenario with valid JSON inputs
- **THEN** the controller SHALL return `ObjectResult` with status 201 and the saved `WorkflowTestScenarios` entity

#### Scenario: SaveScenario returns BadRequest when MockInputJson is invalid
- **WHEN** the `SaveScenarioRequest` contains a `MockInputJson` that is not valid JSON
- **THEN** the controller SHALL return `BadRequestObjectResult` with an error message indicating invalid JSON

#### Scenario: SaveScenario returns BadRequest when ExpectedOutputJson is invalid
- **WHEN** the `SaveScenarioRequest` contains an `ExpectedOutputJson` that is not valid JSON
- **THEN** the controller SHALL return `BadRequestObjectResult` with an error message indicating invalid JSON

#### Scenario: SaveScenario accepts null ExpectedOutputJson
- **WHEN** the `SaveScenarioRequest` has `ExpectedOutputJson` set to null
- **THEN** the controller SHALL save the scenario with `ExpectedOutputJson` as null and return 201

#### Scenario: SaveScenario propagates cancellation token
- **WHEN** the cancellation token is cancelled during save
- **THEN** the controller SHALL throw `OperationCanceledException`

### Requirement: Infrastructure tests verify model validation and error handling
The system SHALL have infrastructure tests using `WebApplicationFactory` to verify ASP.NET Core's automatic model validation and error responses.

#### Scenario: Missing required fields return 400 with validation errors
- **WHEN** a client sends `POST /api/rules/dry-run` with an empty JSON body `{}`
- **THEN** the response SHALL be HTTP 400 with a `ProblemDetails` response containing validation errors for missing required fields

#### Scenario: Invalid JSON body returns 400
- **WHEN** a client sends `POST /api/rules/dry-run` with malformed JSON (e.g., `{invalid}`)
- **THEN** the response SHALL be HTTP 400 indicating the request body could not be parsed

#### Scenario: GET /api/rules returns 200 with correct content type
- **WHEN** a client sends `GET /api/rules`
- **THEN** the response SHALL be HTTP 200 with `Content-Type: application/json`
