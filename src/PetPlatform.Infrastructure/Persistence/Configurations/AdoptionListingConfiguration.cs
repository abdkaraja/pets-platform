using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class AdoptionListingConfiguration : IEntityTypeConfiguration<AdoptionListing>
{
    public void Configure(EntityTypeBuilder<AdoptionListing> builder)
    {
        builder.HasKey(al => al.Id);

        builder.Property(al => al.PetId)
            .IsRequired();

        builder.Property(al => al.ShelterUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(al => al.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(al => al.Description)
            .HasMaxLength(2000);

        builder.Property(al => al.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(al => al.Status)
            .HasConversion<int>();

        builder.HasIndex(al => al.PetId);
        builder.HasIndex(al => al.ShelterUserId);
        builder.HasIndex(al => al.Location);
        builder.HasIndex(al => new { al.PetId, al.Status });
    }
}
