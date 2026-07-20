using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class LostPetReportPhoto
{
    public int Id { get; private set; }
    public int LostPetReportId { get; private set; }
    public string PhotoPath { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public LostPetReport LostPetReport { get; private set; } = null!;

    private LostPetReportPhoto() { } // EF Core

    public static LostPetReportPhoto Create(int lostPetReportId, string photoPath)
    {
        Guard.Against.NegativeOrZero(lostPetReportId, nameof(lostPetReportId));
        Guard.Against.NullOrWhiteSpace(photoPath, nameof(photoPath));

        return new LostPetReportPhoto
        {
            LostPetReportId = lostPetReportId,
            PhotoPath = photoPath,
            CreatedAt = DateTime.UtcNow
        };
    }
}
