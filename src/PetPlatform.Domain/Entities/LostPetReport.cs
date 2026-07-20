using Ardalis.GuardClauses;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class LostPetReport
{
    public int Id { get; private set; }
    public string ReporterUserId { get; private set; } = string.Empty;
    public LostPetReportType ReportType { get; private set; }
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public string Color { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public DateTime DateReported { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public int? PetId { get; private set; }
    public LostPetReportStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Pet? Pet { get; private set; }
    public ICollection<LostPetReportPhoto> Photos { get; private set; } = new List<LostPetReportPhoto>();
    public ICollection<MatchNotification> MatchNotificationsAsMatched { get; private set; } = new List<MatchNotification>();
    public ICollection<MatchNotification> MatchNotificationsAsTriggered { get; private set; } = new List<MatchNotification>();

    private LostPetReport() { } // EF Core

    public static LostPetReport Create(
        string reporterUserId,
        LostPetReportType reportType,
        PetSpecies species,
        string color,
        string location,
        DateTime dateReported,
        string description,
        string? breed = null,
        int? petId = null)
    {
        Guard.Against.NullOrWhiteSpace(reporterUserId, nameof(reporterUserId));
        Guard.Against.NullOrWhiteSpace(color, nameof(color));
        Guard.Against.NullOrWhiteSpace(location, nameof(location));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));

        return new LostPetReport
        {
            ReporterUserId = reporterUserId,
            ReportType = reportType,
            Species = species,
            Breed = breed,
            Color = color,
            Location = location,
            DateReported = dateReported,
            Description = description,
            PetId = petId,
            Status = LostPetReportStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(
        string color,
        string location,
        DateTime dateReported,
        string description,
        string? breed = null)
    {
        Guard.Against.NullOrWhiteSpace(color, nameof(color));
        Guard.Against.NullOrWhiteSpace(location, nameof(location));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));

        Color = color;
        Location = location;
        DateReported = dateReported;
        Description = description;
        Breed = breed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        if (Status != LostPetReportStatus.Open)
            throw new InvalidOperationException("Only Open reports can be resolved.");

        Status = LostPetReportStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;
    }
}
