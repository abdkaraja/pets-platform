using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class VetAssignmentConfiguration : IEntityTypeConfiguration<VetAssignment>
{
    public void Configure(EntityTypeBuilder<VetAssignment> builder)
    {
        builder.HasKey(va => va.Id);

        builder.Property(va => va.RequestedByUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(va => va.Status)
            .HasConversion<int>();

        builder.Property(va => va.RejectionReason)
            .HasMaxLength(500);

        builder.HasOne(va => va.Pet)
            .WithMany()
            .HasForeignKey(va => va.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(va => va.VetProfile)
            .WithMany(vp => vp.VetAssignments)
            .HasForeignKey(va => va.VetProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(va => va.PetId);
        builder.HasIndex(va => va.VetProfileId);
        builder.HasIndex(va => va.Status);
        builder.HasIndex(va => new { va.PetId, va.VetProfileId, va.Status });
    }
}
