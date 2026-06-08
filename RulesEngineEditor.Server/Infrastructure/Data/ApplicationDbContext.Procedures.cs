using Microsoft.Data.SqlClient;

namespace RulesEngineEditor.Server.Infrastructure.Data;

// Hand-maintained partial class for stored procedure support.
// Add new procedures here as the schema grows.
// Do NOT auto-generate this file — it is intentionally separate from
// the EF Power Tools scaffold in Business/Entities/Models/.
public partial class ApplicationDbContext
{
    private IApplicationDbContextProcedures? _procedures;

    public IApplicationDbContextProcedures Procedures
    {
        get
        {
            _procedures ??= new ApplicationDbContextProcedures(this);
            return _procedures;
        }
        set => _procedures = value;
    }

    public IApplicationDbContextProcedures GetProcedures() => Procedures;
}

public interface IApplicationDbContextProcedures
{
    // Add stored procedure method signatures here as needed.
    // Example:
    // Task<List<MyResult>> MyStoredProcAsync(int param, CancellationToken cancellationToken = default);
}

internal sealed class ApplicationDbContextProcedures(ApplicationDbContext context)
    : IApplicationDbContextProcedures
{
    // Implement stored procedure calls here as needed.
    // Example:
    // public async Task<List<MyResult>> MyStoredProcAsync(int param, CancellationToken cancellationToken = default)
    // {
    //     var p = new SqlParameter("@Param", param);
    //     return await context.SqlQueryAsync<MyResult>("EXEC dbo.MyStoredProc @Param", [p], cancellationToken);
    // }
}
