using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class ApplicationStatusHistory
{
    public int Id { get; private set; }
    public int ApplicationId { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public AdoptionApplication Application { get; private set; } = null!;

    private ApplicationStatusHistory() { } // EF Core

    public static ApplicationStatusHistory Create(int applicationId, ApplicationStatus status)
    {
        return new ApplicationStatusHistory
        {
            ApplicationId = applicationId,
            Status = status,
            ChangedAt = DateTime.UtcNow
        };
    }
}
