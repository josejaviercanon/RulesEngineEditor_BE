using RulesEngineEditor.Server.Business.Entities.Models;

namespace RulesEngineEditor.Server.Infrastructure.Repositories;

public interface IUserRepository
{
    Task<IReadOnlyList<AspNetUsers>> GetCurrentUsersAsync(CancellationToken cancellationToken);
}
