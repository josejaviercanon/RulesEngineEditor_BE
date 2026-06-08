using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace RulesEngineEditor.Server.Infrastructure.Identity;

public sealed class TokenService(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor)
    : UserClaimsPrincipalFactory<IdentityUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(IdentityUser appUser)
    {
        ArgumentNullException.ThrowIfNull(appUser);

        var identity = await base.GenerateClaimsAsync(appUser);
        var roles = await UserManager.GetRolesAsync(appUser);

        foreach (var role in roles)
        {
            if (!identity.HasClaim(ClaimTypes.Role, role))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }

        return identity;
    }
}
