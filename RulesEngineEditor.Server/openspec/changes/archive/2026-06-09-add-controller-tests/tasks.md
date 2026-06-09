## 1. Create Testing Folder Structure and Project Files

- [x] 1.1 Create `./Testing/` directory at `src/BE/` with a `Directory.Build.props` that sets `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, and adds common NuGet packages (`xunit`, `xunit.runner.visualstudio`, `Moq`, `FluentAssertions`, `coverlet.collector`) with consistent versions
- [x] 1.2 Create `./Testing/RulesEngineEditor.Server.UnitTests/RulesEngineEditor.Server.UnitTests.csproj` targeting `net10.0` with project reference to `RulesEngineEditor.Server.csproj` and NuGet references to `xunit` (2.9.3), `xunit.runner.visualstudio` (3.0.2), `Moq` (4.20.72), `FluentAssertions`, `coverlet.collector`, and `Microsoft.NET.Test.Sdk`
- [x] 1.3 Create `./Testing/RulesEngineEditor.Server.InfrastructureTests/RulesEngineEditor.Server.InfrastructureTests.csproj` targeting `net10.0` with project reference to `RulesEngineEditor.Server.csproj` and NuGet references to `xunit`, `xunit.runner.visualstudio`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`, `coverlet.collector`, and `Microsoft.NET.Test.Sdk`

## 2. Create Unit Test Classes for RulesController

- [x] 2.1 Create `RulesControllerTests.cs` in the UnitTests project with a sealed test class using `[Trait("Category", "Unit")]` and `[ExcludeFromCodeCoverage]`, with private fields for mocked `IRulesRepository` and `IRulesEvaluationService`, a constructor that creates the mocked instances with `MockBehavior.Strict`, and a factory method to create the controller instance
- [x] 2.2 Write unit test `GetAllWorkflows_WhenWorkflowsExist_Returns200WithList` that mocks `repository.GetAllWorkflowsAsync` to return a list of `WorkflowDefinitions`, invokes the controller action, and asserts `OkObjectResult` with a `List<WorkflowSummaryResponse>` containing correct mapped values
- [x] 2.3 Write unit test `GetAllWorkflows_WhenNoWorkflows_Returns200WithEmptyList` that mocks `repository.GetAllWorkflowsAsync` to return an empty list, invokes the controller action, and asserts `OkObjectResult` with an empty `List<WorkflowSummaryResponse>`
- [x] 2.4 Write unit test `GetAllWorkflows_WhenCancelled_ThrowsOperationCanceledException` that creates a cancelled `CancellationToken`, invokes the controller action, and asserts `OperationCanceledException` is thrown
- [x] 2.5 Write unit test `DryRun_WhenSuccess_Returns200WithResult` that mocks `evaluationService.EvaluateAsync` to return a successful `EvaluationResult`, invokes the controller with a valid `DryRunRequest`, and asserts `OkObjectResult` containing the `EvaluationResult`
- [x] 2.6 Write unit test `DryRun_WhenNotSuccess_Returns400WithError` that mocks `evaluationService.EvaluateAsync` to return a failed `EvaluationResult` with an error message, invokes the controller, and asserts `BadRequestObjectResult` with an `error` property matching the message
- [x] 2.7 Write unit test `DryRun_WhenCancelled_ThrowsOperationCanceledException` that creates a cancelled `CancellationToken`, invokes the controller action with a valid `DryRunRequest`, and asserts `OperationCanceledException` is thrown
- [x] 2.8 Write unit test `SaveScenario_WhenValid_Returns201WithScenario` that mocks `repository.SaveScenarioAsync` to return a saved `WorkflowTestScenarios`, invokes the controller with a valid `SaveScenarioRequest`, and asserts `ObjectResult` with status 201 containing the saved entity
- [x] 2.9 Write unit test `SaveScenario_WhenMockInputIsInvalidJson_Returns400` that invokes the controller with `MockInputJson` containing invalid JSON and asserts `BadRequestObjectResult` with an error message about invalid JSON
- [x] 2.10 Write unit test `SaveScenario_WhenExpectedOutputIsInvalidJson_Returns400` that invokes the controller with valid `MockInputJson` but invalid `ExpectedOutputJson` and asserts `BadRequestObjectResult` with an error message about invalid JSON
- [x] 2.11 Write unit test `SaveScenario_WhenExpectedOutputIsNull_Returns201` that invokes the controller with `ExpectedOutputJson` set to `null` and asserts `ObjectResult` with status 201
- [x] 2.12 Write unit test `SaveScenario_WhenCancelled_ThrowsOperationCanceledException` that creates a cancelled `CancellationToken`, invokes the controller, and asserts `OperationCanceledException` is thrown

## 3. Create Infrastructure Test Classes for RulesController

- [x] 3.1 Create a `TestingWebAppFactory.cs` or configure a base class that creates a `WebApplicationFactory<Program>` with optional service overrides (e.g., replacing DbContext registrations with in-memory or stubs to avoid requiring a real SQL Server connection)
- [x] 3.2 Create `RulesControllerInfrastructureTests.cs` with a sealed test class using `[Trait("Category", "Infrastructure")]` and a class fixture or constructor that creates the `WebApplicationFactory`
- [x] 3.3 Write infrastructure test `GetAllWorkflows_Returns200WithJsonContentType` that sends `GET /api/rules` to the test server via `HttpClient` and asserts HTTP 200 with `Content-Type: application/json`
- [x] 3.4 Write infrastructure test `DryRun_WithEmptyBody_Returns400WithValidationErrors` that sends `POST /api/rules/dry-run` with an empty JSON body `{}` and asserts HTTP 400 with a `ProblemDetails` response containing validation error details for `RulesJson` and/or `FactsJson`
- [x] 3.5 Write infrastructure test `DryRun_WithMalformedBody_Returns400` that sends `POST /api/rules/dry-run` with malformed JSON `{invalid}` and asserts HTTP 400

## 4. Build and Verify

- [x] 4.1 Run `dotnet build` on both test projects to verify compilation succeeds
- [x] 4.2 Run `dotnet test` on the UnitTests project to verify all unit tests pass
- [x] 4.3 Run `dotnet test` on the InfrastructureTests project to verify all infrastructure tests pass
- [x] 4.4 Run `dotnet test --collect:"XPlat Code Coverage"` from `./Testing/` to verify coverage reports are generated
