namespace RulesEngineEditor.Server.Business.Services;

public sealed record VerificationResult(
    bool IsMatch,
    IReadOnlyList<Difference> Differences);

public sealed record Difference(
    string Path,
    string? Expected,
    string? Actual,
    string Message);
