using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class VetProfileConfiguration : IEntityTypeConfiguration<VetProfile>
{
    public void Configure(EntityTypeBuilder<VetProfile> builder)
    {
        builder.HasKey(vp => vp.Id);

        builder.Property(vp => vp.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(vp => vp.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(vp => vp.Clinic)
            .HasMaxLength(200);

        builder.Property(vp => vp.Specialty)
            .HasMaxLength(100);

        builder.Property(vp => vp.Bio)
            .HasMaxLength(2000);

        builder.Property(vp => vp.ServicesOffered)
            .HasMaxLength(500);

        builder.HasIndex(vp => vp.UserId)
            .IsUnique();

        builder.HasIndex(vp => vp.Specialty);

        builder.HasIndex(vp => vp.IsApproved);
    }
}
