using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class MatchNotificationConfiguration : IEntityTypeConfiguration<MatchNotification>
{
    public void Configure(EntityTypeBuilder<MatchNotification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.MatchedReportId)
            .IsRequired();

        builder.Property(n => n.TriggeredReportId)
            .IsRequired();

        builder.Property(n => n.ReporterUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.IsRead)
            .IsRequired();

        builder.HasOne(n => n.MatchedReport)
            .WithMany(r => r.MatchNotificationsAsMatched)
            .HasForeignKey(n => n.MatchedReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.TriggeredReport)
            .WithMany(r => r.MatchNotificationsAsTriggered)
            .HasForeignKey(n => n.TriggeredReportId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.MatchedReportId);
        builder.HasIndex(n => n.TriggeredReportId);
        builder.HasIndex(n => n.ReporterUserId);
        builder.HasIndex(n => new { n.ReporterUserId, n.IsRead });
    }
}
