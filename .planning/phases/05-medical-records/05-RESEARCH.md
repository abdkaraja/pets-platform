# Phase 5: Medical Records & Admin Expansion - Research

**Researched:** 2026-07-21
**Domain:** ASP.NET Core MVC, EF Core, Clean Architecture — Medical Records & Vet Management
**Confidence:** HIGH

## Summary

Phase 5 adds five new domain entities (VetProfile, VetAssignment, VaccinationRecord, MedicationRecord, VetVisitNote) and three new enums (VetAssignmentStatus, MedicalRecordType, VetAvailability). The entities follow the established Domain pattern: private setters, static `Create` factory, `Guard.Against` validation, private EF Core constructor, and `CreatedAt`/`UpdatedAt` timestamps. All medical records link to `Pet` via `PetId` FK and to the vet via `VetUserId` FK (matching the `OwnerId`/`ReporterUserId` pattern used by existing entities).

The service layer follows the existing pattern: interface in `Application`, implementation in `Infrastructure` with `IApplicationDbContext` + FluentValidation, returning `Result<T>` for fallible operations. Three new service interfaces are needed: `IVetService` (profile + assignment management), `IMedicalRecordService` (CRUD for all three record types), and `IVetDashboardService` (aggregated stats). Controllers follow the existing area pattern: `[Area]` attribute, `[Authorize(Policy)]` for claims-based access, ViewModels defined at the bottom of controller files.

Two new MVC areas are created: `Areas/Vet/` (mirrors `Areas/Admin/` with sidebar layout) and expanded `Areas/Admin/` with vet management pages. The Customer area gets a `MedicalHistoryController` for pet owners to view their pet's medical records.

**Primary recommendation:** Build entities and services first (Wave 1), then controllers and views (Wave 2), then admin expansion (Wave 3). Reuse `_AdminLayout.cshtml` pattern for Vet area sidebar.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MED-01 | Vet can create vaccination history records | VaccinationRecord entity + Vet VaccinationController + MedRecordService |
| MED-02 | Vet can create medication records | MedicationRecord entity + Vet MedicationController + MedRecordService |
| MED-03 | Vet can create visit notes | VetVisitNote entity + Vet VisitNoteController + MedRecordService |
| MED-04 | Pet owner can view pet's medical records | Customer MedicalHistoryController + timeline view + filter tabs |
</phase_requirements>

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- D-01: Owner requests a vet — two-step workflow. Owner selects vet, sends assignment request. Vet accepts or rejects.
- D-02: Vet discovery via search/filter — owner searches vets by name, specialty, location. Vets have profiles: name, clinic, specialty, bio, services, availability.
- D-03: Assignment statuses: Pending, Accepted, Rejected. No expiration.
- D-04: Vaccination records: vaccine name, date administered, batch/lot number, next due date, administering vet, notes/reactions.
- D-05: Medication records: medication name, dosage, frequency, start/end date, prescribing vet, reason/diagnosis, instructions, side effects.
- D-06: Visit notes use SOAP format: Subjective, Objective, Assessment, Plan.
- D-07: All records linked to Pet via PetId FK. CreatedAt/UpdatedAt timestamps. Vet records include VetUserId FK.
- D-08: Full Vet area (Areas/Vet/) with sidebar navigation, dashboard, and multiple controllers.
- D-09: Vet dashboard: stat cards, assigned pets list, pending requests, availability calendar.
- D-10: Vet can manage own profile (clinic, specialty, bio, services, availability).
- D-11: Summary on Pet Details page — recent medical records (last 5). Full history on separate Medical History page per pet.
- D-12: Medical history timeline: unified chronological view with type badges + filter tabs.
- D-13: Full vet admin panel — Admin manages vet profiles, assigns vets to pets, views all medical records, generates reports.
- D-14: Admin can view vet profiles, approve/reject registrations, manage assignments, view records across all pets.

### Agent's Discretion
- Pagination, sorting, and table/list UX patterns — planner decides (reuses existing patterns)
- Calendar implementation details — planner decides (jQuery datepicker or simple table)
- Medical record forms layout — planner decides
- EF Core configurations and migrations — planner decides
- PDF/printable view for medical history — planner decides if in scope

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| VetProfile management | API/Backend | Database/Storage | Business logic for vet profile CRUD lives in service layer |
| Vet-Pet assignment workflow | API/Backend | Database/Storage | Two-step request/accept workflow is backend state machine |
| Vaccination record creation | API/Backend | Database/Storage | Vet submits form, service validates and persists |
| Medication record creation | API/Backend | Database/Storage | Same as vaccination — form → service → DB |
| SOAP visit notes | API/Backend | Database/Storage | Structured medical notes, backend validation |
| Medical record viewing (owner) | Frontend Server (SSR) | API/Backend | Razor Views render timeline, controller queries service |
| Vet dashboard stats | Frontend Server (SSR) | API/Backend | Aggregated queries rendered server-side |
| Admin vet management | Frontend Server (SSR) | API/Backend | Admin area expansion with new controllers |
| Vet profile search/discovery | API/Backend | Frontend Server (SSR) | Backend search/filter, rendered in customer views |

## Standard Stack

### Core (existing — no new installs)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core MVC | LTS | Web framework | Project standard — already in use |
| Entity Framework Core | LTS | ORM / Code-First migrations | Project standard — already in use |
| FluentValidation | latest | Input validation | Project standard — already in use |
| Ardalis.GuardClauses | latest | Guard clauses in entities | Project standard — already in use |
| ASP.NET Core Identity | LTS | Authentication / role management | Project standard — already in use |

