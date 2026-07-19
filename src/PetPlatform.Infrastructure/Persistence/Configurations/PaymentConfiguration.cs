using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.StripePaymentIntentId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Status)
            .HasConversion<int>();

        builder.HasOne(p => p.Order)
            .WithMany()
            .HasForeignKey(p => p.OrderId);
    }
}
