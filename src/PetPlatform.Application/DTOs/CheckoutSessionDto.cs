namespace PetPlatform.Application.DTOs;

public class CheckoutSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
}
