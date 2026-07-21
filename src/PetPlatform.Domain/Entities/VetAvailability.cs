using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class VetAvailability
{
    public int Id { get; private set; }
    public int VetProfileId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsAvailable { get; private set; }

    public VetProfile VetProfile { get; private set; } = null!;

    private VetAvailability() { } // EF Core

    public static VetAvailability Create(
        int vetProfileId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        if (vetProfileId <= 0)
            throw new ArgumentException("VetProfileId must be greater than 0.", nameof(vetProfileId));

        if (startTime >= endTime)
            throw new ArgumentException("StartTime must be before EndTime.", nameof(startTime));

        return new VetAvailability
        {
            VetProfileId = vetProfileId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsAvailable = true
        };
    }

    public void Update(TimeOnly startTime, TimeOnly endTime, bool isAvailable)
    {
        if (startTime >= endTime)
            throw new ArgumentException("StartTime must be before EndTime.", nameof(startTime));

        StartTime = startTime;
        EndTime = endTime;
        IsAvailable = isAvailable;
    }
}
