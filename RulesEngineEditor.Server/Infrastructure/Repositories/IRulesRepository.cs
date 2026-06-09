using RulesEngineEditor.Server.Business.Entities.Models;

namespace RulesEngineEditor.Server.Infrastructure.Repositories;

public interface IRulesRepository
{
    Task<List<WorkflowDefinitions>> GetAllWorkflowsAsync(CancellationToken ct = default);
    Task<WorkflowDefinitions?> GetWorkflowByIdAsync(int id, CancellationToken ct = default);
    Task<WorkflowTestScenarios> SaveScenarioAsync(WorkflowTestScenarios scenario, CancellationToken ct = default);
}
