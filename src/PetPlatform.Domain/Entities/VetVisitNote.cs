using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class VetVisitNote
{
    public int Id { get; private set; }
    public int PetId { get; private set; }
    public string VetUserId { get; private set; } = string.Empty;
    public DateTime VisitDate { get; private set; }
    public string Subjective { get; private set; } = string.Empty;
    public string Objective { get; private set; } = string.Empty;
    public string Assessment { get; private set; } = string.Empty;
    public string Plan { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Pet Pet { get; private set; } = null!;

    private VetVisitNote() { } // EF Core

    public static VetVisitNote Create(
        int petId,
        string vetUserId,
        DateTime visitDate,
        string subjective,
        string objective,
        string assessment,
        string plan,
        string? notes)
    {
        if (petId <= 0)
            throw new ArgumentException("PetId must be greater than 0.", nameof(petId));
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));
        Guard.Against.NullOrWhiteSpace(subjective, nameof(subjective));
        Guard.Against.NullOrWhiteSpace(objective, nameof(objective));
        Guard.Against.NullOrWhiteSpace(assessment, nameof(assessment));
        Guard.Against.NullOrWhiteSpace(plan, nameof(plan));

        return new VetVisitNote
        {
            PetId = petId,
            VetUserId = vetUserId,
            VisitDate = visitDate,
            Subjective = subjective,
            Objective = objective,
            Assessment = assessment,
            Plan = plan,
            Notes = notes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(
        DateTime visitDate,
        string subjective,
        string objective,
        string assessment,
        string plan,
        string? notes)
    {
        VisitDate = visitDate;
        Subjective = subjective;
        Objective = objective;
        Assessment = assessment;
        Plan = plan;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