### Supporting (existing — no new installs)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Tailwind CSS | via npm | UI styling | All views — already in use |
| jQuery | via CDN/npm | Client-side interactivity | Filter tabs, form interactions, calendar |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| EF Core Code-First | Database-First | Project already uses Code-First — stay consistent |
| FluentValidation | DataAnnotations | Project already uses FluentValidation — stay consistent |
| jQuery tabs | Custom JS/CSS | jQuery already available — reuse for filter tabs |

**Installation:** No new NuGet packages or npm dependencies required for this phase.

## Package Legitimacy Audit

No external packages are being installed in this phase. All work uses existing project dependencies.

## Entity Design

### Overview

Five new domain entities, two new enums, and one new `VetAvailability` day-of-week entity.

### Entity 1: VetProfile

Links to `ApplicationUser` via `UserId` FK. Contains the vet's professional information.

```csharp
// Pattern: Domain/Entities/VetProfile.cs
public class VetProfile
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;     // FK → ApplicationUser
    public string FullName { get; private set; } = string.Empty;
    public string? Clinic { get; private set; }
    public string? Specialty { get; private set; }                 // e.g., "General", "Surgery", "Dermatology"
    public string? Bio { get; private set; }
    public string? ServicesOffered { get; private set; }           // Comma-separated or JSON string
    public bool IsAvailable { get; private set; } = true;          // Whether vet is accepting patients
    public bool IsApproved { get; private set; } = false;          // Admin approval flag
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public ICollection<VetAvailability> AvailabilitySchedule { get; private set; } = new List<VetAvailability>();
    public ICollection<VetAssignment> VetAssignments { get; private set; } = new List<VetAssignment>();
}
```

**Design rationale:**
- `UserId` (not `VetUserId`) because this IS the user — mirrors `CustomerProfile.UserId` pattern.
- `IsApproved` flag allows admin to approve/reject vet registrations (D-14).
- `ServicesOffered` as string (comma-separated) keeps it simple for v1 — avoids a separate join table.
- `IsAvailable` as a quick toggle separate from availability schedule.

### Entity 2: VetAvailability

Stores the vet's weekly availability schedule. One record per day-of-week per vet.

```csharp
// Pattern: Domain/Entities/VetAvailability.cs
public class VetAvailability
{
    public int Id { get; private set; }
    public int VetProfileId { get; private set; }                  // FK → VetProfile
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public bool IsAvailable { get; private set; } = true;          // Can disable a specific day

    // Navigation
    public VetProfile VetProfile { get; private set; } = null!;
}
```

### Entity 3: VetAssignment

Tracks the vet-pet assignment workflow (D-01, D-03). Two-step: owner requests → vet accepts/rejects.

```csharp
// Pattern: Domain/Entities/VetAssignment.cs
public class VetAssignment
{
    public int Id { get; private set; }
    public int PetId { get; private set; }                         // FK → Pet
    public int VetProfileId { get; private set; }                  // FK → VetProfile
    public string RequestedByUserId { get; private set; } = string.Empty; // FK → ApplicationUser (owner)
    public VetAssignmentStatus Status { get; private set; }        // Pending, Accepted, Rejected
    public string? RejectionReason { get; private set; }           // Optional: why vet rejected
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public Pet Pet { get; private set; } = null!;
    public VetProfile VetProfile { get; private set; } = null!;
}
```

**Design rationale:**
- `RequestedByUserId` tracks who initiated the request (the owner).
- `RejectionReason` is optional but helpful for UX.
- No expiration (D-03) — records stay indefinitely.
- Only one active (Accepted) assignment per pet-vet pair is enforced in the service layer.

### Entity 4: VaccinationRecord

Per D-04 and D-07: linked to Pet via PetId FK, linked to vet via VetUserId FK.

```csharp
// Pattern: Domain/Entities/VaccinationRecord.cs
public class VaccinationRecord
{
    public int Id { get; private set; }
    public int PetId { get; private set; }                         // FK → Pet
    public string VetUserId { get; private set; } = string.Empty;  // FK → ApplicationUser (vet)
    public string VaccineName { get; private set; } = string.Empty;
    public DateTime DateAdministered { get; private set; }
    public string? BatchLotNumber { get; private set; }
    public DateTime? NextDueDate { get; private set; }
    public string? Notes { get; private set; }                     // Reactions, side effects, notes
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public Pet Pet { get; private set; } = null!;
}
```

### Entity 5: MedicationRecord

Per D-05 and D-07.

```csharp
// Pattern: Domain/Entities/MedicationRecord.cs
public class MedicationRecord
{
    public int Id { get; private set; }
    public int PetId { get; private set; }                         // FK → Pet
    public string VetUserId { get; private set; } = string.Empty;  // FK → ApplicationUser (vet)
    public string MedicationName { get; private set; } = string.Empty;
    public string Dosage { get; private set; } = string.Empty;     // e.g., "500mg"
    public string Frequency { get; private set; } = string.Empty;  // e.g., "Twice daily"
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? PrescribingReason { get; private set; }         // Reason/diagnosis
    public string? Instructions { get; private set; }              // Special instructions
    public string? SideEffectsNoted { get; private set; }          // Observed side effects
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public Pet Pet { get; private set; } = null!;
}
```

### Entity 6: VetVisitNote

Per D-06 and D-07. SOAP format.

```csharp
// Pattern: Domain/Entities/VetVisitNote.cs
public class VetVisitNote
{
    public int Id { get; private set; }
    public int PetId { get; private set; }                         // FK → Pet
    public string VetUserId { get; private set; } = string.Empty;  // FK → ApplicationUser (vet)
    public DateTime VisitDate { get; private set; }
    public string Subjective { get; private set; } = string.Empty; // Owner's description
    public string Objective { get; private set; } = string.Empty;  // Vet's findings
    public string Assessment { get; private set; } = string.Empty; // Diagnosis
    public string Plan { get; private set; } = string.Empty;       // Treatment plan
    public string? Notes { get; private set; }                     // Additional notes
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public Pet Pet { get; private set; } = null!;
}
```

