using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RulesEngineEditor.Server.Business.Entities;

namespace RulesEngineEditor.Server.Infrastructure.Data.Interceptors;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var now = DateTime.UtcNow;
            foreach (var entry in eventData.Context.ChangeTracker.Entries<Product>()
                         .Where(e => e.State == EntityState.Modified))
            {
                entry.Property(p => p.UpdatedAtUtc).CurrentValue = now;
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
