namespace RulesEngineEditor.Server.Business.Services;

public sealed record AdminUserDto(
    string Id,
    string? UserName,
    string? Email,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool LockoutEnabled);
