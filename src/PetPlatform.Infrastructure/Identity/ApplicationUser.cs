using Microsoft.AspNetCore.Identity;

namespace PetPlatform.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