### New Enums

```csharp
// Domain/Enums/VetAssignmentStatus.cs
public enum VetAssignmentStatus
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2
}

// Domain/Enums/MedicalRecordType.cs
public enum MedicalRecordType
{
    Vaccination = 0,
    Medication = 1,
    VisitNote = 2
}
```

## Database Schema

### EF Core Configurations

Each entity gets an `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`.

**VetProfileConfiguration:**
```csharp
// Pattern: follows LostPetReportConfiguration exactly
builder.HasKey(vp => vp.Id);
builder.Property(vp => vp.UserId).IsRequired().HasMaxLength(450); // Identity max
builder.Property(vp => vp.FullName).IsRequired().HasMaxLength(200);
builder.Property(vp => vp.Clinic).HasMaxLength(200);
builder.Property(vp => vp.Specialty).HasMaxLength(100);
builder.Property(vp => vp.Bio).HasMaxLength(2000);
builder.Property(vp => vp.ServicesOffered).HasMaxLength(500);
builder.HasIndex(vp => vp.UserId).IsUnique(); // One profile per user
builder.HasIndex(vp => vp.Specialty);
builder.HasIndex(vp => vp.IsApproved);
```

**VetAssignmentConfiguration:**
```csharp
builder.HasKey(va => va.Id);
builder.Property(va => va.RequestedByUserId).IsRequired().HasMaxLength(450);
builder.Property(va => va.Status).HasConversion<int>();
builder.Property(va => va.RejectionReason).HasMaxLength(500);

builder.HasOne(va => va.Pet).WithMany().HasForeignKey(va => va.PetId)
    .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete — vet records survive
builder.HasOne(va => va.VetProfile).WithMany(vp => vp.VetAssignments)
    .HasForeignKey(va => va.VetProfileId).OnDelete(DeleteBehavior.Cascade);

builder.HasIndex(va => va.PetId);
builder.HasIndex(va => va.VetProfileId);
builder.HasIndex(va => va.Status);
builder.HasIndex(va => new { va.PetId, va.VetProfileId, va.Status }); // Compound for active assignment lookup
```

**VaccinationRecordConfiguration:**
```csharp
builder.HasKey(vr => vr.Id);
builder.Property(vr => vr.VetUserId).IsRequired().HasMaxLength(450);
builder.Property(vr => vr.VaccineName).IsRequired().HasMaxLength(200);
builder.Property(vr => vr.BatchLotNumber).HasMaxLength(100);
builder.Property(vr => vr.Notes).HasMaxLength(2000);

builder.HasOne(vr => vr.Pet).WithMany().HasForeignKey(vr => vr.PetId)
    .OnDelete(DeleteBehavior.Restrict);

builder.HasIndex(vr => vr.PetId);
builder.HasIndex(vr => vr.VetUserId);
builder.HasIndex(vr => vr.DateAdministered);
builder.HasIndex(vr => new { vr.PetId, vr.DateAdministered }); // For timeline query
```

**MedicationRecordConfiguration:**
```csharp
builder.HasKey(mr => mr.Id);
builder.Property(mr => mr.VetUserId).IsRequired().HasMaxLength(450);
builder.Property(mr => mr.MedicationName).IsRequired().HasMaxLength(200);
builder.Property(mr => mr.Dosage).IsRequired().HasMaxLength(100);
builder.Property(mr => mr.Frequency).IsRequired().HasMaxLength(100);
builder.Property(mr => mr.PrescribingReason).HasMaxLength(500);
builder.Property(mr => mr.Instructions).HasMaxLength(1000);
builder.Property(mr => mr.SideEffectsNoted).HasMaxLength(1000);

builder.HasOne(mr => mr.Pet).WithMany().HasForeignKey(mr => mr.PetId)
    .OnDelete(DeleteBehavior.Restrict);

builder.HasIndex(mr => mr.PetId);
builder.HasIndex(mr => mr.VetUserId);
builder.HasIndex(mr => new { mr.PetId, mr.StartDate }); // For timeline query
```

**VetVisitNoteConfiguration:**
```csharp
builder.HasKey(vn => vn.Id);
builder.Property(vn => vn.VetUserId).IsRequired().HasMaxLength(450);
builder.Property(vn => vn.Subjective).IsRequired().HasMaxLength(4000);
builder.Property(vn => vn.Objective).IsRequired().HasMaxLength(4000);
builder.Property(vn => vn.Assessment).IsRequired().HasMaxLength(4000);
builder.Property(vn => vn.Plan).IsRequired().HasMaxLength(4000);
builder.Property(vn => vn.Notes).HasMaxLength(2000);

builder.HasOne(vn => vn.Pet).WithMany().HasForeignKey(vn => vn.PetId)
    .OnDelete(DeleteBehavior.Restrict);

builder.HasIndex(vn => vn.PetId);
builder.HasIndex(vn => vn.VetUserId);
builder.HasIndex(vn => vn.VisitDate);
builder.HasIndex(vn => new { vn.PetId, vn.VisitDate }); // For timeline query
```

**VetAvailabilityConfiguration:**
```csharp
builder.HasKey(va => va.Id);
builder.Property(va => va.DayOfWeek).HasConversion<int>();

builder.HasOne(va => va.VetProfile).WithMany(vp => vp.AvailabilitySchedule)
    .HasForeignKey(va => va.VetProfileId).OnDelete(DeleteBehavior.Cascade);

builder.HasIndex(va => new { va.VetProfileId, va.DayOfWeek }).IsUnique();
```

### DbContext Registration

