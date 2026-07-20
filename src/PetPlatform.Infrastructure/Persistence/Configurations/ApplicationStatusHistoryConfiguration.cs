using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.HasKey(ash => ash.Id);

        builder.Property(ash => ash.Status)
            .HasConversion<int>();

        builder.HasOne(ash => ash.Application)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(ash => ash.ApplicationId);
    }
}
