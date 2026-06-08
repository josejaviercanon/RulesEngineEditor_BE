namespace RulesEngineEditor.Server.Business.Entities;

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());
}
