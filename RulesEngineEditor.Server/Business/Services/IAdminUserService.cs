namespace RulesEngineEditor.Server.Business.Services;

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserDto>> GetCurrentUsersAsync(CancellationToken cancellationToken);
}
