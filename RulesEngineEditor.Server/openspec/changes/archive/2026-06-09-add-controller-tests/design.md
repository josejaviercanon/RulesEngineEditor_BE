## Context

The `RulesController` exposes three endpoints (`GET /api/rules`, `POST /api/rules/dry-run`, `POST /api/rules/scenarios`) with dependencies on `IRulesRepository` and `IRulesEvaluationService`. There are currently no tests for any controller, service, or repository in the `RulesEngineEditor.Server` project. The only existing test project lives under `BE.Libraries/RulesEngine/test/RulesEngine.UnitTest/` and covers the third-party RulesEngine library itself.

A dedicated `./Testing/` folder at `src/BE/` will house the new test projects, keeping them co-located with the server project and separate from the library tests. The project uses .NET 10, C# 12+, primary constructors, file-scoped namespaces, and Newtonsoft.Json for specific JSON operations.

## Goals / Non-Goals

**Goals:**
- Create a reusable test project structure under `./Testing/` with consistent conventions
- Achieve full coverage of `RulesController` action methods: `GetAllWorkflows`, `DryRun`, `SaveScenario`
- Test normal (happy-path) responses, validation errors, JSON parse failures, null/edge-case inputs, and cancellation token propagation
- Verify HTTP status codes, response body types, and error shapes match the API spec
- Provide unit tests (pure logic, mocked dependencies) and infrastructure tests (controller factory, real model binding/validation)
- Add code coverage tooling (`coverlet.collector`) to measure and report coverage

**Non-Goals:**
- Writing tests for other controllers (`AdminUsersController`, `WeatherForecastController` — the latter is already commented out)
- Writing tests for `RulesEvaluationService`, `RulesRepository`, or other internal services/repositories directly (those are covered indirectly via the controller tests; dedicated service-level tests are a future concern)
- Writing integration tests that require a real database or external HTTP calls
- Configuring CI/CD pipeline steps — the tests must be runnable locally via `dotnet test` but pipeline wiring is out of scope
- Modifying any production code — tests are purely additive

## Decisions

### Decision: Use xUnit as the test framework
- **Rationale**: The existing library test project (`RulesEngine.UnitTest`) already uses xUnit v2.9.3 with `xunit.runner.visualstudio` v3.0.2. Using the same framework across the repo avoids framework fragmentation, allows shared knowledge, and simplifies tooling. xUnit is also the most widely adopted .NET test framework with excellent IDE and CLI support.
- **Alternatives considered**: MSTest (would introduce a second framework to the repo), NUnit (no existing usage).

### Decision: Use Moq for mocking dependencies
- **Rationale**: Already used in the existing test project (`Moq 4.20.72`). Moq is the standard mocking library for .NET with strong integration in xUnit tests. It cleanly handles the interface-based dependencies (`IRulesRepository`, `IRulesEvaluationService`) used by the controller.
- **Alternatives considered**: NSubstitute (different syntax, not used in repo), FakeItEasy, manual stubs.

### Decision: Use FluentAssertions for readable assertions
- **Rationale**: Provides rich, descriptive assertion failure messages (e.g., `response.StatusCode.Should().Be(HttpStatusCode.OK)`) compared to raw xUnit `Assert.Equal()`. Improves test readability and debugging experience.
- **Alternatives considered**: Plain xUnit assertions (adequate but less expressive), Shouldly.

### Decision: Use Microsoft.AspNetCore.Mvc.Testing for infrastructure tests
- **Rationale**: The `WebApplicationFactory<T>` from `Microsoft.AspNetCore.Mvc.Testing` enables creating a test host that exercises the full ASP.NET Core middleware pipeline (model binding, validation, routing, serialization) without a real database. This allows testing controller-level concerns like `[ApiController]` automatic 400 responses for invalid ModelState, which unit tests (with manually invoked controller methods) cannot reproduce.
- **Alternatives considered**: Only unit tests (misses model binding/validation integration), only integration tests with real DB (too heavy, no DB available in CI).

### Decision: Two separate test projects (UnitTests + InfrastructureTests)
- **Rationale**: Separates fast, isolated unit tests from slower infrastructure tests that bootstrap the WebApplicationFactory. Developers can run just `dotnet test --filter UnitTests` for quick feedback during TDD, and run the full suite before commit. This follows industry convention and keeps build times predictable.
- **Alternatives considered**: Single test project (mixed concerns, slower `dotnet test` cycles), three projects (adding a pure integration test project is overkill at this stage).

### Decision: Tests live in `./Testing/` under the BE root, NOT inside the server project
- **Rationale**: The user explicitly requested `./Testing` folder. Keeping tests outside the server project avoids shipping test dependencies in production deployments and follows standard .NET solution layout patterns. A `Directory.Build.props` in `./Testing/` will centralize common properties (TargetFramework, Nullable, ImplicitUsings).
- **Alternatives considered**: Tests inside server project (mixes concerns), tests alongside server project (different parent requested).

### Decision: Use `coverlet.collector` for code coverage
- **Rationale**: Already used in the existing test project (`coverlet.collector 6.0.4`). Works seamlessly with `dotnet test --collect:"XPlat Code Coverage"` and produces Cobertura/OpenCover reports for CI tools.
- **Alternatives considered**: Fine Code Coverage VS extension (local-only), no coverage tooling.

## Risks / Trade-offs

- **[Risk] Shared `Directory.Build.props` version mismatch**: The test projects must target `net10.0` to match the server project. If `Directory.Build.props` uses a different TFM, the projects won't build. → **Mitigation**: Pin target framework explicitly in each .csproj and use props only for common analyzer/package versions.
- **[Risk] `WebApplicationFactory` requires a Program.cs entry point**: By default, `WebApplicationFactory<T>` references the server project's `Program` class. If `Program.cs` uses top-level statements and implicit `Program` class (which it does), the factory may need `internal` visibility or a `public partial Program` workaround. → **Mitigation**: Add `InternalsVisibleTo` or use the project reference with `<UseWebApplicationFactory>` attribute. The standard .NET 10 approach will be used.
- **[Trade-off] No database integration tests**: The `InfrastructureTests` project uses `WebApplicationFactory` but does not spin up a real SQL Server. Tests that need database interaction (e.g., verifying `SaveScenarioAsync` actually persists) are not covered. This is acceptable because the repository layer is a thin EF Core wrapper that delegates to SQL Server; the controller's orchestration logic (validation, response mapping) is the primary concern.
- **[Risk] Moq and strict vs. loose mocking**: If default loose mocking hides unexpected calls, tests may pass when they should fail. → **Mitigation**: Use `MockBehavior.Strict` for `IRulesEvaluationService` and `IRulesRepository` to ensure all calls are expected and verified.
- **[Risk] `Newtonsoft.Json` within `IsValidJson`**: The controller's `IsValidJson` uses `Newtonsoft.Json.Linq.JToken.Parse`. Infrastructure tests using `WebApplicationFactory` will exercise this through the full pipeline, but unit tests that call the method on an instance need to account for the `private static` method. → **Mitigation**: Unit tests will cover the private method indirectly via `SaveScenario` endpoint behavior; infrastructure tests will cover the JSON validation path through HTTP requests.
