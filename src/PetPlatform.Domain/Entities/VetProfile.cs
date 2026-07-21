using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class VetProfile
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Clinic { get; private set; }
    public string? Specialty { get; private set; }
    public string? Bio { get; private set; }
    public string? ServicesOffered { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsApproved { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<VetAvailability> AvailabilitySchedule { get; private set; } = new List<VetAvailability>();
    public ICollection<VetAssignment> VetAssignments { get; private set; } = new List<VetAssignment>();

    private VetProfile() { } // EF Core

    public static VetProfile Create(
        string userId,
        string fullName,
        string? clinic,
        string? specialty,
        string? bio,
        string? servicesOffered)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(fullName, nameof(fullName));

        return new VetProfile
        {
            UserId = userId,
            FullName = fullName,
            Clinic = clinic,
            Specialty = specialty,
            Bio = bio,
            ServicesOffered = servicesOffered,
            IsAvailable = true,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(
        string fullName,
        string? clinic,
        string? specialty,
        string? bio,
        string? servicesOffered)
    {
        Guard.Against.NullOrWhiteSpace(fullName, nameof(fullName));

        FullName = fullName;
        Clinic = clinic;
        Specialty = specialty;
        Bio = bio;
        ServicesOffered = servicesOffered;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        IsApproved = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        IsApproved = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
