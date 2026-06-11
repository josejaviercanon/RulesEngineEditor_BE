using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RulesEngineEditor.Server.Business.Entities.Models;
using RulesEngineEditor.Server.Business.Services;
using RulesEngineEditor.Server.Infrastructure.Repositories;

namespace RulesEngineEditor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Intentional for RulesEngine Lab; auth can be layered in a future change.
public sealed class RulesController(
    IRulesRepository repository,
    IRulesEvaluationService evaluationService) : ControllerBase
{
    /// <summary>
    /// Returns all stored workflow definitions with metadata.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<WorkflowSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllWorkflows(CancellationToken ct)
    {
        var workflows = await repository.GetAllWorkflowsAsync(ct);

        var response = workflows.Select(w => new WorkflowSummaryResponse(
            w.WorkflowDefinitionId,
            w.WorkflowName,
            w.Version,
            w.Status,
            w.CreatedAt,
            w.CreatedBy)).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Returns a single workflow definition by ID, including full JsonContent.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(WorkflowDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflowById(int id, CancellationToken ct)
    {
        var workflow = await repository.GetWorkflowByIdAsync(id, ct);

        if (workflow is null)
        {
            return NotFound();
        }

        var response = new WorkflowDefinitionResponse(
            workflow.WorkflowDefinitionId,
            workflow.WorkflowName,
            workflow.Version,
            workflow.JsonContent,
            workflow.Status,
            workflow.CreatedAt,
            workflow.CreatedBy);

        return Ok(response);
    }

    /// <summary>
    /// Creates a new workflow definition.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType(typeof(WorkflowDefinitionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWorkflow([FromBody] CreateWorkflowRequest request, CancellationToken ct)
    {
        // Validate JSON content
        if (!IsValidJson(request.JsonContent))
        {
            return BadRequest(new { error = "JsonContent is not valid JSON." });
        }

        var workflow = new WorkflowDefinitions
        {
            WorkflowName = request.WorkflowName,
            Version = request.Version,
            JsonContent = request.JsonContent,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var saved = await repository.CreateWorkflowAsync(workflow, ct);

            var response = new WorkflowDefinitionResponse(
                saved.WorkflowDefinitionId,
                saved.WorkflowName,
                saved.Version,
                saved.JsonContent,
                saved.Status,
                saved.CreatedAt,
                saved.CreatedBy);

            return CreatedAtAction(nameof(GetWorkflowById), new { id = saved.WorkflowDefinitionId }, response);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { error = "A workflow with the same Name and Version already exists." });
        }
    }

    /// <summary>
    /// Updates an existing workflow definition. Only provided fields are updated.
    /// </summary>
    [HttpPut("{id:int}")]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType(typeof(WorkflowDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateWorkflow(int id, [FromBody] UpdateWorkflowRequest request, CancellationToken ct)
    {
        // Validate JSON content if provided
        if (request.JsonContent is not null && !IsValidJson(request.JsonContent))
        {
            return BadRequest(new { error = "JsonContent is not valid JSON." });
        }

        var updateEntity = new WorkflowDefinitions
        {
            // Null-forgiving: the repository checks for null before assigning
            WorkflowName = request.WorkflowName!,
            Version = request.Version ?? default,
            JsonContent = request.JsonContent!,
            Status = request.Status!
        };

        try
        {
            var updated = await repository.UpdateWorkflowAsync(id, updateEntity, ct);

            if (updated is null)
            {
                return NotFound();
            }

            var response = new WorkflowDefinitionResponse(
                updated.WorkflowDefinitionId,
                updated.WorkflowName,
                updated.Version,
                updated.JsonContent,
                updated.Status,
                updated.CreatedAt,
                updated.CreatedBy);

            return Ok(response);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { error = "A workflow with the same Name and Version already exists." });
        }
    }

    /// <summary>
    /// Deletes a workflow definition by ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkflow(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteWorkflowAsync(id, ct);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Executes a dry-run evaluation of the provided rules against the provided facts.
    /// </summary>
    [HttpPost("dry-run")]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType(typeof(EvaluationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DryRun([FromBody] DryRunRequest request, CancellationToken ct)
    {
        var result = await evaluationService.EvaluateAsync(
            request.RulesJson,
            request.FactsJson,
            request.SettingsJson,
            request.CustomTypes,
            ct);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result);
    }

    /// <summary>
    /// Returns all test scenarios, optionally filtered by workflow ID.
    /// </summary>
    [HttpGet("scenarios")]
    [ProducesResponseType(typeof(List<ScenarioResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetScenarios([FromQuery] int? workflowId, CancellationToken ct)
    {
        var scenarios = await repository.GetScenariosAsync(workflowId, ct);

        var response = scenarios.Select(s => new ScenarioResponse(
            s.ScenarioId,
            s.WorkflowDefinitionId,
            s.ScenarioName,
            s.MockInputJson,
            s.ExpectedOutputJson,
            s.CreatedAt)).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Returns a single test scenario by ID.
    /// </summary>
    [HttpGet("scenarios/{id:int}")]
    [ProducesResponseType(typeof(ScenarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScenarioById(int id, CancellationToken ct)
    {
        var scenario = await repository.GetScenarioByIdAsync(id, ct);

        if (scenario is null)
        {
            return NotFound();
        }

        var response = new ScenarioResponse(
            scenario.ScenarioId,
            scenario.WorkflowDefinitionId,
            scenario.ScenarioName,
            scenario.MockInputJson,
            scenario.ExpectedOutputJson,
            scenario.CreatedAt);

        return Ok(response);
    }

    /// <summary>
    /// Saves a new test scenario with mock input and expected output.
    /// </summary>
    [HttpPost("scenarios")]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType(typeof(WorkflowTestScenarios), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveScenario([FromBody] SaveScenarioRequest request, CancellationToken ct)
    {
        // Validate JSON fields
        if (!IsValidJson(request.MockInputJson))
        {
            return BadRequest(new { error = "MockInputJson is not valid JSON." });
        }

        if (!string.IsNullOrWhiteSpace(request.ExpectedOutputJson) && !IsValidJson(request.ExpectedOutputJson))
        {
            return BadRequest(new { error = "ExpectedOutputJson is not valid JSON." });
        }

        var scenario = new WorkflowTestScenarios
        {
            WorkflowDefinitionId = request.WorkflowDefinitionId,
            ScenarioName = request.ScenarioName,
            MockInputJson = request.MockInputJson,
            ExpectedOutputJson = request.ExpectedOutputJson,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await repository.SaveScenarioAsync(scenario, ct);

        return StatusCode(StatusCodes.Status201Created, saved);
    }

    /// <summary>
    /// Updates an existing test scenario. Only provided fields are updated.
    /// WorkflowDefinitionId cannot be changed.
    /// </summary>
    [HttpPut("scenarios/{id:int}")]
    [RequestSizeLimit(1_048_576)]
    [ProducesResponseType(typeof(ScenarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateScenario(int id, [FromBody] UpdateScenarioRequest request, CancellationToken ct)
    {
        // Validate JSON fields if provided
        if (request.MockInputJson is not null && !IsValidJson(request.MockInputJson))
        {
            return BadRequest(new { error = "MockInputJson is not valid JSON." });
        }

        if (!string.IsNullOrWhiteSpace(request.ExpectedOutputJson) && !IsValidJson(request.ExpectedOutputJson))
        {
            return BadRequest(new { error = "ExpectedOutputJson is not valid JSON." });
        }

        var updateEntity = new WorkflowTestScenarios
        {
            // Null-forgiving: the repository checks for null before assigning
            ScenarioName = request.ScenarioName!,
            MockInputJson = request.MockInputJson!,
            ExpectedOutputJson = request.ExpectedOutputJson
        };

        var updated = await repository.UpdateScenarioAsync(id, updateEntity, ct);

        if (updated is null)
        {
            return NotFound();
        }

        var response = new ScenarioResponse(
            updated.ScenarioId,
            updated.WorkflowDefinitionId,
            updated.ScenarioName,
            updated.MockInputJson,
            updated.ExpectedOutputJson,
            updated.CreatedAt);

        return Ok(response);
    }

    /// <summary>
    /// Deletes a test scenario by ID.
    /// </summary>
    [HttpDelete("scenarios/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScenario(int id, CancellationToken ct)
    {
        var deleted = await repository.DeleteScenarioAsync(id, ct);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    private static bool IsValidJson(string json)
    {
        try
        {
            var obj = Newtonsoft.Json.Linq.JToken.Parse(json);
            return obj is not null;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // EF Core wraps the provider-specific exception. Check the inner exception message
        // for the unique constraint name UQ_Workflow_Version.
        return ex.InnerException is not null
               && ex.InnerException.Message.Contains("UQ_Workflow_Version", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Request body for the dry-run evaluation endpoint.
/// </summary>
public sealed record DryRunRequest(
    [Required] string RulesJson,
    [Required] string FactsJson,
    string? SettingsJson = null,
    string[]? CustomTypes = null);

/// <summary>
/// Request body for saving a test scenario.
/// </summary>
public sealed record SaveScenarioRequest(
    [Required] int WorkflowDefinitionId,
    [Required] string ScenarioName,
    [Required] string MockInputJson,
    string? ExpectedOutputJson = null);

/// <summary>
/// Summary response for listing workflows.
/// </summary>
public sealed record WorkflowSummaryResponse(
    int WorkflowDefinitionId,
    string WorkflowName,
    int Version,
    string Status,
    DateTime? CreatedAt,
    string? CreatedBy);

/// <summary>
/// Full workflow definition response including JSON content.
/// </summary>
public sealed record WorkflowDefinitionResponse(
    int WorkflowDefinitionId,
    string WorkflowName,
    int Version,
    string JsonContent,
    string Status,
    DateTime? CreatedAt,
    string? CreatedBy);

/// <summary>
/// Request body for creating a new workflow definition.
/// </summary>
public sealed record CreateWorkflowRequest(
    [Required] string WorkflowName,
    [Required] int Version,
    [Required] string JsonContent,
    [Required] string Status);

/// <summary>
/// Request body for updating an existing workflow definition. All fields are optional.
/// </summary>
public sealed record UpdateWorkflowRequest(
    string? WorkflowName = null,
    int? Version = null,
    string? JsonContent = null,
    string? Status = null);

/// <summary>
/// Test scenario response.
/// </summary>
public sealed record ScenarioResponse(
    int ScenarioId,
    int WorkflowDefinitionId,
    string ScenarioName,
    string MockInputJson,
    string? ExpectedOutputJson,
    DateTime? CreatedAt);

/// <summary>
/// Request body for updating an existing test scenario. All fields are optional.
/// WorkflowDefinitionId cannot be changed.
/// </summary>
public sealed record UpdateScenarioRequest(
    string? ScenarioName = null,
    string? MockInputJson = null,
    string? ExpectedOutputJson = null);
