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

    public async Task<WorkflowTestScenarios> SaveScenarioAsync(WorkflowTestScenarios scenario, CancellationToken ct = default)
    {
        context.WorkflowTestScenarios.Add(scenario);
        await context.SaveChangesAsync(ct);
        return scenario;
    }
}
