namespace RulesEngineEditor.Server.Business.Entities;

public sealed class Product
{
    // EF Core parameterless constructor
    private Product() { }

    public Product(string name, string? description, decimal basePrice, string sku)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters.", nameof(name));
        if (basePrice <= 0)
            throw new ArgumentException("BasePrice must be positive.", nameof(basePrice));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("Sku cannot be empty.", nameof(sku));
        if (sku.Length > 50)
            throw new ArgumentException("Sku cannot exceed 50 characters.", nameof(sku));

        Id = ProductId.New();
        Name = name;
        Description = description;
        BasePrice = basePrice;
        CurrentPrice = basePrice;
        Sku = sku;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public ProductId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public decimal CurrentPrice { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Price must be positive.", nameof(newPrice));
        CurrentPrice = newPrice;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
