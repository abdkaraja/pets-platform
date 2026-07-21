using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class VetAvailabilityConfiguration : IEntityTypeConfiguration<VetAvailability>
{
    public void Configure(EntityTypeBuilder<VetAvailability> builder)
    {
        builder.HasKey(va => va.Id);

        builder.Property(va => va.DayOfWeek)
            .HasConversion<int>();

        builder.HasOne(va => va.VetProfile)
            .WithMany(vp => vp.AvailabilitySchedule)
            .HasForeignKey(va => va.VetProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(va => new { va.VetProfileId, va.DayOfWeek })
            .IsUnique();
    }
}
