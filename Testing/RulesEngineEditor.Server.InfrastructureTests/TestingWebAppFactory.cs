using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RulesEngineEditor.Server.Business.Entities.Models;
using RulesEngineEditor.Server.Infrastructure.Data;

namespace RulesEngineEditor.Server.InfrastructureTests;

[ExcludeFromCodeCoverage]
public class TestingWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use ConfigureServices (not ConfigureTestServices) so we can remove
        // and replace service registrations BEFORE EF Core's internal service
        // provider is initialized.
        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core-related service registrations to avoid
            // conflicts between SqlServer (from Program.cs) and InMemory (test).
            var servicesToRemove = services
                .Where(d =>
                {
                    var fullName = d.ServiceType.FullName;
                    if (fullName is null) return false;

                    // Match EF Core internal types
                    if (fullName.StartsWith("Microsoft.EntityFrameworkCore") &&
                        !fullName.Contains("Design"))
                    {
                        return true;
                    }

                    // Match DbContext types and their options
                    if (d.ServiceType == typeof(ApplicationDbContext) ||
                        d.ServiceType == typeof(RulesEngineEditorContext) ||
                        d.ServiceType == typeof(DbContextOptions) ||
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                        d.ServiceType == typeof(DbContextOptions<RulesEngineEditorContext>))
                    {
                        return true;
                    }

                    return false;
                })
                .ToList();

            foreach (var service in servicesToRemove)
            {
                services.Remove(service);
            }

            // Re-register both contexts with InMemory database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("RulesEngineEditorTestDb"));

            services.AddDbContext<RulesEngineEditorContext>(options =>
                options.UseInMemoryDatabase("RulesEngineEditorTestDb"));
        });
    }
}
