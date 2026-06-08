using System.ComponentModel.DataAnnotations;

namespace RulesEngineEditor.Server.Business.Services.Models;

public record CreateProductRequest
{
    [Required]
    [StringLength(200)]
    public required string Name { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal BasePrice { get; init; }

    [Required]
    [StringLength(50)]
    public required string Sku { get; init; }

    public string CustomerTier { get; init; } = "Standard";
}
