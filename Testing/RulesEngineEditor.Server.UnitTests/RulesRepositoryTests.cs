using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RulesEngineEditor.Server.Business.Entities.Models;
using RulesEngineEditor.Server.Infrastructure.Data;
using RulesEngineEditor.Server.Infrastructure.Repositories;
using Xunit;

namespace RulesEngineEditor.Server.UnitTests;

[Trait("Category", "Unit")]
[ExcludeFromCodeCoverage]
public sealed class RulesRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly RulesRepository _repository;

    public RulesRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"RulesEngineTestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new RulesRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private WorkflowDefinitions CreateTestWorkflow(string name = "TestWorkflow", int version = 1, string status = "Active")
    {
        return new WorkflowDefinitions
        {
            WorkflowName = name,
            Version = version,
            JsonContent = """{"rules":[]}""",
            Status = status
        };
    }

    private WorkflowTestScenarios CreateTestScenario(int workflowId, string name = "TestScenario")
    {
        return new WorkflowTestScenarios
        {
            WorkflowDefinitionId = workflowId,
            ScenarioName = name,
            MockInputJson = """{"input":true}""",
            ExpectedOutputJson = """{"output":true}"""
        };
    }

    // ===== CreateWorkflowAsync =====

    [Fact]
    public async Task CreateWorkflowAsync_ValidWorkflow_ReturnsEntityWithGeneratedId()
    {
        // Arrange
        var workflow = CreateTestWorkflow();

        // Act
        var result = await _repository.CreateWorkflowAsync(workflow);

        // Assert
        result.Should().NotBeNull();
        result.WorkflowDefinitionId.Should().BeGreaterThan(0);
        result.WorkflowName.Should().Be("TestWorkflow");
        result.Version.Should().Be(1);
        result.CreatedAt.Should().NotBeNull();
        result.CreatedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task CreateWorkflowAsync_CreatedAtAlreadySet_PreservesValue()
    {
        // Arrange
        var workflow = CreateTestWorkflow();
        workflow.CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await _repository.CreateWorkflowAsync(workflow);

        // Assert
        result.CreatedAt.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    // ===== UpdateWorkflowAsync =====

    [Fact]
    public async Task UpdateWorkflowAsync_ExistingWorkflow_UpdatesAndReturnsEntity()
    {
        // Arrange
        var original = CreateTestWorkflow("Original", 1, "Draft");
        await _repository.CreateWorkflowAsync(original);

        var update = new WorkflowDefinitions { Status = "Active" };

        // Act
        var result = await _repository.UpdateWorkflowAsync(original.WorkflowDefinitionId, update);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Active");
        result.WorkflowName.Should().Be("Original"); // unchanged
    }

    [Fact]
    public async Task UpdateWorkflowAsync_PartialUpdate_OnlyChangesProvidedFields()
    {
        // Arrange
        var original = CreateTestWorkflow("Original", 1, "Draft");
        await _repository.CreateWorkflowAsync(original);

        var update = new WorkflowDefinitions { Status = "Published" };

        // Act
        var result = await _repository.UpdateWorkflowAsync(original.WorkflowDefinitionId, update);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be("Published");
        result.WorkflowName.Should().Be("Original");
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task UpdateWorkflowAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var update = new WorkflowDefinitions { Status = "Active" };

        // Act
        var result = await _repository.UpdateWorkflowAsync(9999, update);

        // Assert
        result.Should().BeNull();
    }

    // ===== DeleteWorkflowAsync =====

    [Fact]
    public async Task DeleteWorkflowAsync_ExistingWorkflow_ReturnsTrue()
    {
        // Arrange
        var workflow = CreateTestWorkflow();
        var saved = await _repository.CreateWorkflowAsync(workflow);

        // Act
        var result = await _repository.DeleteWorkflowAsync(saved.WorkflowDefinitionId);

        // Assert
        result.Should().BeTrue();
        var deleted = await _repository.GetWorkflowByIdAsync(saved.WorkflowDefinitionId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteWorkflowAsync_NonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeleteWorkflowAsync(9999);

        // Assert
        result.Should().BeFalse();
    }

    // ===== GetScenariosAsync =====

    [Fact]
    public async Task GetWorkflowByIdAsync_ExistingId_ReturnsWorkflow()
    {
        // Arrange
        var workflow = CreateTestWorkflow();
        var saved = await _repository.CreateWorkflowAsync(workflow);

        // Act
        var result = await _repository.GetWorkflowByIdAsync(saved.WorkflowDefinitionId);

        // Assert
        result.Should().NotBeNull();
        result!.WorkflowDefinitionId.Should().Be(saved.WorkflowDefinitionId);
        result.JsonContent.Should().Be("""{"rules":[]}""");
    }

    [Fact]
    public async Task GetWorkflowByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetWorkflowByIdAsync(9999);

        // Assert
        result.Should().BeNull();
    }

    // ===== GetScenariosAsync =====

    [Fact]
    public async Task GetScenariosAsync_NoFilter_ReturnsAll()
    {
        // Arrange
        var w1 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W1"));
        var w2 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W2", 2));
        await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "A"));
        await _repository.SaveScenarioAsync(CreateTestScenario(w2.WorkflowDefinitionId, "B"));

        // Act
        var result = await _repository.GetScenariosAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetScenariosAsync_WithWorkflowIdFilter_ReturnsFiltered()
    {
        // Arrange
        var w1 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W1"));
        var w2 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W2", 2));
        await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "A"));
        await _repository.SaveScenarioAsync(CreateTestScenario(w2.WorkflowDefinitionId, "B"));

        // Act
        var result = await _repository.GetScenariosAsync(w1.WorkflowDefinitionId);

        // Assert
        result.Should().HaveCount(1);
        result[0].WorkflowDefinitionId.Should().Be(w1.WorkflowDefinitionId);
    }

    [Fact]
    public async Task GetScenariosAsync_NoMatchingFilter_ReturnsEmpty()
    {
        // Arrange
        var w1 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W1"));
        await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "A"));

        // Act
        var result = await _repository.GetScenariosAsync(9999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetScenariosAsync_ReturnsOrderedByScenarioName()
    {
        // Arrange
        var w1 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W1"));
        await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "C"));
        await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "A"));
        await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "B"));

        // Act
        var result = await _repository.GetScenariosAsync();

        // Assert
        result.Select(s => s.ScenarioName).Should().ContainInOrder("A", "B", "C");
    }

    // ===== GetScenarioByIdAsync =====

    [Fact]
    public async Task GetScenarioByIdAsync_ExistingId_ReturnsScenario()
    {
        // Arrange
        var w1 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W1"));
        var saved = await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "Test"));

        // Act
        var result = await _repository.GetScenarioByIdAsync(saved.ScenarioId);

        // Assert
        result.Should().NotBeNull();
        result!.ScenarioId.Should().Be(saved.ScenarioId);
        result.ScenarioName.Should().Be("Test");
    }

    [Fact]
    public async Task GetScenarioByIdAsync_NonExistentId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetScenarioByIdAsync(9999);

        // Assert
        result.Should().BeNull();
    }

    // ===== UpdateScenarioAsync =====

    [Fact]
    public async Task UpdateScenarioAsync_ExistingScenario_UpdatesAndReturnsEntity()
    {
        // Arrange
        var w1 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W1"));
        var saved = await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "Original"));

        var update = new WorkflowTestScenarios { ScenarioName = "Updated" };

        // Act
        var result = await _repository.UpdateScenarioAsync(saved.ScenarioId, update);

        // Assert
        result.Should().NotBeNull();
        result!.ScenarioName.Should().Be("Updated");
        result.MockInputJson.Should().Be("""{"input":true}"""); // unchanged
    }

    [Fact]
    public async Task UpdateScenarioAsync_PreservesWorkflowDefinitionId()
    {
        // Arrange
        var w1 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W1"));
        var w2 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W2", 2));
        var saved = await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "Original"));

        // Try to change WorkflowDefinitionId — should be ignored
        var update = new WorkflowTestScenarios
        {
            ScenarioName = "Renamed",
            WorkflowDefinitionId = w2.WorkflowDefinitionId
        };

        // Act
        var result = await _repository.UpdateScenarioAsync(saved.ScenarioId, update);

        // Assert
        result.Should().NotBeNull();
        result!.WorkflowDefinitionId.Should().Be(w1.WorkflowDefinitionId); // preserved
        result.ScenarioName.Should().Be("Renamed"); // updated
    }

    [Fact]
    public async Task UpdateScenarioAsync_NonExistentId_ReturnsNull()
    {
        // Arrange
        var update = new WorkflowTestScenarios { ScenarioName = "Updated" };

        // Act
        var result = await _repository.UpdateScenarioAsync(9999, update);

        // Assert
        result.Should().BeNull();
    }

    // ===== DeleteScenarioAsync =====

    [Fact]
    public async Task DeleteScenarioAsync_ExistingScenario_ReturnsTrue()
    {
        // Arrange
        var w1 = await _repository.CreateWorkflowAsync(CreateTestWorkflow("W1"));
        var saved = await _repository.SaveScenarioAsync(CreateTestScenario(w1.WorkflowDefinitionId, "Test"));

        // Act
        var result = await _repository.DeleteScenarioAsync(saved.ScenarioId);

        // Assert
        result.Should().BeTrue();
        var deleted = await _repository.GetScenarioByIdAsync(saved.ScenarioId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteScenarioAsync_NonExistentId_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeleteScenarioAsync(9999);

        // Assert
        result.Should().BeFalse();
    }
}
