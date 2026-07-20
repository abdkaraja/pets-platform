using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.DTOs;

public class LostPetReportFilterDto
{
    public LostPetReportType? ReportType { get; set; }
    public PetSpecies? Species { get; set; }
    public string? Breed { get; set; }
    public string? Color { get; set; }
    public string? Location { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class LostPetReportDto
{
    public int Id { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public LostPetReportType ReportType { get; set; }
    public PetSpecies Species { get; set; }
    public string? Breed { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime DateReported { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? PetId { get; set; }
    public LostPetReportStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? PetName { get; set; }
    public string? PetPhotoPath { get; set; }
    public List<LostPetReportPhotoDto> Photos { get; set; } = new();
}

public class CreateLostPetReportDto
{
    public LostPetReportType ReportType { get; set; }
    public PetSpecies Species { get; set; }
    public string? Breed { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime DateReported { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? PetId { get; set; }
}

public class UpdateLostPetReportDto
{
    public string Color { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime DateReported { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class LostPetReportPhotoDto
{
    public int Id { get; set; }
    public string PhotoPath { get; set; } = string.Empty;
}

public class MatchNotificationDto
{
    public int Id { get; set; }
    public int MatchedReportId { get; set; }
    public int TriggeredReportId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? MatchedReportSpecies { get; set; }
    public string? MatchedReportLocation { get; set; }
    public string? MatchedReportDescription { get; set; }
}
