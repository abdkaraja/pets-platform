using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Infrastructure.Services;

public class MedicalRecordService : IMedicalRecordService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateVaccinationDto> _vaccinationValidator;
    private readonly IValidator<CreateMedicationDto> _medicationValidator;
    private readonly IValidator<CreateVetVisitNoteDto> _visitNoteValidator;

    public MedicalRecordService(
        IApplicationDbContext context,
        IValidator<CreateVaccinationDto> vaccinationValidator,
        IValidator<CreateMedicationDto> medicationValidator,
        IValidator<CreateVetVisitNoteDto> visitNoteValidator)
    {
        _context = Guard.Against.Null(context, nameof(context));
        _vaccinationValidator = Guard.Against.Null(vaccinationValidator, nameof(vaccinationValidator));
        _medicationValidator = Guard.Against.Null(medicationValidator, nameof(medicationValidator));
        _visitNoteValidator = Guard.Against.Null(visitNoteValidator, nameof(visitNoteValidator));
    }

    // ── Assignment Verification ──────────────────────────────────────

    private async Task<bool> VerifyVetAssignmentAsync(int petId, string vetUserId)
    {
        return await _context.VetAssignments.AnyAsync(va =>
            va.PetId == petId &&
            va.VetProfile.UserId == vetUserId &&
            va.Status == VetAssignmentStatus.Accepted);
    }

    // ── Vaccination ──────────────────────────────────────────────────

    public async Task<Result<VaccinationRecordDto>> CreateVaccinationAsync(CreateVaccinationDto dto, string vetUserId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));

        var validation = await _vaccinationValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<VaccinationRecordDto>.Failure(errors);
        }

        var pet = await _context.Pets.FindAsync(dto.PetId);
        if (pet is null)
            return Result<VaccinationRecordDto>.Failure("Pet not found.");

        if (!await VerifyVetAssignmentAsync(dto.PetId, vetUserId))
            return Result<VaccinationRecordDto>.Failure("You must have an accepted assignment for this pet to create medical records.");

        var record = VaccinationRecord.Create(
            dto.PetId, vetUserId, dto.VaccineName, dto.DateAdministered,
            dto.BatchLotNumber, dto.NextDueDate, dto.Notes);

        _context.VaccinationRecords.Add(record);
        await _context.SaveChangesAsync();

        var created = await _context.VaccinationRecords
            .Include(vr => vr.Pet)
            .FirstAsync(vr => vr.Id == record.Id);

        return Result<VaccinationRecordDto>.Success(await MapToVaccinationDtoAsync(created));
    }

    public async Task<VaccinationRecordDto?> GetVaccinationByIdAsync(int id)
    {
        var record = await _context.VaccinationRecords
            .Include(vr => vr.Pet)
            .FirstOrDefaultAsync(vr => vr.Id == id);

        return record is null ? null : await MapToVaccinationDtoAsync(record);
    }

    public async Task<IEnumerable<VaccinationRecordDto>> GetVaccinationsByPetIdAsync(int petId)
    {
        var records = await _context.VaccinationRecords
            .Include(vr => vr.Pet)
            .Where(vr => vr.PetId == petId)
            .OrderByDescending(vr => vr.DateAdministered)
            .ToListAsync();

        var dtos = new List<VaccinationRecordDto>();
        foreach (var record in records)
        {
            dtos.Add(await MapToVaccinationDtoAsync(record));
        }
        return dtos;
    }

    // ── Medication ───────────────────────────────────────────────────

    public async Task<Result<MedicationRecordDto>> CreateMedicationAsync(CreateMedicationDto dto, string vetUserId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));

        var validation = await _medicationValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<MedicationRecordDto>.Failure(errors);
        }

        var pet = await _context.Pets.FindAsync(dto.PetId);
        if (pet is null)
            return Result<MedicationRecordDto>.Failure("Pet not found.");

        if (!await VerifyVetAssignmentAsync(dto.PetId, vetUserId))
            return Result<MedicationRecordDto>.Failure("You must have an accepted assignment for this pet to create medical records.");

        var record = MedicationRecord.Create(
            dto.PetId, vetUserId, dto.MedicationName, dto.Dosage, dto.Frequency,
            dto.StartDate, dto.EndDate, dto.PrescribingReason, dto.Instructions, dto.SideEffectsNoted);

        _context.MedicationRecords.Add(record);
        await _context.SaveChangesAsync();

        var created = await _context.MedicationRecords
            .Include(mr => mr.Pet)
            .FirstAsync(mr => mr.Id == record.Id);

        return Result<MedicationRecordDto>.Success(await MapToMedicationDtoAsync(created));
    }

    public async Task<MedicationRecordDto?> GetMedicationByIdAsync(int id)
    {
        var record = await _context.MedicationRecords
            .Include(mr => mr.Pet)
            .FirstOrDefaultAsync(mr => mr.Id == id);

        return record is null ? null : await MapToMedicationDtoAsync(record);
    }

    public async Task<IEnumerable<MedicationRecordDto>> GetMedicationsByPetIdAsync(int petId)
    {
        var records = await _context.MedicationRecords
            .Include(mr => mr.Pet)
            .Where(mr => mr.PetId == petId)
            .OrderByDescending(mr => mr.StartDate)
            .ToListAsync();

        var dtos = new List<MedicationRecordDto>();
        foreach (var record in records)
        {
            dtos.Add(await MapToMedicationDtoAsync(record));
        }
        return dtos;
    }

    // ── Visit Notes ──────────────────────────────────────────────────

    public async Task<Result<VetVisitNoteDto>> CreateVisitNoteAsync(CreateVetVisitNoteDto dto, string vetUserId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));

        var validation = await _visitNoteValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<VetVisitNoteDto>.Failure(errors);
        }

        var pet = await _context.Pets.FindAsync(dto.PetId);
        if (pet is null)
            return Result<VetVisitNoteDto>.Failure("Pet not found.");

        if (!await VerifyVetAssignmentAsync(dto.PetId, vetUserId))
            return Result<VetVisitNoteDto>.Failure("You must have an accepted assignment for this pet to create medical records.");

        var record = VetVisitNote.Create(
            dto.PetId, vetUserId, dto.VisitDate, dto.Subjective,
            dto.Objective, dto.Assessment, dto.Plan, dto.Notes);

        _context.VetVisitNotes.Add(record);
        await _context.SaveChangesAsync();

        var created = await _context.VetVisitNotes
            .Include(vn => vn.Pet)
            .FirstAsync(vn => vn.Id == record.Id);

        return Result<VetVisitNoteDto>.Success(await MapToVisitNoteDtoAsync(created));
    }

    public async Task<VetVisitNoteDto?> GetVisitNoteByIdAsync(int id)
    {
        var record = await _context.VetVisitNotes
            .Include(vn => vn.Pet)
            .FirstOrDefaultAsync(vn => vn.Id == id);

        return record is null ? null : await MapToVisitNoteDtoAsync(record);
    }

    public async Task<IEnumerable<VetVisitNoteDto>> GetVisitNotesByPetIdAsync(int petId)
    {
        var records = await _context.VetVisitNotes
            .Include(vn => vn.Pet)
            .Where(vn => vn.PetId == petId)
            .OrderByDescending(vn => vn.VisitDate)
            .ToListAsync();

        var dtos = new List<VetVisitNoteDto>();
        foreach (var record in records)
        {
            dtos.Add(await MapToVisitNoteDtoAsync(record));
        }
        return dtos;
    }

    // ── Unified Timeline (D-12) ──────────────────────────────────────

    public async Task<IEnumerable<MedicalRecordSummaryDto>> GetMedicalHistoryAsync(int petId)
    {
        var vaccinations = await _context.VaccinationRecords
            .Include(vr => vr.Pet)
            .Where(vr => vr.PetId == petId)
            .ToListAsync();

        var medications = await _context.MedicationRecords
            .Include(mr => mr.Pet)
            .Where(mr => mr.PetId == petId)
            .ToListAsync();

        var visitNotes = await _context.VetVisitNotes
            .Include(vn => vn.Pet)
            .Where(vn => vn.PetId == petId)
            .ToListAsync();

        var summary = new List<MedicalRecordSummaryDto>();

        // Resolve pet name from first available record
        var petName = vaccinations.FirstOrDefault()?.Pet?.Name
            ?? medications.FirstOrDefault()?.Pet?.Name
            ?? visitNotes.FirstOrDefault()?.Pet?.Name
            ?? string.Empty;

        // Resolve vet names from VetProfile
        var vetUserIds = vaccinations.Select(v => v.VetUserId)
            .Concat(medications.Select(m => m.VetUserId))
            .Concat(visitNotes.Select(v => v.VetUserId))
            .Distinct()
            .ToList();

        var vetProfiles = await _context.VetProfiles
            .Where(vp => vetUserIds.Contains(vp.UserId))
            .ToDictionaryAsync(vp => vp.UserId, vp => vp.FullName);

        foreach (var v in vaccinations)
        {
            vetProfiles.TryGetValue(v.VetUserId, out var vetName);
            summary.Add(new MedicalRecordSummaryDto
            {
                Id = v.Id,
                RecordType = MedicalRecordType.Vaccination,
                PetId = v.PetId,
                PetName = petName,
                VetUserName = vetName ?? string.Empty,
                Date = v.DateAdministered,
                Summary = v.VaccineName,
                CreatedAt = v.CreatedAt
            });
        }

        foreach (var m in medications)
        {
            vetProfiles.TryGetValue(m.VetUserId, out var vetName);
            summary.Add(new MedicalRecordSummaryDto
            {
                Id = m.Id,
                RecordType = MedicalRecordType.Medication,
                PetId = m.PetId,
                PetName = petName,
                VetUserName = vetName ?? string.Empty,
                Date = m.StartDate,
                Summary = $"{m.MedicationName} {m.Dosage}",
                CreatedAt = m.CreatedAt
            });
        }

        foreach (var vn in visitNotes)
        {
            vetProfiles.TryGetValue(vn.VetUserId, out var vetName);
            summary.Add(new MedicalRecordSummaryDto
            {
                Id = vn.Id,
                RecordType = MedicalRecordType.VisitNote,
                PetId = vn.PetId,
                PetName = petName,
                VetUserName = vetName ?? string.Empty,
                Date = vn.VisitDate,
                Summary = vn.Assessment,
                CreatedAt = vn.CreatedAt
            });
        }

        return summary.OrderByDescending(s => s.Date).ToList();
    }

    public async Task<IEnumerable<MedicalRecordSummaryDto>> GetRecentRecordsAsync(int petId, int count = 5)
    {
        var history = await GetMedicalHistoryAsync(petId);
        return history.Take(count);
    }

    // ── Mappers ──────────────────────────────────────────────────────

    private async Task<VaccinationRecordDto> MapToVaccinationDtoAsync(VaccinationRecord vr)
    {
        var vetProfile = await _context.VetProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == vr.VetUserId);

        return new VaccinationRecordDto
        {
            Id = vr.Id,
            PetId = vr.PetId,
            VetUserId = vr.VetUserId,
            VaccineName = vr.VaccineName,
            DateAdministered = vr.DateAdministered,
            BatchLotNumber = vr.BatchLotNumber,
            NextDueDate = vr.NextDueDate,
            Notes = vr.Notes,
            CreatedAt = vr.CreatedAt,
            UpdatedAt = vr.UpdatedAt,
            PetName = vr.Pet?.Name ?? string.Empty,
            VetUserName = vetProfile?.FullName ?? string.Empty
        };
    }

    private async Task<MedicationRecordDto> MapToMedicationDtoAsync(MedicationRecord mr)
    {
        var vetProfile = await _context.VetProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == mr.VetUserId);

        return new MedicationRecordDto
        {
            Id = mr.Id,
            PetId = mr.PetId,
            VetUserId = mr.VetUserId,
            MedicationName = mr.MedicationName,
            Dosage = mr.Dosage,
            Frequency = mr.Frequency,
            StartDate = mr.StartDate,
            EndDate = mr.EndDate,
            PrescribingReason = mr.PrescribingReason,
            Instructions = mr.Instructions,
            SideEffectsNoted = mr.SideEffectsNoted,
            CreatedAt = mr.CreatedAt,
            UpdatedAt = mr.UpdatedAt,
            PetName = mr.Pet?.Name ?? string.Empty,
            VetUserName = vetProfile?.FullName ?? string.Empty
        };
    }

    private async Task<VetVisitNoteDto> MapToVisitNoteDtoAsync(VetVisitNote vn)
    {
        var vetProfile = await _context.VetProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == vn.VetUserId);

        return new VetVisitNoteDto
        {
            Id = vn.Id,
            PetId = vn.PetId,
            VetUserId = vn.VetUserId,
            VisitDate = vn.VisitDate,
            Subjective = vn.Subjective,
            Objective = vn.Objective,
            Assessment = vn.Assessment,
            Plan = vn.Plan,
            Notes = vn.Notes,
            CreatedAt = vn.CreatedAt,
            UpdatedAt = vn.UpdatedAt,
            PetName = vn.Pet?.Name ?? string.Empty,
            VetUserName = vetProfile?.FullName ?? string.Empty
        };
    }
}
