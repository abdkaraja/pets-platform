using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class VaccinationRecord
{
    public int Id { get; private set; }
    public int PetId { get; private set; }
    public string VetUserId { get; private set; } = string.Empty;
    public string VaccineName { get; private set; } = string.Empty;
    public DateTime DateAdministered { get; private set; }
    public string? BatchLotNumber { get; private set; }
    public DateTime? NextDueDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Pet Pet { get; private set; } = null!;

    private VaccinationRecord() { } // EF Core

    public static VaccinationRecord Create(
        int petId,
        string vetUserId,
        string vaccineName,
        DateTime dateAdministered,
        string? batchLotNumber,
        DateTime? nextDueDate,
        string? notes)
    {
        if (petId <= 0)
            throw new ArgumentException("PetId must be greater than 0.", nameof(petId));
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));
        Guard.Against.NullOrWhiteSpace(vaccineName, nameof(vaccineName));

        return new VaccinationRecord
        {
            PetId = petId,
            VetUserId = vetUserId,
            VaccineName = vaccineName,
            DateAdministered = dateAdministered,
            BatchLotNumber = batchLotNumber,
            NextDueDate = nextDueDate,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(
        string vaccineName,
        DateTime dateAdministered,
        string? batchLotNumber,
        DateTime? nextDueDate,
        string? notes)
    {
        VaccineName = vaccineName;
        DateAdministered = dateAdministered;
        BatchLotNumber = batchLotNumber;
        NextDueDate = nextDueDate;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
