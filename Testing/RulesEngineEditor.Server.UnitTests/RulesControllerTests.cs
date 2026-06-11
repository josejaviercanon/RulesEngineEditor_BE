using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    // ===== GetWorkflowById =====

    [Fact]
    public async Task GetWorkflowById_WhenWorkflowExists_Returns200WithWorkflow()
    {
        // Arrange
        var workflow = new WorkflowDefinitions
        {
            WorkflowDefinitionId = 1,
            WorkflowName = "Test",
            Version = 1,
            JsonContent = """{"rules":[]}""",
            Status = "Active",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "admin"
        };

        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        var controller = CreateController();

        // Act
        var result = await controller.GetWorkflowById(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<WorkflowDefinitionResponse>().Subject;
        response.WorkflowDefinitionId.Should().Be(1);
        response.WorkflowName.Should().Be("Test");
        response.JsonContent.Should().Be("""{"rules":[]}""");

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task GetWorkflowById_WhenNotFound_Returns404()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetWorkflowByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDefinitions?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetWorkflowById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _repositoryMock.VerifyAll();
    }

    // ===== CreateWorkflow =====

    [Fact]
    public async Task CreateWorkflow_WhenValid_Returns201()
    {
        // Arrange
        var request = new CreateWorkflowRequest("Test", 1, """{"rules":[]}""", "Active");

        var savedWorkflow = new WorkflowDefinitions
        {
            WorkflowDefinitionId = 1,
            WorkflowName = "Test",
            Version = 1,
            JsonContent = """{"rules":[]}""",
            Status = "Active",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = null
        };

        _repositoryMock
            .Setup(r => r.CreateWorkflowAsync(It.IsAny<WorkflowDefinitions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedWorkflow);

        var controller = CreateController();

        // Act
        var result = await controller.CreateWorkflow(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        var response = createdResult.Value.Should().BeAssignableTo<WorkflowDefinitionResponse>().Subject;
        response.WorkflowDefinitionId.Should().Be(1);

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task CreateWorkflow_WithInvalidJsonContent_Returns400()
    {
        // Arrange
        var request = new CreateWorkflowRequest("Test", 1, "{invalid}", "Active");

        var controller = CreateController();

        // Act
        var result = await controller.CreateWorkflow(request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObj = badRequest.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObj.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObj)?.ToString().Should().Contain("JsonContent");
    }

    [Fact]
    public async Task CreateWorkflow_WhenDuplicateConstraint_Returns409()
    {
        // Arrange
        var request = new CreateWorkflowRequest("Test", 1, """{"rules":[]}""", "Active");

        var innerException = new Exception("Violation of UNIQUE KEY constraint 'UQ_Workflow_Version'");
        var dbUpdateException = new DbUpdateException("Error", innerException);

        _repositoryMock
            .Setup(r => r.CreateWorkflowAsync(It.IsAny<WorkflowDefinitions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(dbUpdateException);

        var controller = CreateController();

        // Act
        var result = await controller.CreateWorkflow(request, CancellationToken.None);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);

        _repositoryMock.VerifyAll();
    }

    // ===== UpdateWorkflow =====

    [Fact]
    public async Task UpdateWorkflow_WhenValid_Returns200()
    {
        // Arrange
        var request = new UpdateWorkflowRequest(Status: "Archived");

        var updatedWorkflow = new WorkflowDefinitions
        {
            WorkflowDefinitionId = 1,
            WorkflowName = "Test",
            Version = 1,
            JsonContent = """{"rules":[]}""",
            Status = "Archived",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedBy = "admin"
        };

        _repositoryMock
            .Setup(r => r.UpdateWorkflowAsync(1, It.IsAny<WorkflowDefinitions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedWorkflow);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateWorkflow(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<WorkflowDefinitionResponse>().Subject;
        response.Status.Should().Be("Archived");

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateWorkflow_WhenNotFound_Returns404()
    {
        // Arrange
        var request = new UpdateWorkflowRequest(Status: "Archived");

        _repositoryMock
            .Setup(r => r.UpdateWorkflowAsync(999, It.IsAny<WorkflowDefinitions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDefinitions?)null);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateWorkflow(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateWorkflow_WithInvalidJson_Returns400()
    {
        // Arrange
        var request = new UpdateWorkflowRequest(JsonContent: "{invalid}");

        var controller = CreateController();

        // Act
        var result = await controller.UpdateWorkflow(1, request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObj = badRequest.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObj.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObj)?.ToString().Should().Contain("JsonContent");
    }

    // ===== DeleteWorkflow =====

    [Fact]
    public async Task DeleteWorkflow_WhenExists_Returns204()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteWorkflowAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteWorkflow(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task DeleteWorkflow_WhenNotFound_Returns404()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteWorkflowAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteWorkflow(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _repositoryMock.VerifyAll();
    }

    // ===== GetScenarios =====

    [Fact]
    public async Task GetScenarios_WithoutFilter_Returns200WithAll()
    {
        // Arrange
        var scenarios = new List<WorkflowTestScenarios>
        {
            new() { ScenarioId = 1, WorkflowDefinitionId = 1, ScenarioName = "A", MockInputJson = "{}", ExpectedOutputJson = null, CreatedAt = DateTime.UtcNow },
            new() { ScenarioId = 2, WorkflowDefinitionId = 1, ScenarioName = "B", MockInputJson = "{}", ExpectedOutputJson = "{}", CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock
            .Setup(r => r.GetScenariosAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenarios);

        var controller = CreateController();

        // Act
        var result = await controller.GetScenarios(null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<ScenarioResponse>>().Subject;
        response.Should().HaveCount(2);

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task GetScenarios_WithWorkflowIdFilter_ReturnsFiltered()
    {
        // Arrange
        var scenarios = new List<WorkflowTestScenarios>
        {
            new() { ScenarioId = 1, WorkflowDefinitionId = 1, ScenarioName = "A", MockInputJson = "{}", ExpectedOutputJson = null, CreatedAt = DateTime.UtcNow }
        };

        _repositoryMock
            .Setup(r => r.GetScenariosAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenarios);

        var controller = CreateController();

        // Act
        var result = await controller.GetScenarios(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<ScenarioResponse>>().Subject;
        response.Should().HaveCount(1);
        response[0].WorkflowDefinitionId.Should().Be(1);

        _repositoryMock.VerifyAll();
    }

    // ===== GetScenarioById =====

    [Fact]
    public async Task GetScenarioById_WhenExists_Returns200()
    {
        // Arrange
        var scenario = new WorkflowTestScenarios
        {
            ScenarioId = 1,
            WorkflowDefinitionId = 1,
            ScenarioName = "Test",
            MockInputJson = """{"key":"value"}""",
            ExpectedOutputJson = """{"result":true}""",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _repositoryMock
            .Setup(r => r.GetScenarioByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scenario);

        var controller = CreateController();

        // Act
        var result = await controller.GetScenarioById(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<ScenarioResponse>().Subject;
        response.ScenarioId.Should().Be(1);
        response.ScenarioName.Should().Be("Test");

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task GetScenarioById_WhenNotFound_Returns404()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetScenarioByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTestScenarios?)null);

        var controller = CreateController();

        // Act
        var result = await controller.GetScenarioById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _repositoryMock.VerifyAll();
    }

    // ===== UpdateScenario =====

    [Fact]
    public async Task UpdateScenario_WhenValid_Returns200()
    {
        // Arrange
        var request = new UpdateScenarioRequest(ScenarioName: "Updated");

        var updatedScenario = new WorkflowTestScenarios
        {
            ScenarioId = 1,
            WorkflowDefinitionId = 1,
            ScenarioName = "Updated",
            MockInputJson = """{"key":"value"}""",
            ExpectedOutputJson = null,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        _repositoryMock
            .Setup(r => r.UpdateScenarioAsync(1, It.IsAny<WorkflowTestScenarios>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedScenario);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateScenario(1, request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<ScenarioResponse>().Subject;
        response.ScenarioName.Should().Be("Updated");

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateScenario_WhenNotFound_Returns404()
    {
        // Arrange
        var request = new UpdateScenarioRequest(ScenarioName: "Updated");

        _repositoryMock
            .Setup(r => r.UpdateScenarioAsync(999, It.IsAny<WorkflowTestScenarios>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowTestScenarios?)null);

        var controller = CreateController();

        // Act
        var result = await controller.UpdateScenario(999, request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task UpdateScenario_WithInvalidMockInputJson_Returns400()
    {
        // Arrange
        var request = new UpdateScenarioRequest(MockInputJson: "{invalid}");

        var controller = CreateController();

        // Act
        var result = await controller.UpdateScenario(1, request, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorObj = badRequest.Value.Should().BeAssignableTo<object>().Subject;
        var errorProperty = errorObj.GetType().GetProperty("error");
        errorProperty.Should().NotBeNull();
        errorProperty!.GetValue(errorObj)?.ToString().Should().Contain("MockInputJson");
    }

    // ===== DeleteScenario =====

    [Fact]
    public async Task DeleteScenario_WhenExists_Returns204()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteScenarioAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteScenario(1, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _repositoryMock.VerifyAll();
    }

    [Fact]
    public async Task DeleteScenario_WhenNotFound_Returns404()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.DeleteScenarioAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController();

        // Act
        var result = await controller.DeleteScenario(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _repositoryMock.VerifyAll();
    }
}
