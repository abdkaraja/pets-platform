using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.HasKey(osh => osh.Id);

        builder.Property(osh => osh.Status)
            .HasConversion<int>();

        builder.HasOne(osh => osh.Order)
            .WithMany(o => o.StatusHistory)
            .HasForeignKey(osh => osh.OrderId);
    }
}
