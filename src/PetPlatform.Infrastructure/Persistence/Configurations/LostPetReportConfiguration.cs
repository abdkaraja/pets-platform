using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class LostPetReportConfiguration : IEntityTypeConfiguration<LostPetReport>
{
    public void Configure(EntityTypeBuilder<LostPetReport> builder)
    {
        builder.HasKey(lpr => lpr.Id);

        builder.Property(lpr => lpr.ReporterUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(lpr => lpr.ReportType)
            .HasConversion<int>();

        builder.Property(lpr => lpr.Species)
            .HasConversion<int>();

        builder.Property(lpr => lpr.Breed)
            .HasMaxLength(100);

        builder.Property(lpr => lpr.Color)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(lpr => lpr.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(lpr => lpr.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(lpr => lpr.Status)
            .HasConversion<int>();

        builder.HasOne(lpr => lpr.Pet)
            .WithMany()
            .HasForeignKey(lpr => lpr.PetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(lpr => lpr.ReporterUserId);
        builder.HasIndex(lpr => lpr.Location);
        builder.HasIndex(lpr => lpr.Species);
        builder.HasIndex(lpr => lpr.Status);
        builder.HasIndex(lpr => new { lpr.Species, lpr.Status });
        builder.HasIndex(lpr => new { lpr.ReportType, lpr.Status, lpr.Species });
    }
}