Add to both `IApplicationDbContext` and `ApplicationDbContext`:

```csharp
// New DbSets
DbSet<VetProfile> VetProfiles => Set<VetProfile>();
DbSet<VetAvailability> VetAvailability => Set<VetAvailability>();
DbSet<VetAssignment> VetAssignments => Set<VetAssignment>();
DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();
DbSet<MedicationRecord> MedicationRecords => Set<MedicationRecord>();
DbSet<VetVisitNote> VetVisitNotes => Set<VetVisitNote>();
```

### Key Relationships Diagram

```
ApplicationUser ──1:1── VetProfile ──1:N── VetAvailability
                      │
                      ├──── VetAssignment ──────── Pet ────1:N── VaccinationRecord
                      │                    │                  ├──1:N── MedicationRecord
                      │                    │                  └──1:N── VetVisitNote
                      │                    │
ApplicationUser ──────┤ (owner)            └──1:N── VaccinationRecord (VetUserId)
  (owner Id)          │                          ├──1:N── MedicationRecord (VetUserId)
                      │                          └──1:N── VetVisitNote (VetUserId)
                      │
ApplicationUser ──1:N── Pet ────1:N── AdoptionListing (existing)
                   (CustomerId)       └──1:N── LostPetReport (existing)
```

**Delete behavior:** Medical records and assignments use `DeleteBehavior.Restrict` on Pet FK — deleting a pet should not cascade-delete medical history. Admin must clean up first.

## Service Layer Design

### IVetService (Application layer)

```csharp
// Pattern: follows IAdoptionService exactly
public interface IVetService
{
    // Profile management
    Task<VetProfileDto?> GetProfileByUserIdAsync(string userId);
    Task<Result<VetProfileDto>> CreateProfileAsync(CreateVetProfileDto dto, string userId);
    Task<Result<VetProfileDto>> UpdateProfileAsync(int id, UpdateVetProfileDto dto, string userId);
    Task<IEnumerable<VetProfileDto>> SearchVetsAsync(string? searchTerm, string? specialty, string? location);

    // Assignment workflow
    Task<Result<VetAssignmentDto>> RequestAssignmentAsync(int petId, int vetProfileId, string ownerUserId);
    Task<Result<VetAssignmentDto>> AcceptAssignmentAsync(int assignmentId, string vetUserId);
    Task<Result<VetAssignmentDto>> RejectAssignmentAsync(int assignmentId, string vetUserId, string? reason);
    Task<IEnumerable<VetAssignmentDto>> GetPendingRequestsAsync(string vetUserId);
    Task<IEnumerable<VetAssignmentDto>> GetAcceptedAssignmentsAsync(string vetUserId);
    Task<VetAssignmentDto?> GetActiveAssignmentAsync(int petId, string vetUserId);

    // Admin
    Task<PagedResultDto<VetProfileDto>> GetAllVetProfilesAsync(int page, int pageSize);
    Task<Result<VetProfileDto>> ApproveVetAsync(int vetProfileId);
    Task<Result<VetProfileDto>> RejectVetAsync(int vetProfileId);
    Task<IEnumerable<VetAssignmentDto>> GetAssignmentsForPetAsync(int petId);

    // Availability
    Task<IEnumerable<VetAvailabilityDto>> GetAvailabilityAsync(string vetUserId);
    Task<Result<bool>> UpdateAvailabilityAsync(string vetUserId, List<VetAvailabilityDto> schedule);
}
```

### IMedicalRecordService (Application layer)

```csharp
public interface IMedicalRecordService
{
    // Vaccination (MED-01)
    Task<Result<VaccinationRecordDto>> CreateVaccinationAsync(CreateVaccinationDto dto, string vetUserId);
    Task<VaccinationRecordDto?> GetVaccinationByIdAsync(int id);
    Task<IEnumerable<VaccinationRecordDto>> GetVaccinationsByPetIdAsync(int petId);

    // Medication (MED-02)
    Task<Result<MedicationRecordDto>> CreateMedicationAsync(CreateMedicationDto dto, string vetUserId);
    Task<MedicationRecordDto?> GetMedicationByIdAsync(int id);
    Task<IEnumerable<MedicationRecordDto>> GetMedicationsByPetIdAsync(int petId);

    // Visit Notes (MED-03)
    Task<Result<VetVisitNoteDto>> CreateVisitNoteAsync(CreateVetVisitNoteDto dto, string vetUserId);
    Task<VetVisitNoteDto?> GetVisitNoteByIdAsync(int id);
    Task<IEnumerable<VetVisitNoteDto>> GetVisitNotesByPetIdAsync(int petId);

    // Combined (MED-04 — owner view)
    Task<IEnumerable<MedicalRecordSummaryDto>> GetMedicalHistoryAsync(int petId); // unified timeline
    Task<IEnumerable<MedicalRecordSummaryDto>> GetRecentRecordsAsync(int petId, int count = 5);

    // Admin
    Task<PagedResultDto<MedicalRecordSummaryDto>> GetAllRecordsAsync(int page, int pageSize, MedicalRecordType? filter);
}
```

### Service Implementation Pattern

Services go in `Infrastructure/Services/`. Each follows the same pattern as `AdoptionService` and `LostPetService`:

