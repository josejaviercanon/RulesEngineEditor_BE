using Microsoft.AspNetCore.Identity;
using RulesEngineEditor.Server.Infrastructure.Data;

namespace RulesEngineEditor.Server.Infrastructure.Identity;

public static class IdentityConfiguration
{
    public static IServiceCollection AddWebApiIdentity(this IServiceCollection services)
    {
        services.AddIdentityApiEndpoints<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped<IUserClaimsPrincipalFactory<IdentityUser>, TokenService>();

        return services;
    }
}
