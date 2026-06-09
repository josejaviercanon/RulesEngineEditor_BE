using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RulesEngineEditor.Server.Business.Entities;
using RulesEngineEditor.Server.Business.Entities.Models;

namespace RulesEngineEditor.Server.Infrastructure.Data;

public sealed partial class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<WorkflowDefinitions> WorkflowDefinitions => Set<WorkflowDefinitions>();
    public DbSet<WorkflowTestScenarios> WorkflowTestScenarios => Set<WorkflowTestScenarios>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
