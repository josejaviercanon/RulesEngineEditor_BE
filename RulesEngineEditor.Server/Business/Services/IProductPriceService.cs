namespace RulesEngineEditor.Server.Business.Services;

public interface IProductPriceService
{
    decimal CalculateEffectivePrice(decimal basePrice, string customerTier);
}
