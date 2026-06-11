using RulesEngineEditor.Server.Business.Entities.Models;

namespace RulesEngineEditor.Server.Infrastructure.Repositories;

public interface IRulesRepository
{
    Task<List<WorkflowDefinitions>> GetAllWorkflowsAsync(CancellationToken ct = default);
    Task<WorkflowDefinitions?> GetWorkflowByIdAsync(int id, CancellationToken ct = default);
    Task<WorkflowDefinitions> CreateWorkflowAsync(WorkflowDefinitions workflow, CancellationToken ct = default);
    Task<WorkflowDefinitions?> UpdateWorkflowAsync(int id, WorkflowDefinitions workflow, CancellationToken ct = default);
    Task<bool> DeleteWorkflowAsync(int id, CancellationToken ct = default);
    Task<WorkflowTestScenarios> SaveScenarioAsync(WorkflowTestScenarios scenario, CancellationToken ct = default);
    Task<List<WorkflowTestScenarios>> GetScenariosAsync(int? workflowId = null, CancellationToken ct = default);
    Task<WorkflowTestScenarios?> GetScenarioByIdAsync(int id, CancellationToken ct = default);
    Task<WorkflowTestScenarios?> UpdateScenarioAsync(int id, WorkflowTestScenarios scenario, CancellationToken ct = default);
    Task<bool> DeleteScenarioAsync(int id, CancellationToken ct = default);
}
