using RulesEngine.Models;

namespace RulesEngineEditor.Server.Business.Services;

public sealed record EvaluationResult(
    bool IsSuccess,
    List<RuleResultTree>? RuleResultTree,
    string? ErrorMessage);
