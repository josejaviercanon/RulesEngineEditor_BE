using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RulesEngineEditor.Server.Business.Services;

namespace RulesEngineEditor.Server.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Administrator")]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    private readonly IAdminUserService _adminUserService = adminUserService;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetCurrentUsersAsync(CancellationToken cancellationToken)
    {
        var users = await _adminUserService.GetCurrentUsersAsync(cancellationToken);
        return Ok(users);
    }
}
