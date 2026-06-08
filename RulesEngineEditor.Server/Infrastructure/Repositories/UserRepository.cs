using Microsoft.EntityFrameworkCore;
using RulesEngineEditor.Server.Business.Entities.Models;

namespace RulesEngineEditor.Server.Infrastructure.Repositories;

public sealed class UserRepository(RulesEngineEditorContext rulesEngineEditorContext) : IUserRepository
{
    private readonly RulesEngineEditorContext _rulesEngineEditorContext = rulesEngineEditorContext;

    public async Task<IReadOnlyList<AspNetUsers>> GetCurrentUsersAsync(CancellationToken cancellationToken)
    {
        return await _rulesEngineEditorContext.AspNetUsers
            .AsNoTracking()
            .OrderBy(user => user.UserName)
            .ToListAsync(cancellationToken);
    }
}
