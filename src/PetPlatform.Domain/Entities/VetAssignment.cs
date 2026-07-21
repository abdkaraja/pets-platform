using Ardalis.GuardClauses;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class VetAssignment
{
    public int Id { get; private set; }
    public int PetId { get; private set; }
    public int VetProfileId { get; private set; }
    public string RequestedByUserId { get; private set; } = string.Empty;
    public VetAssignmentStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Pet Pet { get; private set; } = null!;
    public VetProfile VetProfile { get; private set; } = null!;

    private VetAssignment() { } // EF Core

    public static VetAssignment Create(
        int petId,
        int vetProfileId,
        string requestedByUserId)
    {
        Guard.Against.NullOrWhiteSpace(requestedByUserId, nameof(requestedByUserId));
        if (petId <= 0)
            throw new ArgumentException("PetId must be greater than 0.", nameof(petId));
        if (vetProfileId <= 0)
            throw new ArgumentException("VetProfileId must be greater than 0.", nameof(vetProfileId));

        return new VetAssignment
        {
            PetId = petId,
            VetProfileId = vetProfileId,
            RequestedByUserId = requestedByUserId,
            Status = VetAssignmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Accept()
    {
        if (Status != VetAssignmentStatus.Pending)
            throw new InvalidOperationException("Only Pending assignments can be accepted.");

        Status = VetAssignmentStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reject(string? reason)
    {
        if (Status != VetAssignmentStatus.Pending)
            throw new InvalidOperationException("Only Pending assignments can be rejected.");

        Status = VetAssignmentStatus.Rejected;
        RejectionReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }
}