```csharp
// Pattern: Infrastructure/Services/MedicalRecordService.cs
public class MedicalRecordService : IMedicalRecordService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateVaccinationDto> _createVaccinationValidator;
    // ... other validators

    // Constructor with Guard.Against.Null for each dependency (same as AdoptionService)

    public async Task<Result<VaccinationRecordDto>> CreateVaccinationAsync(
        CreateVaccinationDto dto, string vetUserId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));

        var validation = await _createVaccinationValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<VaccinationRecordDto>.Failure(errors);
        }

        // Verify pet exists
        var pet = await _context.Pets.FindAsync(dto.PetId);
        if (pet is null)
            return Result<VaccinationRecordDto>.Failure("Pet not found.");

        // Verify vet has accepted assignment for this pet
        var hasAssignment = await _context.VetAssignments
            .AnyAsync(va => va.PetId == dto.PetId
                && va.VetProfile.UserId == vetUserId
                && va.Status == VetAssignmentStatus.Accepted);
        if (!hasAssignment)
            return Result<VaccinationRecordDto>.Failure(
                "You must have an accepted assignment for this pet to create medical records.");

        // Create record using factory
        var record = VaccinationRecord.Create(dto.PetId, vetUserId, dto.VaccineName,
            dto.DateAdministered, dto.BatchLotNumber, dto.NextDueDate, dto.Notes);

        _context.VaccinationRecords.Add(record);
        await _context.SaveChangesAsync();

        return Result<VaccinationRecordDto>.Success(MapToDto(record));
    }
}
```

**Key service behaviors:**
1. All write operations verify vet has an `Accepted` assignment for the pet
2. Validation runs via FluentValidation before entity creation
3. Entity created via static `Create` factory (not `new`)
4. `Result<T>` returned for all fallible operations
5. Read operations include `.Include()` for navigation properties where needed

### MedicalRecordSummaryDto (unified timeline)

```csharp
public class MedicalRecordSummaryDto
{
    public int Id { get; set; }
    public MedicalRecordType RecordType { get; set; }
    public int PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public string VetUserName { get; set; } = string.Empty;
    public DateTime Date { get; set; }       // DateAdministered / StartDate / VisitDate
    public string Summary { get; set; } = string.Empty; // Key info for timeline display
    public DateTime CreatedAt { get; set; }
}
```

### FluentValidation Validators

New validators in `Application/Validators/`:

| Validator | Validates | Rules |
|-----------|-----------|-------|
| `CreateVaccinationValidator` | `CreateVaccinationDto` | PetId > 0, VaccineName required + max 200, DateAdministered required, BatchLotNumber max 100, NextDueDate > DateAdministered (when present), Notes max 2000 |
| `CreateMedicationValidator` | `CreateMedicationDto` | PetId > 0, MedicationName required + max 200, Dosage required + max 100, Frequency required + max 100, StartDate required, EndDate > StartDate (when present), Instructions max 1000 |
| `CreateVetVisitNoteValidator` | `CreateVetVisitNoteDto` | PetId > 0, VisitDate required, Subjective/Objective/Assessment/Plan each required + max 4000 |
| `CreateVetProfileValidator` | `CreateVetProfileDto` | UserId required, FullName required + max 200, Clinic max 200, Specialty max 100, Bio max 2000 |

## Controller & Area Structure

### New Area: Areas/Vet/

Created as a new MVC area mirroring the Admin area structure.

**Files to create:**
```
src/PetPlatform.Host.MVC/Areas/Vet/
├── Controllers/
│   ├── DashboardController.cs        // Vet dashboard (D-09)
│   ├── ProfileController.cs          // Vet profile management (D-10)
│   ├── PetsController.cs             // Assigned pets list
│   ├── VaccinationController.cs      // Vaccination CRUD (MED-01)
│   ├── MedicationController.cs       // Medication CRUD (MED-02)
│   ├── VisitNoteController.cs        // SOAP notes CRUD (MED-03)
│   └── AssignmentController.cs       // Accept/reject requests
├── Views/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Shared/
│   │   └── _VetLayout.cshtml         // Sidebar layout (mirrors _AdminLayout)
│   ├── Dashboard/
│   │   └── Index.cshtml
│   ├── Profile/
│   │   ├── Index.cshtml              // View + edit profile
│   │   └── Edit.cshtml
│   ├── Pets/
│   │   ├── Index.cshtml              // Assigned pets list
│   │   └── Details.cshtml            // Pet detail with medical records
│   ├── Vaccination/
│   │   ├── Create.cshtml
│   │   └── Details.cshtml
│   ├── Medication/
│   │   ├── Create.cshtml
│   │   └── Details.cshtml
│   ├── VisitNote/
│   │   ├── Create.cshtml
│   │   └── Details.cshtml
│   └── Assignment/
│       ├── Pending.cshtml            // Pending requests list
│       └── Details.cshtml            // View request + accept/reject
```

**Controller pattern (follows Admin area exactly):**
```csharp
[Area("Vet")]
[Authorize(Roles = "Vet")]
public class DashboardController : Controller
{
    private readonly IVetService _vetService;
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly UserManager<ApplicationUser> _userManager;

    // Constructor DI (same pattern as Admin controllers)

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _vetService.GetProfileByUserIdAsync(userId!);
        if (profile is null) return RedirectToAction("Create", "Profile");

        // Fetch stats
        var assignments = await _vetService.GetAcceptedAssignmentsAsync(userId!);
        var pendingRequests = await _vetService.GetPendingRequestsAsync(userId!);
        var recentRecords = await _medicalRecordService.GetRecentRecordsAsync(/* for all assigned pets */);

        // Render dashboard
        return View(new VetDashboardViewModel
        {
            TotalAssignedPets = assignments.Count(),
            PendingRequests = pendingRequests.Count(),
            RecentRecords = recentRecords.ToList()
        });
    }
}
```

**Authorization pattern:**
- Vet area controllers: `[Authorize(Roles = "Vet")]` — only vets can access
- Individual write actions verify vet owns the assignment via service layer
- Admin area controllers for vet management: `[Authorize(Policy = "Permission:Users.View")]` (existing pattern)

