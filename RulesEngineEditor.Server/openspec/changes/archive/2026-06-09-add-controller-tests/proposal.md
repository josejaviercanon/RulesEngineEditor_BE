## Why

The `RulesController` — the primary API surface for the Rules Engine — has zero test coverage. Without tests, regressions in existing endpoints (`GET /api/rules`, `POST /api/rules/dry-run`, `POST /api/rules/scenarios`) go undetected during refactoring or feature additions. Establishing a test project and a comprehensive test suite now ensures the controller's behavior is verified, documented, and protected against regressions as the codebase evolves.

## What Changes

- Create a new test solution (or extend the existing solution) at `./Testing/` housing two test projects:
  - `RulesEngineEditor.Server.UnitTests` — Pure unit tests for `RulesController` action methods, mocking `IRulesRepository` and `IRulesEvaluationService`
  - `RulesEngineEditor.Server.InfrastructureTests` — Integration-style tests that verify controller-to-service-to-repository wiring, request validation, and error handling paths
- Add NuGet test dependencies (`xUnit`, `Moq`, `FluentAssertions`, `Microsoft.AspNetCore.Mvc.Testing`) to the new projects
- Write tests covering all three `RulesController` endpoints: `GetAllWorkflows`, `DryRun`, `SaveScenario`
- Cover normal paths, validation errors, JSON parsing failures, null/empty inputs, and cancellation
- Add `coverlet.collector` for code coverage reporting
- Add a `Directory.Build.props` in `./Testing/` for consistent test project configuration

## Capabilities

### New Capabilities
- `controller-testing`: Test project structure, conventions, mock setup, and code coverage infrastructure for the Rules Engine Editor Web API controllers

### Modified Capabilities
- `rules-engine-api`: Add testing requirements to the existing API spec — each endpoint requirement will reference its corresponding test scenarios

## Impact

- New `./Testing/` folder at the solution root `src/BE/` containing two `.csproj` files and a shared `Directory.Build.props`
- No changes to production code — tests are additive only
- Existing `RulesEngine.sln` solution may be extended, or a new `.sln` created in `./Testing/` if preferred
- No impact on existing CI/CD pipelines until tests are wired in (future task)
