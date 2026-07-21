using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class MedicationRecord
{
    public int Id { get; private set; }
    public int PetId { get; private set; }
    public string VetUserId { get; private set; } = string.Empty;
    public string MedicationName { get; private set; } = string.Empty;
    public string Dosage { get; private set; } = string.Empty;
    public string Frequency { get; private set; } = string.Empty;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? PrescribingReason { get; private set; }
    public string? Instructions { get; private set; }
    public string? SideEffectsNoted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Pet Pet { get; private set; } = null!;

    private MedicationRecord() { } // EF Core

    public static MedicationRecord Create(
        int petId,
        string vetUserId,
        string medicationName,
        string dosage,
        string frequency,
        DateTime startDate,
        DateTime? endDate,
        string? prescribingReason,
        string? instructions,
        string? sideEffectsNoted)
    {
        if (petId <= 0)
            throw new ArgumentException("PetId must be greater than 0.", nameof(petId));
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));
        Guard.Against.NullOrWhiteSpace(medicationName, nameof(medicationName));
        Guard.Against.NullOrWhiteSpace(dosage, nameof(dosage));
        Guard.Against.NullOrWhiteSpace(frequency, nameof(frequency));

        return new MedicationRecord
        {
            PetId = petId,
            VetUserId = vetUserId,
            MedicationName = medicationName,
            Dosage = dosage,
            Frequency = frequency,
            StartDate = startDate,
            EndDate = endDate,
            PrescribingReason = prescribingReason,
            Instructions = instructions,
            SideEffectsNoted = sideEffectsNoted,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(
        string medicationName,
        string dosage,
        string frequency,
        DateTime startDate,
        DateTime? endDate,
        string? prescribingReason,
        string? instructions,
        string? sideEffectsNoted)
    {
        Guard.Against.NullOrWhiteSpace(medicationName, nameof(medicationName));
        Guard.Against.NullOrWhiteSpace(dosage, nameof(dosage));
        Guard.Against.NullOrWhiteSpace(frequency, nameof(frequency));
        MedicationName = medicationName;
        Dosage = dosage;
        Frequency = frequency;
        StartDate = startDate;
        EndDate = endDate;
        PrescribingReason = prescribingReason;
        Instructions = instructions;
        SideEffectsNoted = sideEffectsNoted;
        UpdatedAt = DateTime.UtcNow;
    }
}
