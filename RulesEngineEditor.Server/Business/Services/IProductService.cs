using RulesEngineEditor.Server.Business.Entities;
using RulesEngineEditor.Server.Business.Services.Models;

namespace RulesEngineEditor.Server.Business.Services;

public interface IProductService
{
    Task<ProductId> CreateProductAsync(CreateProductRequest request, CancellationToken ct = default);
}
