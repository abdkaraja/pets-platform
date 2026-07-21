using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class VetVisitNoteConfiguration : IEntityTypeConfiguration<VetVisitNote>
{
    public void Configure(EntityTypeBuilder<VetVisitNote> builder)
    {
        builder.HasKey(vn => vn.Id);

        builder.Property(vn => vn.VetUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(vn => vn.Subjective)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(vn => vn.Objective)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(vn => vn.Assessment)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(vn => vn.Plan)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(vn => vn.Notes)
            .HasMaxLength(2000);

        builder.HasOne(vn => vn.Pet)
            .WithMany()
            .HasForeignKey(vn => vn.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(vn => vn.PetId);
        builder.HasIndex(vn => vn.VetUserId);
        builder.HasIndex(vn => vn.VisitDate);
        builder.HasIndex(vn => new { vn.PetId, vn.VisitDate });
    }
}
