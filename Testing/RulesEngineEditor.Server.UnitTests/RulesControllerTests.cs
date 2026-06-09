using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RulesEngineEditor.Server.Business.Entities.Models;
using RulesEngineEditor.Server.Business.Services;
using RulesEngineEditor.Server.Controllers;
using RulesEngineEditor.Server.Infrastructure.Repositories;
using Xunit;

namespace RulesEngineEditor.Server.UnitTests;

[Trait("Category", "Unit")]
[ExcludeFromCodeCoverage]
public sealed class RulesControllerTests
{
    private readonly Mock<IRulesRepository> _repositoryMock;
    private readonly Mock<IRulesEvaluationService> _evaluationServiceMock;

    public RulesControllerTests()
    {
        _repositoryMock = new Mock<IRulesRepository>(MockBehavior.Strict);
        _evaluationServiceMock = new Mock<IRulesEvaluationService>(MockBehavior.Strict);
    }

    private RulesController CreateController()
    {
        return new RulesController(_repositoryMock.Object, _evaluationServiceMock.Object);
    }

    [Fact]
    public async Task GetAllWorkflows_WhenWorkflowsExist_Returns200WithList()
    {
        // Arrange
        var workflows = new List<WorkflowDefinitions>
        {
            new()
            {
                WorkflowDefinitionId = 1,
                WorkflowName = "Test Workflow",
                Version = 1,
                Status = "Published",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedBy = "admin",
                JsonContent = "{}"
            }
        };

        _repositoryMock
            .Setup(r => r.GetAllWorkflowsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflows);

        var controller = CreateController();

        // Act
        var result = await controller.GetAllWorkflows(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<WorkflowSummaryResponse>>().Subject;

        response.Should().HaveCount(1);
        var item = response[0];
        item.WorkflowDefinitionId.Should().Be(1);
        item.WorkflowName.Should().Be("Test Workflow");
        item.Version.Should().Be(1);
        item.Status.Should().Be("Published");
        item.CreatedAt.Should().Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        item.CreatedBy.Should().Be("admin");

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task GetAllWorkflows_WhenNoWorkflows_Returns200WithEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAllWorkflowsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkflowDefinitions>());

        var controller = CreateController();

        // Act
        var result = await controller.GetAllWorkflows(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<WorkflowSummaryResponse>>().Subject;
        response.Should().BeEmpty();

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task GetAllWorkflows_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _repositoryMock
            .Setup(r => r.GetAllWorkflowsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var controller = CreateController();

        // Act
        Func<Task> act = () => controller.GetAllWorkflows(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task DryRun_WhenSuccess_Returns200WithResult()
    {
        // Arrange
        var request = new DryRunRequest(
            RulesJson: """{"Rules": []}""",
            FactsJson: """{"fact1": "value1"}""");

        var evaluationResult = new EvaluationResult(true, null, null);

        _evaluationServiceMock
            .Setup(s => s.EvaluateAsync(
                request.RulesJson,
                request.FactsJson,
                request.SettingsJson,
                request.CustomTypes,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationResult);

        var controller = CreateController();

        // Act
        var result = await controller.DryRun(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeSameAs(evaluationResult);

        _evaluationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task DryRun_WhenNotSuccess_Returns400WithError()
    {
        // Arrange
        var request = new DryRunRequest(
            RulesJson: """{"Rules": []}""",
            FactsJson: """{"fact1": "value1"}""");

        var evaluationResult = new EvaluationResult(false, null, "Something went wrong");

        _evaluationServiceMock
            .Setup(s => s.EvaluateAsync(
                request.RulesJson,
                request.FactsJson,
                request.SettingsJson,
                request.CustomTypes,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(evaluationResult);

        var controller = CreateController();

        // Act
        var result = await controller.DryRun(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObj = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObj.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        var errorValue = errorProperty!.GetValue(errorObj)?.ToString();
        errorValue.Should().Be("Something went wrong");

        _evaluationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task DryRun_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var request = new DryRunRequest(
            RulesJson: """{"Rules": []}""",
            FactsJson: """{"fact1": "value1"}""");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _evaluationServiceMock
            .Setup(s => s.EvaluateAsync(
                request.RulesJson,
                request.FactsJson,
                request.SettingsJson,
                request.CustomTypes,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var controller = CreateController();

        // Act
        Func<Task> act = () => controller.DryRun(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        _evaluationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task SaveScenario_WhenValid_Returns201WithScenario()
    {
        // Arrange
        var request = new SaveScenarioRequest(
            WorkflowDefinitionId: 1,
            ScenarioName: "Test Scenario",
            MockInputJson: """{"key": "value"}""",
            ExpectedOutputJson: """{"result": true}""");

        var savedScenario = new WorkflowTestScenarios
        {
            ScenarioId = 1,
            WorkflowDefinitionId = 1,
            ScenarioName = "Test Scenario",
            MockInputJson = """{"key": "value"}""",
            ExpectedOutputJson = """{"result": true}""",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _repositoryMock
            .Setup(r => r.SaveScenarioAsync(It.IsAny<WorkflowTestScenarios>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedScenario);

        var controller = CreateController();

        // Act
        var result = await controller.SaveScenario(request, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
        objectResult.Value.Should().BeSameAs(savedScenario);

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task SaveScenario_WhenMockInputIsInvalidJson_Returns400()
    {
        // Arrange
        var request = new SaveScenarioRequest(
            WorkflowDefinitionId: 1,
            ScenarioName: "Test Scenario",
            MockInputJson: "{invalid-json}",
            ExpectedOutputJson: """{"result": true}""");

        var controller = CreateController();

        // Act
        var result = await controller.SaveScenario(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObj = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObj.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        var errorValue = errorProperty!.GetValue(errorObj)?.ToString();
        errorValue.Should().Contain("MockInputJson");
    }

    [Fact]
    public async Task SaveScenario_WhenExpectedOutputIsInvalidJson_Returns400()
    {
        // Arrange
        var request = new SaveScenarioRequest(
            WorkflowDefinitionId: 1,
            ScenarioName: "Test Scenario",
            MockInputJson: """{"key": "value"}""",
            ExpectedOutputJson: "{invalid-json}");

        var controller = CreateController();

        // Act
        var result = await controller.SaveScenario(request, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObj = badRequestResult.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObj.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        var errorValue = errorProperty!.GetValue(errorObj)?.ToString();
        errorValue.Should().Contain("ExpectedOutputJson");
    }

    [Fact]
    public async Task SaveScenario_WhenExpectedOutputIsNull_Returns201()
    {
        // Arrange
        var request = new SaveScenarioRequest(
            WorkflowDefinitionId: 1,
            ScenarioName: "Test Scenario",
            MockInputJson: """{"key": "value"}""",
            ExpectedOutputJson: null);

        var savedScenario = new WorkflowTestScenarios
        {
            ScenarioId = 1,
            WorkflowDefinitionId = 1,
            ScenarioName = "Test Scenario",
            MockInputJson = """{"key": "value"}""",
            ExpectedOutputJson = null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _repositoryMock
            .Setup(r => r.SaveScenarioAsync(It.IsAny<WorkflowTestScenarios>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedScenario);

        var controller = CreateController();

        // Act
        var result = await controller.SaveScenario(request, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
        objectResult.Value.Should().BeSameAs(savedScenario);

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task SaveScenario_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var request = new SaveScenarioRequest(
            WorkflowDefinitionId: 1,
            ScenarioName: "Test Scenario",
            MockInputJson: """{"key": "value"}""",
            ExpectedOutputJson: """{"result": true}""");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _repositoryMock
            .Setup(r => r.SaveScenarioAsync(It.IsAny<WorkflowTestScenarios>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var controller = CreateController();

        // Act
        Func<Task> act = () => controller.SaveScenario(request, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();

        _repositoryMock.VerifyAll();
    }
}
