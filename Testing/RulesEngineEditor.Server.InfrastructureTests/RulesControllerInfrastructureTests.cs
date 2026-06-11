using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RulesEngineEditor.Server.Business.Entities.Models;
using RulesEngineEditor.Server.Controllers;
using RulesEngineEditor.Server.Infrastructure.Data;
using Xunit;

namespace RulesEngineEditor.Server.InfrastructureTests;

[Trait("Category", "Infrastructure")]
[ExcludeFromCodeCoverage]
public sealed class RulesControllerInfrastructureTests : IClassFixture<TestingWebAppFactory>
{
    private readonly HttpClient _client;
    private readonly TestingWebAppFactory _factory;

    public RulesControllerInfrastructureTests(TestingWebAppFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Fact]
    public async Task GetAllWorkflows_Returns200WithJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/rules");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task DryRun_WithEmptyBody_Returns400WithValidationErrors()
    {
        // Arrange
        var jsonContent = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/rules/dry-run", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonDocument.Parse(body);

        // Verify it's a ProblemDetails response with model validation errors
        problemDetails.RootElement.TryGetProperty("title", out _).Should().BeTrue();
        problemDetails.RootElement.TryGetProperty("status", out var statusProp).Should().BeTrue();
        statusProp.GetInt32().Should().Be(400);

        // Should contain validation errors for required fields (RulesJson and/or FactsJson)
        problemDetails.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public async Task DryRun_WithMalformedBody_Returns400()
    {
        // Arrange
        var jsonContent = new StringContent("{invalid}", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/rules/dry-run", jsonContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===== GET /api/Rules/{id} =====

    [Fact]
    public async Task GetWorkflowById_WithExistingId_Returns200WithJsonContent()
    {
        // Arrange - seed a workflow
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = new WorkflowDefinitions
        {
            WorkflowName = "IntegrationTest",
            Version = 1,
            JsonContent = """{"rules":[{"ruleName":"test"}]}""",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        db.WorkflowDefinitions.Add(workflow);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/rules/{workflow.WorkflowDefinitionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("workflowDefinitionId").GetInt32().Should().Be(workflow.WorkflowDefinitionId);
        json.RootElement.GetProperty("jsonContent").GetString().Should().Be("""{"rules":[{"ruleName":"test"}]}""");
    }

    [Fact]
    public async Task GetWorkflowById_WithNonExistentId_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/rules/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== POST /api/Rules =====

    [Fact]
    public async Task CreateWorkflow_WithValidRequest_Returns201WithLocation()
    {
        // Arrange
        var request = new CreateWorkflowRequest("NewWorkflow", 1, """{"rules":[]}""", "Draft");
        var content = JsonContent.Create(request);

        // Act
        var response = await _client.PostAsync("/api/rules", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("workflowDefinitionId").GetInt32().Should().BeGreaterThan(0);
        json.RootElement.GetProperty("workflowName").GetString().Should().Be("NewWorkflow");
        json.RootElement.GetProperty("status").GetString().Should().Be("Draft");
    }

    [Fact]
    public async Task CreateWorkflow_WithInvalidJson_Returns400()
    {
        // Arrange
        var content = new StringContent(
            """{"workflowName":"Test","version":1,"jsonContent":"{invalid}","status":"Draft"}""",
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/rules", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===== PUT /api/Rules/{id} =====

    [Fact]
    public async Task UpdateWorkflow_WithValidRequest_Returns200()
    {
        // Arrange - seed a workflow
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = new WorkflowDefinitions
        {
            WorkflowName = "ToUpdate",
            Version = 1,
            JsonContent = """{"rules":[]}""",
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
        db.WorkflowDefinitions.Add(workflow);
        await db.SaveChangesAsync();

        var updateContent = new StringContent(
            """{"status":"Published"}""",
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PutAsync($"/api/rules/{workflow.WorkflowDefinitionId}", updateContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetString().Should().Be("Published");
    }

    [Fact]
    public async Task UpdateWorkflow_WithNonExistentId_Returns404()
    {
        // Arrange
        var content = new StringContent("""{"status":"Published"}""", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync("/api/rules/99999", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== DELETE /api/Rules/{id} =====

    [Fact]
    public async Task DeleteWorkflow_WithExistingId_Returns204()
    {
        // Arrange - seed a workflow
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = new WorkflowDefinitions
        {
            WorkflowName = "ToDelete",
            Version = 1,
            JsonContent = """{"rules":[]}""",
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
        db.WorkflowDefinitions.Add(workflow);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/rules/{workflow.WorkflowDefinitionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteWorkflow_WithNonExistentId_Returns404()
    {
        // Act
        var response = await _client.DeleteAsync("/api/rules/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== GET /api/Rules/scenarios =====

    [Fact]
    public async Task GetScenarios_WithoutFilter_Returns200WithArray()
    {
        // Arrange - seed scenarios
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = new WorkflowDefinitions
        {
            WorkflowName = "ForScenarios",
            Version = 1,
            JsonContent = """{"rules":[]}""",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        db.WorkflowDefinitions.Add(workflow);
        await db.SaveChangesAsync();

        db.WorkflowTestScenarios.Add(new WorkflowTestScenarios
        {
            WorkflowDefinitionId = workflow.WorkflowDefinitionId,
            ScenarioName = "Scenario1",
            MockInputJson = """{"a":1}""",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/rules/scenarios");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task GetScenarios_WithWorkflowIdFilter_ReturnsFiltered()
    {
        // Arrange - seed two workflows with scenarios
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var w1 = new WorkflowDefinitions { WorkflowName = "W1", Version = 1, JsonContent = "{}", Status = "Active", CreatedAt = DateTime.UtcNow };
        var w2 = new WorkflowDefinitions { WorkflowName = "W2", Version = 1, JsonContent = "{}", Status = "Active", CreatedAt = DateTime.UtcNow };
        db.WorkflowDefinitions.AddRange(w1, w2);
        await db.SaveChangesAsync();

        db.WorkflowTestScenarios.AddRange(
            new WorkflowTestScenarios { WorkflowDefinitionId = w1.WorkflowDefinitionId, ScenarioName = "S1", MockInputJson = "{}", CreatedAt = DateTime.UtcNow },
            new WorkflowTestScenarios { WorkflowDefinitionId = w2.WorkflowDefinitionId, ScenarioName = "S2", MockInputJson = "{}", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/rules/scenarios?workflowId={w1.WorkflowDefinitionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetArrayLength().Should().Be(1);
        json.RootElement[0].GetProperty("workflowDefinitionId").GetInt32().Should().Be(w1.WorkflowDefinitionId);
    }

    // ===== GET /api/Rules/scenarios/{id} =====

    [Fact]
    public async Task GetScenarioById_WithExistingId_Returns200()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = new WorkflowDefinitions { WorkflowName = "W", Version = 1, JsonContent = "{}", Status = "Active", CreatedAt = DateTime.UtcNow };
        db.WorkflowDefinitions.Add(workflow);
        await db.SaveChangesAsync();

        var scenario = new WorkflowTestScenarios { WorkflowDefinitionId = workflow.WorkflowDefinitionId, ScenarioName = "GetMe", MockInputJson = "{}", CreatedAt = DateTime.UtcNow };
        db.WorkflowTestScenarios.Add(scenario);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/rules/scenarios/{scenario.ScenarioId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetScenarioById_WithNonExistentId_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/rules/scenarios/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ===== PUT /api/Rules/scenarios/{id} =====

    [Fact]
    public async Task UpdateScenario_PreservesWorkflowDefinitionId()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = new WorkflowDefinitions { WorkflowName = "W", Version = 1, JsonContent = "{}", Status = "Active", CreatedAt = DateTime.UtcNow };
        db.WorkflowDefinitions.Add(workflow);
        await db.SaveChangesAsync();

        var scenario = new WorkflowTestScenarios
        {
            WorkflowDefinitionId = workflow.WorkflowDefinitionId,
            ScenarioName = "PreserveMe",
            MockInputJson = """{"old":true}""",
            CreatedAt = DateTime.UtcNow
        };
        db.WorkflowTestScenarios.Add(scenario);
        await db.SaveChangesAsync();
        var originalWorkflowId = scenario.WorkflowDefinitionId;

        var updateContent = new StringContent(
            """{"scenarioName":"Renamed"}""",
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PutAsync($"/api/rules/scenarios/{scenario.ScenarioId}", updateContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("workflowDefinitionId").GetInt32().Should().Be(originalWorkflowId);
        json.RootElement.GetProperty("scenarioName").GetString().Should().Be("Renamed");
    }

    [Fact]
    public async Task UpdateScenario_WithInvalidJson_Returns400()
    {
        // Arrange - seed a scenario
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = new WorkflowDefinitions { WorkflowName = "W", Version = 1, JsonContent = "{}", Status = "Active", CreatedAt = DateTime.UtcNow };
        db.WorkflowDefinitions.Add(workflow);
        await db.SaveChangesAsync();

        var scenario = new WorkflowTestScenarios { WorkflowDefinitionId = workflow.WorkflowDefinitionId, ScenarioName = "S", MockInputJson = "{}", CreatedAt = DateTime.UtcNow };
        db.WorkflowTestScenarios.Add(scenario);
        await db.SaveChangesAsync();

        var content = new StringContent("""{"mockInputJson":"{invalid}"}""", Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/rules/scenarios/{scenario.ScenarioId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ===== DELETE /api/Rules/scenarios/{id} =====

    [Fact]
    public async Task DeleteScenario_WithExistingId_Returns204AndSubsequentGetReturns404()
    {
        // Arrange - seed a scenario
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var workflow = new WorkflowDefinitions { WorkflowName = "W", Version = 1, JsonContent = "{}", Status = "Active", CreatedAt = DateTime.UtcNow };
        db.WorkflowDefinitions.Add(workflow);
        await db.SaveChangesAsync();

        var scenario = new WorkflowTestScenarios { WorkflowDefinitionId = workflow.WorkflowDefinitionId, ScenarioName = "DeleteMe", MockInputJson = "{}", CreatedAt = DateTime.UtcNow };
        db.WorkflowTestScenarios.Add(scenario);
        await db.SaveChangesAsync();

        // Act - delete
        var deleteResponse = await _client.DeleteAsync($"/api/rules/scenarios/{scenario.ScenarioId}");

        // Assert - delete returns 204
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act - subsequent GET
        var getResponse = await _client.GetAsync($"/api/rules/scenarios/{scenario.ScenarioId}");

        // Assert - subsequent GET returns 404
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
