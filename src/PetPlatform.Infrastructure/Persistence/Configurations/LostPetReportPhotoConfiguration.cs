using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class LostPetReportPhotoConfiguration : IEntityTypeConfiguration<LostPetReportPhoto>
{
    public void Configure(EntityTypeBuilder<LostPetReportPhoto> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.LostPetReportId)
            .IsRequired();

        builder.Property(p => p.PhotoPath)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(p => p.LostPetReport)
            .WithMany(r => r.Photos)
            .HasForeignKey(p => p.LostPetReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.LostPetReportId);
    }
}
