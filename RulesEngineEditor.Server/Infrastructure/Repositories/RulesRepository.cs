using Microsoft.EntityFrameworkCore;
using RulesEngineEditor.Server.Business.Entities.Models;
using RulesEngineEditor.Server.Infrastructure.Data;

namespace RulesEngineEditor.Server.Infrastructure.Repositories;

public sealed class RulesRepository(ApplicationDbContext context) : IRulesRepository
{
    public async Task<List<WorkflowDefinitions>> GetAllWorkflowsAsync(CancellationToken ct = default)
    {
        return await context.WorkflowDefinitions
            .AsNoTracking()
            .OrderBy(w => w.WorkflowName)
            .ThenByDescending(w => w.Version)
            .ToListAsync(ct);
    }

    public async Task<WorkflowDefinitions?> GetWorkflowByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.WorkflowDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WorkflowDefinitionId == id, ct);
    }

    public async Task<WorkflowDefinitions> CreateWorkflowAsync(WorkflowDefinitions workflow, CancellationToken ct = default)
    {
        workflow.CreatedAt ??= DateTime.UtcNow;
        context.WorkflowDefinitions.Add(workflow);
        await context.SaveChangesAsync(ct);
        return workflow;
    }

    public async Task<WorkflowDefinitions?> UpdateWorkflowAsync(int id, WorkflowDefinitions workflow, CancellationToken ct = default)
    {
        var existing = await context.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.WorkflowDefinitionId == id, ct);

        if (existing is null)
        {
            return null;
        }

        if (workflow.WorkflowName is not null)
        {
            existing.WorkflowName = workflow.WorkflowName;
        }

        if (workflow.Version != default)
        {
            existing.Version = workflow.Version;
        }

        if (workflow.JsonContent is not null)
        {
            existing.JsonContent = workflow.JsonContent;
        }

        if (workflow.Status is not null)
        {
            existing.Status = workflow.Status;
        }

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteWorkflowAsync(int id, CancellationToken ct = default)
    {
        var entity = await context.WorkflowDefinitions
            .FirstOrDefaultAsync(w => w.WorkflowDefinitionId == id, ct);

        if (entity is null)
        {
            return false;
        }

        context.WorkflowDefinitions.Remove(entity);
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<WorkflowTestScenarios> SaveScenarioAsync(WorkflowTestScenarios scenario, CancellationToken ct = default)
    {
        context.WorkflowTestScenarios.Add(scenario);
        await context.SaveChangesAsync(ct);
        return scenario;
    }

    public async Task<List<WorkflowTestScenarios>> GetScenariosAsync(int? workflowId = null, CancellationToken ct = default)
    {
        var query = context.WorkflowTestScenarios.AsNoTracking();

        if (workflowId.HasValue)
        {
            query = query.Where(s => s.WorkflowDefinitionId == workflowId.Value);
        }

        return await query
            .OrderBy(s => s.ScenarioName)
            .ToListAsync(ct);
    }

    public async Task<WorkflowTestScenarios?> GetScenarioByIdAsync(int id, CancellationToken ct = default)
    {
        return await context.WorkflowTestScenarios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ScenarioId == id, ct);
    }

    public async Task<WorkflowTestScenarios?> UpdateScenarioAsync(int id, WorkflowTestScenarios scenario, CancellationToken ct = default)
    {
        var existing = await context.WorkflowTestScenarios
            .FirstOrDefaultAsync(s => s.ScenarioId == id, ct);

        if (existing is null)
        {
            return null;
        }

        if (scenario.ScenarioName is not null)
        {
            existing.ScenarioName = scenario.ScenarioName;
        }

        if (scenario.MockInputJson is not null)
        {
            existing.MockInputJson = scenario.MockInputJson;
        }

        if (scenario.ExpectedOutputJson is not null)
        {
            existing.ExpectedOutputJson = scenario.ExpectedOutputJson;
        }

        // Preserve the original WorkflowDefinitionId — scenarios belong to one workflow
        // WorkflowDefinitionId is intentionally NOT copied from the input entity

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteScenarioAsync(int id, CancellationToken ct = default)
    {
        var entity = await context.WorkflowTestScenarios
            .FirstOrDefaultAsync(s => s.ScenarioId == id, ct);

        if (entity is null)
        {
            return false;
        }

        context.WorkflowTestScenarios.Remove(entity);
        await context.SaveChangesAsync(ct);
        return true;
    }
}
