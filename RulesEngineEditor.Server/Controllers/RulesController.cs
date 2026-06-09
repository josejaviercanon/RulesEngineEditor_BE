using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    /// Saves a test scenario with mock input and expected output.
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
