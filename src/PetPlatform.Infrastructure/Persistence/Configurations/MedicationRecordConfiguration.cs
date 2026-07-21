using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class MedicationRecordConfiguration : IEntityTypeConfiguration<MedicationRecord>
{
    public void Configure(EntityTypeBuilder<MedicationRecord> builder)
    {
        builder.HasKey(mr => mr.Id);

        builder.Property(mr => mr.VetUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(mr => mr.MedicationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(mr => mr.Dosage)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(mr => mr.Frequency)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(mr => mr.PrescribingReason)
            .HasMaxLength(500);

        builder.Property(mr => mr.Instructions)
            .HasMaxLength(1000);

        builder.Property(mr => mr.SideEffectsNoted)
            .HasMaxLength(1000);

        builder.HasOne(mr => mr.Pet)
            .WithMany()
            .HasForeignKey(mr => mr.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mr => mr.PetId);
        builder.HasIndex(mr => mr.VetUserId);
        builder.HasIndex(mr => new { mr.PetId, mr.StartDate });
    }
}
