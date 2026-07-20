using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class MatchNotification
{
    public int Id { get; private set; }
    public int MatchedReportId { get; private set; }
    public int TriggeredReportId { get; private set; }
    public string ReporterUserId { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public LostPetReport MatchedReport { get; private set; } = null!;
    public LostPetReport TriggeredReport { get; private set; } = null!;

    private MatchNotification() { } // EF Core

    public static MatchNotification Create(
        int matchedReportId,
        int triggeredReportId,
        string reporterUserId,
        string message)
    {
        Guard.Against.NegativeOrZero(matchedReportId, nameof(matchedReportId));
        Guard.Against.NegativeOrZero(triggeredReportId, nameof(triggeredReportId));
        Guard.Against.NullOrWhiteSpace(reporterUserId, nameof(reporterUserId));
        Guard.Against.NullOrWhiteSpace(message, nameof(message));

        return new MatchNotification
        {
            MatchedReportId = matchedReportId,
            TriggeredReportId = triggeredReportId,
            ReporterUserId = reporterUserId,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
