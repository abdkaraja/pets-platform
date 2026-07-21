using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class VaccinationRecordConfiguration : IEntityTypeConfiguration<VaccinationRecord>
{
    public void Configure(EntityTypeBuilder<VaccinationRecord> builder)
    {
        builder.HasKey(vr => vr.Id);

        builder.Property(vr => vr.VetUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(vr => vr.VaccineName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(vr => vr.BatchLotNumber)
            .HasMaxLength(100);

        builder.Property(vr => vr.Notes)
            .HasMaxLength(2000);

        builder.HasOne(vr => vr.Pet)
            .WithMany()
            .HasForeignKey(vr => vr.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(vr => vr.PetId);
        builder.HasIndex(vr => vr.VetUserId);
        builder.HasIndex(vr => vr.DateAdministered);
        builder.HasIndex(vr => new { vr.PetId, vr.DateAdministered });
    }
}
