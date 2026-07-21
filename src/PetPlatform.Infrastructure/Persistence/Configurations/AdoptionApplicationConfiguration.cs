using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class AdoptionApplicationConfiguration : IEntityTypeConfiguration<AdoptionApplication>
{
    public void Configure(EntityTypeBuilder<AdoptionApplication> builder)
    {
        builder.HasKey(aa => aa.Id);

        builder.Property(aa => aa.ListingId)
            .IsRequired();

        builder.Property(aa => aa.ApplicantUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(aa => aa.Status)
            .HasConversion<int>();

        builder.Property(aa => aa.ReviewedByUserId)
            .HasMaxLength(450);

        builder.Property(aa => aa.ReviewNotes)
            .HasMaxLength(500);

        builder.HasOne(aa => aa.Listing)
            .WithMany(al => al.Applications)
            .HasForeignKey(aa => aa.ListingId);

        builder.HasIndex(aa => aa.ListingId);
        builder.HasIndex(aa => aa.ApplicantUserId);
        builder.HasIndex(aa => new { aa.ListingId, aa.ApplicantUserId }).IsUnique();
    }
}