**Sidebar layout (_VetLayout.cshtml):**
Mirrors `_AdminLayout.cshtml` exactly — same structure, different nav links:
- Dashboard
- My Profile
- Assigned Pets
- Pending Requests
- (Medical record links appear on individual pet pages, not in sidebar)

### Expanded Admin Area

**Files to add:**
```
src/PetPlatform.Host.MVC/Areas/Admin/Controllers/
├── VetManagementController.cs       // Admin vet management (D-13, D-14)
├── VetAssignmentController.cs       // Admin vet assignments

src/PetPlatform.Host.MVC/Areas/Admin/Views/
├── VetManagement/
│   ├── Index.cshtml                 // List all vets with approval status
│   ├── Details.cshtml               // Vet profile detail
│   ├── PendingApprovals.cshtml      // Vets awaiting approval
│   └── MedicalRecords.cshtml        // View medical records across all pets
├── VetAssignment/
│   ├── Index.cshtml                 // List all assignments
│   └── Create.cshtml                // Admin manually assigns vet to pet
```

**Admin sidebar update:** Add "Vet Management" and "Vet Assignments" nav links to `_AdminLayout.cshtml`.

### Expanded Customer Area

**Files to add:**
```
src/PetPlatform.Host.MVC/Areas/Customer/Controllers/
├── VetDiscoveryController.cs        // Browse/search vets (D-02)
├── MedicalHistoryController.cs      // View pet medical records (D-11, D-12)

src/PetPlatform.Host.MVC/Areas/Customer/Views/
├── VetDiscovery/
│   ├── Index.cshtml                 // Search/filter vets
│   └── Details.cshtml               // Vet profile + request assignment button
├── MedicalHistory/
│   ├── Index.cshtml                 // Medical history timeline for a pet (D-12)
│   └── _MedicalRecordRow.cshtml     // Partial for timeline entry with type badge
```

**Pet Details page update:** Add "Medical Records" section showing last 5 records (D-11) with link to full history.

## View Layer Design

### Vet Dashboard View (D-09)

```html
<!-- Stat cards row -->
<div class="grid grid-cols-3 gap-4 mb-6">
    <div class="bg-white rounded-lg shadow p-4">
        <h3 class="text-sm text-gray-500">Assigned Pets</h3>
        <p class="text-2xl font-bold text-gray-800">@Model.TotalAssignedPets</p>
    </div>
    <div class="bg-white rounded-lg shadow p-4">
        <h3 class="text-sm text-gray-500">Pending Requests</h3>
        <p class="text-2xl font-bold text-yellow-600">@Model.PendingRequests</p>
    </div>
    <div class="bg-white rounded-lg shadow p-4">
        <h3 class="text-sm text-gray-500">Recent Records</h3>
        <p class="text-2xl font-bold text-blue-600">@Model.RecentRecordsCount</p>
    </div>
</div>

<!-- Assigned pets table (same pattern as User/Index.cshtml) -->
<!-- Pending requests list with Accept/Reject buttons -->
```

### Medical History Timeline (D-12)

```html
<!-- Filter tabs — jQuery driven -->
<div class="flex gap-2 mb-4">
    <button class="filter-tab active" data-filter="all">All</button>
    <button class="filter-tab" data-filter="vaccination">Vaccinations</button>
    <button class="filter-tab" data-filter="medication">Medications</button>
    <button class="filter-tab" data-filter="visitnote">Visit Notes</button>
</div>

<!-- Unified timeline -->
<div class="space-y-4">
    @foreach (var record in Model.Records)
    {
        <div class="bg-white rounded-lg shadow p-4 flex gap-4 medical-record" data-type="@record.RecordType.ToString().ToLower()">
            <div class="flex-shrink-0">
                @switch (record.RecordType)
                {
                    case MedicalRecordType.Vaccination:
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">Vaccination</span>
                        break;
                    case MedicalRecordType.Medication:
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">Medication</span>
                        break;
                    case MedicalRecordType.VisitNote:
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">Visit Note</span>
                        break;
                }
            </div>
            <div class="flex-1">
                <p class="text-sm text-gray-500">@record.Date.ToString("MMM dd, yyyy")</p>
                <p class="font-medium text-gray-800">@record.Summary</p>
                <p class="text-sm text-gray-600">by @record.VetUserName</p>
            </div>
        </div>
    }
</div>

@section Scripts {
    <script>
        $(function() {
            $('.filter-tab').on('click', function() {
                var filter = $(this).data('filter');
                $('.filter-tab').removeClass('active');
                $(this).addClass('active');
                $('.medical-record').each(function() {
                    if (filter === 'all' || $(this).data('type') === filter) {
                        $(this).show();
                    } else {
                        $(this).hide();
                    }
                });
            });
        });
    </script>
}
```

### Medical Record Forms

**Vaccination Create Form:**
```html
<!-- Standard form pattern — follows CreateProduct/CreateLostPetReport -->
<form asp-action="Create" asp-route-petId="@Model.PetId" method="post">
    @Html.AntiForgeryToken()
    <div asp-validation-summary="ModelOnly" class="text-red-600 mb-4"></div>

    <input type="hidden" asp-for="PetId" />
    <div class="grid grid-cols-2 gap-4">
        <div>
            <label asp-for="VaccineName" class="block text-sm font-medium text-gray-700">Vaccine Name *</label>
            <input asp-for="VaccineName" class="mt-1 block w-full border rounded-md px-3 py-2" />
            <span asp-validation-for="VaccineName" class="text-red-600 text-sm"></span>
        </div>
        <div>
            <label asp-for="DateAdministered" class="block text-sm font-medium text-gray-700">Date Administered *</label>
            <input asp-for="DateAdministered" type="date" class="mt-1 block w-full border rounded-md px-3 py-2" />
            <span asp-validation-for="DateAdministered" class="text-red-600 text-sm"></span>
        </div>
        <!-- Batch/Lot Number, Next Due Date, Notes -->
    </div>
    <div class="mt-4">
        <button type="submit" class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700">
            Save Vaccination Record
        </button>
    </div>
</form>
```

