## ADDED Requirements

### Requirement: Test project layout under ./Testing/
The system SHALL create two test projects under `./Testing/` at the `src/BE/` root: `RulesEngineEditor.Server.UnitTests` and `RulesEngineEditor.Server.InfrastructureTests`.

#### Scenario: UnitTests project follows standard conventions
- **WHEN** a developer opens `./Testing/RulesEngineEditor.Server.UnitTests/RulesEngineEditor.Server.UnitTests.csproj`
- **THEN** the project SHALL target `net10.0`, enable nullable reference types, use implicit usings, and reference `xunit`, `Moq`, `FluentAssertions`, and the server project as a `ProjectReference`

#### Scenario: InfrastructureTests project references WebApplicationFactory
- **WHEN** a developer opens `./Testing/RulesEngineEditor.Server.InfrastructureTests/RulesEngineEditor.Server.InfrastructureTests.csproj`
- **THEN** the project SHALL reference `Microsoft.AspNetCore.Mvc.Testing` and the server project, and SHALL target `net10.0`

#### Scenario: Directory.Build.props centralizes common settings
- **WHEN** a developer inspects `./Testing/Directory.Build.props`
- **THEN** it SHALL define `TargetFramework` as `net10.0`, enable `Nullable`, `ImplicitUsings`, and common analyzer versions (e.g., `coverlet.collector`)

### Requirement: Code coverage tooling
The system SHALL include `coverlet.collector` as a dependency in both test projects and SHALL support generating coverage reports via the standard `dotnet test --collect:"XPlat Code Coverage"` command.

#### Scenario: Coverage collection works
- **WHEN** a developer runs `dotnet test --collect:"XPlat Code Coverage"` from `./Testing/`
- **THEN** a Cobertura coverage report SHALL be generated in the `TestResults` directory

### Requirement: Unit tests verify controller logic in isolation
The `RulesEngineEditor.Server.UnitTests` project SHALL test `RulesController` action methods by instantiating the controller with mocked `IRulesRepository` and `IRulesEvaluationService` dependencies.

#### Scenario: Dependencies are mocked with Moq
- **WHEN** a unit test creates the controller under test
- **THEN** `IRulesRepository` and `IRulesEvaluationService` SHALL be mocked using `Mock<T>` with `MockBehavior.Strict`

#### Scenario: Test names follow consistent convention
- **WHEN** a unit test method is inspected
- **THEN** its name SHALL follow the pattern `{MethodName}_{Scenario}_{ExpectedOutcome}` (e.g., `GetAllWorkflows_WhenWorkflowsExist_Returns200WithList`)

### Requirement: Infrastructure tests verify ASP.NET Core pipeline integration
The `RulesEngineEditor.Server.InfrastructureTests` project SHALL use `WebApplicationFactory<Program>` to exercise the full middleware stack including routing, model binding, validation, serialization, and error handling.

#### Scenario: WebApplicationFactory creates a test server
- **WHEN** an infrastructure test creates a `WebApplicationFactory<Program>`
- **THEN** the factory SHALL host the server project's full middleware pipeline without requiring a real database connection

#### Scenario: Tests use HttpClient for HTTP requests
- **WHEN** an infrastructure test sends an HTTP request to the test server
- **THEN** the request SHALL be sent via `HttpClient` obtained from `WebApplicationFactory.CreateClient()`
