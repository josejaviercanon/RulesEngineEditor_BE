using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RulesEngineEditor.Server.Business.Entities;

namespace RulesEngineEditor.Server.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(
                id => id.Value,
                guid => new ProductId(guid));

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Sku)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.Property(p => p.BasePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.CurrentPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();
    }
}
