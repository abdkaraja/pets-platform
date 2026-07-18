namespace PetPlatform.Application.DTOs;

public class CustomerProfileDto
{
    public int Id { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? City { get; init; }
    public bool NotificationPreferences { get; init; }
}

public class UpdateCustomerProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public bool NotificationPreferences { get; set; }
}
