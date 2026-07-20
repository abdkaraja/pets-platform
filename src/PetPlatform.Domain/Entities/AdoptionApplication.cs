using Ardalis.GuardClauses;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class AdoptionApplication
{
    public int Id { get; private set; }
    public int ListingId { get; private set; }
    public string ApplicantUserId { get; private set; } = string.Empty;
    public string? Message { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public string? ReviewedByUserId { get; private set; }
    public string? ReviewNotes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public AdoptionListing Listing { get; private set; } = null!;
    public ICollection<ApplicationStatusHistory> StatusHistory { get; private set; } = new List<ApplicationStatusHistory>();

    private AdoptionApplication() { } // EF Core

    private static readonly Dictionary<ApplicationStatus, HashSet<ApplicationStatus>> AllowedTransitions = new()
    {
        [ApplicationStatus.Submitted] = new() { ApplicationStatus.UnderReview, ApplicationStatus.Withdrawn },
        [ApplicationStatus.UnderReview] = new() { ApplicationStatus.Approved, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn },
        [ApplicationStatus.Approved] = new(),
        [ApplicationStatus.Rejected] = new(),
        [ApplicationStatus.Withdrawn] = new()
    };

    public static AdoptionApplication Create(int listingId, string applicantUserId, string? message = null)
    {
        Guard.Against.Zero(listingId, nameof(listingId));
        Guard.Against.NullOrWhiteSpace(applicantUserId, nameof(applicantUserId));

        var application = new AdoptionApplication
        {
            ListingId = listingId,
            ApplicantUserId = applicantUserId,
            Message = message,
            Status = ApplicationStatus.Submitted,
            CreatedAt = DateTime.UtcNow
        };

        application.StatusHistory.Add(ApplicationStatusHistory.Create(application.Id, ApplicationStatus.Submitted));

        return application;
    }

    public void UpdateStatus(ApplicationStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException($"Invalid status transition from {Status} to {newStatus}.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        StatusHistory.Add(ApplicationStatusHistory.Create(Id, newStatus));
    }

    public void Review(string reviewerUserId, ApplicationStatus newStatus, string? notes = null)
    {
        UpdateStatus(newStatus);
        ReviewedByUserId = reviewerUserId;
        ReviewNotes = notes;
    }
}
