using RulesEngineEditor.Server.Business.Entities;
using RulesEngineEditor.Server.Business.Services.Models;
using RulesEngineEditor.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RulesEngineEditor.Server.Business.Services;

public sealed class ProductService(
    ApplicationDbContext dbContext,
    IProductPriceService priceService) : IProductService
{
    public async Task<ProductId> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken ct = default)
    {
        var product = new Product(
            request.Name,
            request.Description,
            request.BasePrice,
            request.Sku);

        var effectivePrice = priceService.CalculateEffectivePrice(
            request.BasePrice, request.CustomerTier);
        product.UpdatePrice(effectivePrice);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(ct);

        return product.Id;
    }
}
