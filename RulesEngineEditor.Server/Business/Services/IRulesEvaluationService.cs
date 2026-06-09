using RulesEngine.Models;

namespace RulesEngineEditor.Server.Business.Services;

public interface IRulesEvaluationService
{
    Task<EvaluationResult> EvaluateAsync(
        string rulesJson,
        string factsJson,
        string? settingsJson,
        string[]? customTypes,
        CancellationToken ct = default);

    VerificationResult Verify(RuleResultTree resultTree, string? expectedOutputJson);
}
