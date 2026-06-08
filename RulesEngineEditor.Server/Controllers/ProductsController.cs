using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RulesEngineEditor.Server.Business.Services;
using RulesEngineEditor.Server.Business.Services.Models;

namespace RulesEngineEditor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var productId = await productService.CreateProductAsync(request, ct);
        return Created(string.Empty, new { id = productId.Value });
    }
}
