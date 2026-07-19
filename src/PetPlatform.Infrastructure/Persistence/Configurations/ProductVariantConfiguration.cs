using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Size)
            .HasMaxLength(100);

        builder.Property(v => v.Color)
            .HasMaxLength(100);

        builder.Property(v => v.Weight)
            .HasColumnType("decimal(18,2)");

        builder.Property(v => v.PriceMultiplier)
            .HasColumnType("decimal(18,4)");

        builder.Property(v => v.Sku)
            .HasMaxLength(50);

        builder.HasIndex(v => v.ProductId);
        builder.HasIndex(v => v.Sku);

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId);
    }
}
