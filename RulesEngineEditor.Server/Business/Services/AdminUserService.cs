using RulesEngineEditor.Server.Infrastructure.Repositories;

namespace RulesEngineEditor.Server.Business.Services;

public sealed class AdminUserService(IUserRepository userRepository) : IAdminUserService
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<IReadOnlyList<AdminUserDto>> GetCurrentUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetCurrentUsersAsync(cancellationToken);

        return users
            .Select(user => new AdminUserDto(
                user.Id,
                user.UserName,
                user.Email,
                user.EmailConfirmed,
                user.TwoFactorEnabled,
                user.LockoutEnabled))
            .ToList();
    }
}