**SOAP Visit Note Form:**
```html
<form asp-action="Create" asp-route-petId="@Model.PetId" method="post">
    @Html.AntiForgeryToken()
    <div asp-validation-summary="ModelOnly" class="text-red-600 mb-4"></div>

    <input type="hidden" asp-for="PetId" />
    <div class="mb-4">
        <label asp-for="VisitDate" class="block text-sm font-medium text-gray-700">Visit Date *</label>
        <input asp-for="VisitDate" type="date" class="mt-1 block w-full border rounded-md px-3 py-2" />
    </div>

    <!-- SOAP sections — stacked textareas -->
    <div class="space-y-4">
        <div>
            <label asp-for="Subjective" class="block text-sm font-medium text-gray-700">Subjective (Owner's Description) *</label>
            <textarea asp-for="Subjective" rows="3" class="mt-1 block w-full border rounded-md px-3 py-2"></textarea>
        </div>
        <div>
            <label asp-for="Objective" class="block text-sm font-medium text-gray-700">Objective (Clinical Findings) *</label>
            <textarea asp-for="Objective" rows="3" class="mt-1 block w-full border rounded-md px-3 py-2"></textarea>
        </div>
        <div>
            <label asp-for="Assessment" class="block text-sm font-medium text-gray-700">Assessment (Diagnosis) *</label>
            <textarea asp-for="Assessment" rows="3" class="mt-1 block w-full border rounded-md px-3 py-2"></textarea>
        </div>
        <div>
            <label asp-for="Plan" class="block text-sm font-medium text-gray-700">Plan (Treatment Plan) *</label>
            <textarea asp-for="Plan" rows="3" class="mt-1 block w-full border rounded-md px-3 py-2"></textarea>
        </div>
    </div>

    <div class="mt-4">
        <button type="submit" class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700">
            Save Visit Note
        </button>
    </div>
</form>
```

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Input validation | Custom if/else checks | FluentValidation | Project standard, testable, declarative |
| Error handling | try/catch + ViewBag | Result<T> pattern | Consistent, typed, composable |
| Entity creation | `new Entity { ... }` | Static `Create` factory | Guard clauses, invariant enforcement |
| Pagination | Manual skip/take + ViewBag | PagedResultDto<T> | Reusable, typed, includes metadata |
| Identity management | Custom auth code | UserManager/RoleManager | Handles hashing, lockout, claims |
| Date filtering | Manual date arithmetic | EF Core LINQ Where clauses | Delegates to SQL, efficient |

**Key insight:** The existing codebase has zero reason to deviate from established patterns — every "don't hand-roll" item already has a project-proven implementation to reuse.

## Integration Points

### 1. IApplicationDbContext Expansion

Add 6 new `DbSet` properties. Both `IApplicationDbContext` (Application layer) and `ApplicationDbContext` (Infrastructure layer) must be updated in parallel.

**Risk:** Forgetting to update the interface will cause compilation errors in the Application layer services. This is caught at compile time.

### 2. Identity System

The `Vet` role already exists in `SeedData.cs` (line 17). The new `VetProfile` entity links to `ApplicationUser` via `UserId`. When a vet creates their profile, they should already have the `Vet` role assigned.

**Admin approval flow:** New vets register → Admin sees pending approval in `VetManagement/PendingApprovals.cshtml` → Admin approves → `VetProfile.IsApproved = true` → Vet can access Vet area.

### 3. Pet Entity

Medical records link to `Pet` via `PetId` FK. The `Pet` entity already has `OwnerId` (for pet owner access) and `CreatedAt`/`UpdatedAt`. No changes needed to the `Pet` entity itself.

**Pet Details page (Customer area):** Add a "Medical Records" section to the existing `MyPets/Details.cshtml` view showing last 5 records with a "View Full History" link.

### 4. Admin Area

- `_AdminLayout.cshtml` sidebar: Add "Vet Management" and "Vet Assignments" nav links
- New `VetManagementController` and `VetAssignmentController`
- Seed additional permission claims: `"Vets.View"`, `"Vets.Manage"`, `"Records.View"`

### 5. Customer Area

- New `VetDiscoveryController` for vet search/selection
- New `MedicalHistoryController` for viewing pet medical records
- `MyPetsController.Details` view: Add medical record summary section

## Risks & Mitigations

### Risk 1: Timeline Query Performance
**What:** Unified medical history combines three different tables with different date fields (DateAdministered, StartDate, VisitDate) into a single sorted timeline.
**Impact:** Three separate queries unioned and sorted in memory could be slow for pets with many records.
**Mitigation:** For v1 with typical record counts (< 100 per pet), three separate `.ToListAsync()` calls sorted in C# is fine. If performance degrades, create a `MedicalRecordSummary` materialized view or a single `UNION ALL` query via raw SQL.

### Risk 2: Duplicate Active Assignments
**What:** Multiple vets could have "Accepted" assignments for the same pet simultaneously.
**Impact:** Conflicting medical records, unclear vet responsibility.
**Mitigation:** Service layer enforces: only one `Accepted` assignment per pet at a time. Before accepting, check for existing accepted assignments and reject/block if one exists. Add a compound unique index on `(PetId, Status)` where `Status = Accepted` — but since EF Core doesn't support filtered unique indexes well, enforce in service code.

### Risk 3: Vet Profile Without Role
**What:** A user creates a VetProfile but doesn't have the `Vet` role, or vice versa.
**Impact:** Authorization mismatch — user has profile but can't access Vet area, or has role but no profile.
**Mitigation:** Vet area controllers check both: `User.IsInRole("Vet")` AND profile exists. If profile missing, redirect to profile creation. Admin approval flow ensures `IsApproved` flag is set before vet can use the area.

