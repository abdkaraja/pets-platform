using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(o => o.TotalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Status)
            .HasConversion<int>();

        builder.Property(o => o.StripePaymentIntentId)
            .HasMaxLength(255);

        builder.Property(o => o.ShippingAddress)
            .HasMaxLength(500);

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
    }
}
