namespace RulesEngineEditor.Server.Business.Services;

public sealed class ProductPriceService : IProductPriceService
{
    public decimal CalculateEffectivePrice(decimal basePrice, string customerTier) => customerTier switch
    {
        "Premium" => basePrice * 0.90m,
        _ => basePrice
    };
}