### Risk 4: Medical Record Integrity
**What:** Vet creates record for a pet they're not assigned to.
**Impact:** Unauthorized medical records in the system.
**Mitigation:** Every write operation in `MedicalRecordService` verifies the vet has an `Accepted` `VetAssignment` for the target `PetId` before allowing creation.

### Risk 5: Soft vs Hard Delete
**What:** Deleting a vet profile or assignment might cascade unexpectedly.
**Impact:** Medical records lost if cascade delete is configured.
**Mitigation:** All FK relationships use `DeleteBehavior.Restrict` on `PetId` for medical records. Vet profile cascade only on `VetAvailability` (owned schedule data). Service layer validates no active records before allowing profile deletion.

## Reusable Assets

| Asset | Location | Reuse |
|-------|----------|-------|
| `Result<T>` pattern | `Application/Common/Result.cs` | All service operations |
| `PagedResultDto<T>` | `Application/DTOs/PagedResultDto.cs` | Admin list views, search results |
| `_AdminLayout.cshtml` | `Areas/Admin/Views/Shared/` | Template for `_VetLayout.cshtml` |
| `User/Index.cshtml` table pattern | `Areas/Admin/Views/User/` | Vet list, assignment list views |
| `CreateLostPetReportValidator` | `Application/Validators/` | Pattern for new validators |
| `AdoptionService` constructor pattern | `Application/Services/AdoptionService.cs` | Template for VetService, MedicalRecordService |
| `LostPetService` query pattern | `Infrastructure/Services/` | Template for medical record queries |
| `PetConfiguration` | `Infrastructure/Persistence/Configurations/` | Pattern for new entity configurations |
| `LostPetReportConfiguration` | `Infrastructure/Persistence/Configurations/` | Pattern for FK + index configurations |
| `SeedData.cs` role seeding | `Infrastructure/Identity/` | Extend with vet permission claims |
| `MyPetsController` | `Areas/Customer/Controllers/` | Pattern for customer-facing controllers |
| `PetDetails.cshtml` view | `Areas/Customer/Views/MyPets/` | Extend with medical records section |

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | No new NuGet packages needed — all dependencies already installed | Standard Stack | Medium — might need a date picker library if jQuery UI not bundled |
| A2 | ASP.NET Core LTS version supports `TimeOnly` type in EF Core | Entity Design | Low — `TimeOnly` available since .NET 6 |
| A3 | Existing `Vet` role assignment is handled during user registration (Phase 1) | Integration Points | Low — role seeding exists, just needs registration flow |
| A4 | Tailwind CSS build pipeline handles new view styling without config changes | View Layer | Low — existing setup already compiles all views |

## Open Questions

1. **Admin approval flow trigger:**
   - What we know: D-14 says "approve/reject vet registrations"
   - What's unclear: Does this mean new users registering as vets auto-need approval, or is it manual admin action on existing Vet-role users?
   - Recommendation: Admin manually approves — simpler for v1. Vet creates profile, admin sees pending in VetManagement panel.

2. **Pet Details page integration:**
   - What we know: D-11 says "recent medical records (last 5) on Pet Details page"
   - What's unclear: How does this integrate with the existing `MyPets/Details.cshtml` view?
   - Recommendation: Add a partial view `_MedicalRecordsSummary.cshtml` rendered at the bottom of the existing Details view. No layout changes needed.

## Validation Architecture

> Skip — `workflow.nyquist_validation` is explicitly `false` in `.planning/config.json`.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity (existing) |
| V3 Session Management | yes | ASP.NET Core session middleware (existing) |
| V4 Access Control | yes | Policy-based + role-based authorization |
| V5 Input Validation | yes | FluentValidation (existing pattern) |
| V6 Cryptography | no | No crypto operations in this phase |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Unauthorized medical record creation | Tampering | Verify Accepted assignment before write |
| Vet accessing another vet's records | Information Disclosure | Service layer filters by VetUserId |
| Admin accessing vet-only area | Elevation of Privilege | `[Authorize(Roles = "Vet")]` on Vet area controllers |
| XSS in SOAP notes | Tampering | Razor `@` auto-encoding (default behavior) |
| CSRF on medical record forms | Tampering | `[ValidateAntiForgeryToken]` on all POST actions |

## Sources

### Primary (HIGH confidence)
- Codebase files: ApplicationDbContext.cs, Pet.cs, AdoptionService.cs, IAdoptionService.cs, Admin controllers, _AdminLayout.cshtml, SeedData.cs, LostPetService.cs, LostPetReport.cs, PetConfiguration.cs, LostPetReportConfiguration.cs, Result.cs, PagedResultDto.cs, AdoptionDtos.cs, LostPetDtos.cs, CustomerProfile.cs, MyPetsController.cs, CreateLostPetReportValidator.cs, User/Index.cshtml — all read directly from project source

### Secondary (MEDIUM confidence)
- ASP.NET Core MVC area conventions — standard Microsoft documentation patterns
- EF Core `IEntityTypeConfiguration` pattern — standard Microsoft documentation patterns

### Tertiary (LOW confidence)
None — all findings grounded in direct codebase reading and established .NET patterns.

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — all libraries already in use, no new dependencies
- Architecture: HIGH — patterns extracted directly from existing code, Clean Architecture already established
- Pitfalls: HIGH — risks identified from understanding entity relationships and authorization flows

**Research date:** 2026-07-21
**Valid until:** 2026-08-21 (30 days — stable tech stack, no fast-moving dependencies)

## RESEARCH COMPLETE
