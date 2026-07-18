using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class CustomerProfile
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? City { get; private set; }
    public bool NotificationPreferences { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private CustomerProfile() { } // EF Core

    public static CustomerProfile Create(string userId, string fullName,
                                         string? address = null, string? phone = null,
                                         string? city = null, bool notificationPreferences = true)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(fullName, nameof(fullName));

        return new CustomerProfile
        {
            UserId = userId,
            FullName = fullName,
            Address = address,
            Phone = phone,
            City = city,
            NotificationPreferences = notificationPreferences,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string fullName, string? address, string? phone, string? city, bool notificationPreferences)
    {
        Guard.Against.NullOrWhiteSpace(fullName, nameof(fullName));

        FullName = fullName;
        Address = address;
        Phone = phone;
        City = city;
        NotificationPreferences = notificationPreferences;
        UpdatedAt = DateTime.UtcNow;
    }
}
