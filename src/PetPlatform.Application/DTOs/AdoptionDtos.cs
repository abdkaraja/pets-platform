using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.DTOs;

public class AdoptionListingFilterDto
{
    public PetSpecies? Species { get; set; }
    public string? Breed { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? Location { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class AdoptionListingDto
{
    public int Id { get; init; }
    public int PetId { get; init; }
    public string ShelterUserId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Location { get; init; } = string.Empty;
    public ListingStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string PetName { get; init; } = string.Empty;
    public PetSpecies PetSpecies { get; init; }
    public string? PetBreed { get; init; }
    public int PetAge { get; init; }
    public string? PetPhotoPath { get; init; }
    public int ApplicationCount { get; init; }
}

public class CreateListingDto
{
    public int PetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class UpdateListingDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class AdoptionApplicationDto
{
    public int Id { get; init; }
    public int ListingId { get; init; }
    public string ApplicantUserId { get; init; } = string.Empty;
    public string? Message { get; init; }
    public ApplicationStatus Status { get; init; }
    public string? ReviewedByUserId { get; init; }
    public string? ReviewNotes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string ListingTitle { get; init; } = string.Empty;
    public string PetName { get; init; } = string.Empty;
    public List<ApplicationStatusHistoryDto> StatusHistory { get; init; } = new();
}

public class CreateApplicationDto
{
    public int ListingId { get; set; }
    public string? Message { get; set; }
    public HousingType HousingType { get; set; }
    public bool HasYard { get; set; }
    public int NumberOfOccupants { get; set; }
    public bool HasChildren { get; set; }
    public string? PreviousPets { get; set; }
    public string? CurrentPets { get; set; }
    public string? ExperienceLevel { get; set; }
}

public class ReviewApplicationDto
{
    public ApplicationStatus Status { get; set; }
    public string? ReviewNotes { get; set; }
}

public class ApplicationStatusHistoryDto
{
    public ApplicationStatus Status { get; init; }
    public string StatusText => Status.ToString();
    public DateTime ChangedAt { get; init; }
}
