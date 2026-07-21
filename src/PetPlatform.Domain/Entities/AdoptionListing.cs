using Ardalis.GuardClauses;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class AdoptionListing
{
    public int Id { get; private set; }
    public int PetId { get; private set; }
    public string ShelterUserId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public ListingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Pet Pet { get; private set; } = null!;
    public ICollection<AdoptionApplication> Applications { get; private set; } = new List<AdoptionApplication>();

    private AdoptionListing() { } // EF Core

    public static AdoptionListing Create(int petId, string shelterUserId, string title, string location, string? description = null)
    {
        Guard.Against.Zero(petId, nameof(petId));
        Guard.Against.NullOrWhiteSpace(shelterUserId, nameof(shelterUserId));
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.NullOrWhiteSpace(location, nameof(location));

        return new AdoptionListing
        {
            PetId = petId,
            ShelterUserId = shelterUserId,
            Title = title,
            Location = location,
            Description = description,
            Status = ListingStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string title, string location, string? description)
    {
        Guard.Against.NullOrWhiteSpace(title, nameof(title));
        Guard.Against.NullOrWhiteSpace(location, nameof(location));

        Title = title;
        Location = location;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    private static readonly Dictionary<ListingStatus, HashSet<ListingStatus>> AllowedTransitions = new()
    {
        [ListingStatus.Active] = new() { ListingStatus.Closed, ListingStatus.Adopted },
        [ListingStatus.Pending] = new() { ListingStatus.Active, ListingStatus.Closed },
        [ListingStatus.Adopted] = new(),
        [ListingStatus.Closed] = new()
    };

    public void UpdateStatus(ListingStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }
}
