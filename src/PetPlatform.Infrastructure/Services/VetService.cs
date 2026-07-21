using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Infrastructure.Services;

public class VetService : IVetService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateVetProfileDto> _createProfileValidator;
    private readonly IValidator<UpdateVetProfileDto> _updateProfileValidator;

    public VetService(
        IApplicationDbContext context,
        IValidator<CreateVetProfileDto> createProfileValidator,
        IValidator<UpdateVetProfileDto> updateProfileValidator)
    {
        _context = Guard.Against.Null(context, nameof(context));
        _createProfileValidator = Guard.Against.Null(createProfileValidator, nameof(createProfileValidator));
        _updateProfileValidator = Guard.Against.Null(updateProfileValidator, nameof(updateProfileValidator));
    }

    // ── Profile Management ───────────────────────────────────────────

    public async Task<VetProfileDto?> GetProfileByUserIdAsync(string userId)
    {
        var profile = await _context.VetProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == userId);

        return profile is null ? null : MapToProfileDto(profile);
    }

    public async Task<Result<VetProfileDto>> CreateProfileAsync(CreateVetProfileDto dto, string userId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var validation = await _createProfileValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<VetProfileDto>.Failure(errors);
        }

        var existingProfile = await _context.VetProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == userId);

        if (existingProfile is not null)
            return Result<VetProfileDto>.Failure("A vet profile already exists for this user.");

        var profile = VetProfile.Create(userId, dto.FullName, dto.Clinic, dto.Specialty, dto.Bio, dto.ServicesOffered);
        _context.VetProfiles.Add(profile);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            return Result<VetProfileDto>.Failure("A vet profile already exists for this user.");
        }

        return Result<VetProfileDto>.Success(MapToProfileDto(profile));
    }

    public async Task<Result<VetProfileDto>> UpdateProfileAsync(int id, UpdateVetProfileDto dto, string userId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var validation = await _updateProfileValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<VetProfileDto>.Failure(errors);
        }

        var profile = await _context.VetProfiles.FindAsync(id);
        if (profile is null)
            return Result<VetProfileDto>.Failure("Vet profile not found.");

        if (profile.UserId != userId)
            return Result<VetProfileDto>.Failure("You are not authorized to modify this profile.");

        profile.UpdateProfile(dto.FullName, dto.Clinic, dto.Specialty, dto.Bio, dto.ServicesOffered);
        await _context.SaveChangesAsync();

        return Result<VetProfileDto>.Success(MapToProfileDto(profile));
    }

    // ── Vet Discovery ────────────────────────────────────────────────

    public async Task<PagedResultDto<VetProfileDto>> SearchVetsAsync(VetSearchFilterDto filter)
    {
        var query = _context.VetProfiles
            .Where(vp => vp.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(vp =>
                vp.FullName.ToLower().Contains(term) ||
                (vp.Clinic != null && vp.Clinic.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Specialty))
            query = query.Where(vp => vp.Specialty != null && vp.Specialty.Contains(filter.Specialty));

        if (filter.IsAvailable.HasValue)
            query = query.Where(vp => vp.IsAvailable == filter.IsAvailable.Value);

        var totalCount = await query.CountAsync();

        var items = (await query
            .OrderByDescending(vp => vp.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync())
            .Select(MapToProfileDto)
            .ToList();

        return new PagedResultDto<VetProfileDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    // ── Assignment Workflow ───────────────────────────────────────────

    public async Task<Result<VetAssignmentDto>> RequestAssignmentAsync(int petId, int vetProfileId, string ownerUserId)
    {
        Guard.Against.NullOrWhiteSpace(ownerUserId, nameof(ownerUserId));

        var pet = await _context.Pets.FindAsync(petId);
        if (pet is null)
            return Result<VetAssignmentDto>.Failure("Pet not found.");

        if (pet.OwnerId != ownerUserId)
            return Result<VetAssignmentDto>.Failure("You are not authorized to request an assignment for this pet.");

        var vetProfile = await _context.VetProfiles.FindAsync(vetProfileId);
        if (vetProfile is null)
            return Result<VetAssignmentDto>.Failure("Vet profile not found.");

        if (!vetProfile.IsApproved)
            return Result<VetAssignmentDto>.Failure("This vet is not yet approved.");

        var existingAssignment = await _context.VetAssignments
            .FirstOrDefaultAsync(va =>
                va.PetId == petId &&
                va.VetProfileId == vetProfileId &&
                (va.Status == VetAssignmentStatus.Pending || va.Status == VetAssignmentStatus.Accepted));

        if (existingAssignment is not null)
            return Result<VetAssignmentDto>.Failure("An active assignment already exists for this pet-vet pair.");

        var assignment = VetAssignment.Create(petId, vetProfileId, ownerUserId);
        _context.VetAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        var created = await _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .FirstAsync(va => va.Id == assignment.Id);

        return Result<VetAssignmentDto>.Success(MapToAssignmentDto(created));
    }

    public async Task<Result<VetAssignmentDto>> AcceptAssignmentAsync(int assignmentId, string vetUserId)
    {
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));

        var assignment = await _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .FirstOrDefaultAsync(va => va.Id == assignmentId);

        if (assignment is null)
            return Result<VetAssignmentDto>.Failure("Assignment not found.");

        if (assignment.VetProfile.UserId != vetUserId)
            return Result<VetAssignmentDto>.Failure("You are not authorized to accept this assignment.");

        if (assignment.Status != VetAssignmentStatus.Pending)
            return Result<VetAssignmentDto>.Failure("Only pending assignments can be accepted.");

        // One active vet per pet (risk 2)
        var existingAccepted = await _context.VetAssignments
            .AnyAsync(va =>
                va.PetId == assignment.PetId &&
                va.Status == VetAssignmentStatus.Accepted &&
                va.Id != assignmentId);

        if (existingAccepted)
            return Result<VetAssignmentDto>.Failure("This pet already has an active vet assignment.");

        assignment.Accept();
        await _context.SaveChangesAsync();

        return Result<VetAssignmentDto>.Success(MapToAssignmentDto(assignment));
    }

    public async Task<Result<VetAssignmentDto>> RejectAssignmentAsync(int assignmentId, string vetUserId, string? reason)
    {
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));

        var assignment = await _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .FirstOrDefaultAsync(va => va.Id == assignmentId);

        if (assignment is null)
            return Result<VetAssignmentDto>.Failure("Assignment not found.");

        if (assignment.VetProfile.UserId != vetUserId)
            return Result<VetAssignmentDto>.Failure("You are not authorized to reject this assignment.");

        if (assignment.Status != VetAssignmentStatus.Pending)
            return Result<VetAssignmentDto>.Failure("Only pending assignments can be rejected.");

        assignment.Reject(reason);
        await _context.SaveChangesAsync();

        return Result<VetAssignmentDto>.Success(MapToAssignmentDto(assignment));
    }

    public async Task<IEnumerable<VetAssignmentDto>> GetPendingRequestsAsync(string vetUserId)
    {
        var assignments = await _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .Where(va => va.VetProfile.UserId == vetUserId && va.Status == VetAssignmentStatus.Pending)
            .OrderByDescending(va => va.CreatedAt)
            .ToListAsync();

        return assignments.Select(MapToAssignmentDto);
    }

    public async Task<IEnumerable<VetAssignmentDto>> GetAcceptedAssignmentsAsync(string vetUserId)
    {
        var assignments = await _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .Where(va => va.VetProfile.UserId == vetUserId && va.Status == VetAssignmentStatus.Accepted)
            .OrderByDescending(va => va.CreatedAt)
            .ToListAsync();

        return assignments.Select(MapToAssignmentDto);
    }

    public async Task<VetAssignmentDto?> GetActiveAssignmentAsync(int petId, string vetUserId)
    {
        var assignment = await _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .FirstOrDefaultAsync(va =>
                va.PetId == petId &&
                va.VetProfile.UserId == vetUserId &&
                va.Status == VetAssignmentStatus.Accepted);

        return assignment is null ? null : MapToAssignmentDto(assignment);
    }

    public async Task<VetAssignmentDto?> GetAssignmentByIdAsync(int assignmentId, string vetUserId)
    {
        Guard.Against.NullOrWhiteSpace(vetUserId, nameof(vetUserId));

        var assignment = await _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .FirstOrDefaultAsync(va =>
                va.Id == assignmentId &&
                va.VetProfile.UserId == vetUserId);

        return assignment is null ? null : MapToAssignmentDto(assignment);
    }

    public async Task<IEnumerable<VetAssignmentDto>> GetAssignmentsForPetAsync(int petId)
    {
        var assignments = await _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .Where(va => va.PetId == petId)
            .OrderByDescending(va => va.CreatedAt)
            .ToListAsync();

        return assignments.Select(MapToAssignmentDto);
    }

    // ── Admin ────────────────────────────────────────────────────────

    public async Task<PagedResultDto<VetProfileDto>> GetAllVetProfilesAsync(int page, int pageSize, bool? isApproved)
    {
        var query = _context.VetProfiles.AsQueryable();

        if (isApproved.HasValue)
            query = query.Where(vp => vp.IsApproved == isApproved.Value);

        var totalCount = await query.CountAsync();

        var items = (await query
            .OrderByDescending(vp => vp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync())
            .Select(MapToProfileDto)
            .ToList();

        return new PagedResultDto<VetProfileDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Result<VetProfileDto>> ApproveVetAsync(int vetProfileId)
    {
        var profile = await _context.VetProfiles.FindAsync(vetProfileId);
        if (profile is null)
            return Result<VetProfileDto>.Failure("Vet profile not found.");

        profile.Approve();
        await _context.SaveChangesAsync();

        return Result<VetProfileDto>.Success(MapToProfileDto(profile));
    }

    public async Task<Result<VetProfileDto>> RejectVetAsync(int vetProfileId)
    {
        var profile = await _context.VetProfiles.FindAsync(vetProfileId);
        if (profile is null)
            return Result<VetProfileDto>.Failure("Vet profile not found.");

        profile.Reject();
        await _context.SaveChangesAsync();

        return Result<VetProfileDto>.Success(MapToProfileDto(profile));
    }

    // ── Availability ─────────────────────────────────────────────────

    public async Task<IEnumerable<VetAvailabilityDto>> GetAvailabilityAsync(string vetUserId)
    {
        var profile = await _context.VetProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == vetUserId);

        if (profile is null)
            return Enumerable.Empty<VetAvailabilityDto>();

        var availability = await _context.VetAvailability
            .Where(va => va.VetProfileId == profile.Id)
            .OrderBy(va => va.DayOfWeek)
            .ToListAsync();

        return availability.Select(MapToAvailabilityDto);
    }

    public async Task<Result<bool>> UpdateAvailabilityAsync(string vetUserId, List<VetAvailabilityDto> schedule)
    {
        var profile = await _context.VetProfiles
            .FirstOrDefaultAsync(vp => vp.UserId == vetUserId);

        if (profile is null)
            return Result<bool>.Failure("Vet profile not found.");

        // Validate no duplicate days in the incoming schedule
        var duplicateDays = schedule
            .GroupBy(e => e.DayOfWeek)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        if (duplicateDays.Any())
            return Result<bool>.Failure($"Duplicate availability entries for: {string.Join(", ", duplicateDays)}");

        // Remove existing availability entries
        var existingAvailability = await _context.VetAvailability
            .Where(va => va.VetProfileId == profile.Id)
            .ToListAsync();

        _context.VetAvailability.RemoveRange(existingAvailability);

        // Add new entries
        foreach (var entry in schedule)
        {
            var availability = VetAvailability.Create(profile.Id, entry.DayOfWeek, entry.StartTime, entry.EndTime);
            _context.VetAvailability.Add(availability);
        }

        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    // ── Mappers ──────────────────────────────────────────────────────

    private static VetProfileDto MapToProfileDto(VetProfile vp) => new()
    {
        Id = vp.Id,
        UserId = vp.UserId,
        FullName = vp.FullName,
        Clinic = vp.Clinic,
        Specialty = vp.Specialty,
        Bio = vp.Bio,
        ServicesOffered = vp.ServicesOffered,
        IsAvailable = vp.IsAvailable,
        IsApproved = vp.IsApproved,
        CreatedAt = vp.CreatedAt,
        UpdatedAt = vp.UpdatedAt
    };

    private static VetAssignmentDto MapToAssignmentDto(VetAssignment va) => new()
    {
        Id = va.Id,
        PetId = va.PetId,
        VetProfileId = va.VetProfileId,
        RequestedByUserId = va.RequestedByUserId,
        Status = va.Status,
        RejectionReason = va.RejectionReason,
        CreatedAt = va.CreatedAt,
        UpdatedAt = va.UpdatedAt,
        PetName = va.Pet?.Name ?? string.Empty,
        VetFullName = va.VetProfile?.FullName ?? string.Empty,
        VetClinic = va.VetProfile?.Clinic
    };

    private static VetAvailabilityDto MapToAvailabilityDto(VetAvailability va) => new()
    {
        Id = va.Id,
        VetProfileId = va.VetProfileId,
        DayOfWeek = va.DayOfWeek,
        StartTime = va.StartTime,
        EndTime = va.EndTime,
        IsAvailable = va.IsAvailable
    };
}
