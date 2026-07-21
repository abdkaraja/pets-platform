using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.DTOs;

// ── VetProfile DTOs ─────────────────────────────────────────────────

public class VetProfileDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Clinic { get; set; }
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public string? ServicesOffered { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateVetProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Clinic { get; set; }
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public string? ServicesOffered { get; set; }
}

public class UpdateVetProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Clinic { get; set; }
    public string? Specialty { get; set; }
    public string? Bio { get; set; }
    public string? ServicesOffered { get; set; }
}

// ── VetAssignment DTOs ──────────────────────────────────────────────

public class VetAssignmentDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public int VetProfileId { get; set; }
    public string RequestedByUserId { get; set; } = string.Empty;
    public VetAssignmentStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Flattened
    public string PetName { get; set; } = string.Empty;
    public string VetFullName { get; set; } = string.Empty;
    public string? VetClinic { get; set; }
}

public class RequestAssignmentDto
{
    public int PetId { get; set; }
    public int VetProfileId { get; set; }
}

// ── VetAvailability DTOs ────────────────────────────────────────────

public class VetAvailabilityDto
{
    public int Id { get; set; }
    public int VetProfileId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsAvailable { get; set; }
}

public class VetSearchFilterDto
{
    public string? SearchTerm { get; set; }
    public string? Specialty { get; set; }
    public bool? IsAvailable { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

// ── VaccinationRecord DTOs ──────────────────────────────────────────

public class VaccinationRecordDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string VetUserId { get; set; } = string.Empty;
    public string VaccineName { get; set; } = string.Empty;
    public DateTime DateAdministered { get; set; }
    public string? BatchLotNumber { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Flattened
    public string PetName { get; set; } = string.Empty;
    public string VetUserName { get; set; } = string.Empty;
}

public class CreateVaccinationDto
{
    public int PetId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateTime DateAdministered { get; set; }
    public string? BatchLotNumber { get; set; }
    public DateTime? NextDueDate { get; set; }
    public string? Notes { get; set; }
}

// ── MedicationRecord DTOs ───────────────────────────────────────────

public class MedicationRecordDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string VetUserId { get; set; } = string.Empty;
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PrescribingReason { get; set; }
    public string? Instructions { get; set; }
    public string? SideEffectsNoted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Flattened
    public string PetName { get; set; } = string.Empty;
    public string VetUserName { get; set; } = string.Empty;
}

public class CreateMedicationDto
{
    public int PetId { get; set; }
    public string MedicationName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PrescribingReason { get; set; }
    public string? Instructions { get; set; }
    public string? SideEffectsNoted { get; set; }
}

// ── VetVisitNote DTOs ───────────────────────────────────────────────

public class VetVisitNoteDto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string VetUserId { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string Subjective { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Flattened
    public string PetName { get; set; } = string.Empty;
    public string VetUserName { get; set; } = string.Empty;
}

public class CreateVetVisitNoteDto
{
    public int PetId { get; set; }
    public DateTime VisitDate { get; set; }
    public string Subjective { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

// ── Unified Timeline DTO (D-12) ─────────────────────────────────────

public class MedicalRecordSummaryDto
{
    public int Id { get; set; }
    public MedicalRecordType RecordType { get; set; }
    public int PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public string VetUserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── VetDashboard DTO ────────────────────────────────────────────────

public class VetDashboardDto
{
    public int TotalAssignedPets { get; set; }
    public int PendingRequests { get; set; }
    public int RecentRecordsCount { get; set; }
    public List<VetAssignmentDto> AssignedPets { get; set; } = new();
    public List<VetAssignmentDto> PendingRequestsList { get; set; } = new();
}
