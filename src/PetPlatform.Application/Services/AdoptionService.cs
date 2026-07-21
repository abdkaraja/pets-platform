using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.Services;

public class AdoptionService : IAdoptionService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateListingDto> _createListingValidator;
    private readonly IValidator<UpdateListingDto> _updateListingValidator;
    private readonly IValidator<CreateApplicationDto> _createApplicationValidator;
    private readonly IValidator<ReviewApplicationDto> _reviewApplicationValidator;

    public AdoptionService(
        IApplicationDbContext context,
        IValidator<CreateListingDto> createListingValidator,
        IValidator<UpdateListingDto> updateListingValidator,
        IValidator<CreateApplicationDto> createApplicationValidator,
        IValidator<ReviewApplicationDto> reviewApplicationValidator)
    {
        _context = Guard.Against.Null(context, nameof(context));
        _createListingValidator = Guard.Against.Null(createListingValidator, nameof(createListingValidator));
        _updateListingValidator = Guard.Against.Null(updateListingValidator, nameof(updateListingValidator));
        _createApplicationValidator = Guard.Against.Null(createApplicationValidator, nameof(createApplicationValidator));
        _reviewApplicationValidator = Guard.Against.Null(reviewApplicationValidator, nameof(reviewApplicationValidator));
    }

    public async Task<PagedResultDto<AdoptionListingDto>> GetActiveListingsAsync(AdoptionListingFilterDto filter)
    {
        var query = _context.AdoptionListings
            .Where(al => al.Status == ListingStatus.Active)
            .Include(al => al.Pet)
            .AsQueryable();

        if (filter.Species.HasValue)
            query = query.Where(al => al.Pet.Species == filter.Species.Value);

        if (!string.IsNullOrWhiteSpace(filter.Breed))
            query = query.Where(al => al.Pet.Breed != null && al.Pet.Breed.Contains(filter.Breed));

        if (filter.MinAge.HasValue)
            query = query.Where(al => al.Pet.Age >= filter.MinAge.Value);

        if (filter.MaxAge.HasValue)
            query = query.Where(al => al.Pet.Age <= filter.MaxAge.Value);

        if (!string.IsNullOrWhiteSpace(filter.Location))
            query = query.Where(al => al.Location.Contains(filter.Location));

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(al => al.Title.Contains(filter.SearchTerm) ||
                                       (al.Description != null && al.Description.Contains(filter.SearchTerm)));

        var totalCount = await query.CountAsync();

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 50);

        var items = await query
            .OrderByDescending(al => al.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(al => MapToListingDto(al))
            .ToListAsync();

        return new PagedResultDto<AdoptionListingDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdoptionListingDto?> GetListingByIdAsync(int id)
    {
        var listing = await _context.AdoptionListings
            .Include(al => al.Pet)
            .FirstOrDefaultAsync(al => al.Id == id);

        return listing is null ? null : MapToListingDto(listing);
    }

    public async Task<Result<AdoptionApplicationDto>> SubmitApplicationAsync(CreateApplicationDto dto, string applicantUserId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(applicantUserId, nameof(applicantUserId));

        var validation = await _createApplicationValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<AdoptionApplicationDto>.Failure(errors);
        }

        var listing = await _context.AdoptionListings.FindAsync(dto.ListingId);
        if (listing is null)
            return Result<AdoptionApplicationDto>.Failure("Listing not found.");

        if (listing.Status != ListingStatus.Active)
            return Result<AdoptionApplicationDto>.Failure("This listing is no longer accepting applications.");

        var existingApplication = await _context.AdoptionApplications
            .FirstOrDefaultAsync(aa => aa.ListingId == dto.ListingId && aa.ApplicantUserId == applicantUserId);

        if (existingApplication is not null)
            return Result<AdoptionApplicationDto>.Failure("You have already applied for this pet.");

        var application = AdoptionApplication.Create(
            dto.ListingId,
            applicantUserId,
            dto.Message,
            dto.HousingType,
            dto.HasYard,
            dto.NumberOfOccupants,
            dto.HasChildren,
            dto.PreviousPets,
            dto.CurrentPets,
            dto.ExperienceLevel);
        _context.AdoptionApplications.Add(application);
        await _context.SaveChangesAsync();

        return Result<AdoptionApplicationDto>.Success(MapToApplicationDto(application));
    }

    public async Task<IEnumerable<AdoptionApplicationDto>> GetMyApplicationsAsync(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var applications = await _context.AdoptionApplications
            .Where(aa => aa.ApplicantUserId == userId)
            .Include(aa => aa.Listing)
                .ThenInclude(l => l.Pet)
            .Include(aa => aa.StatusHistory)
            .OrderByDescending(aa => aa.CreatedAt)
            .ToListAsync();

        return applications.Select(a => MapToApplicationDto(a));
    }

    public async Task<AdoptionApplicationDto?> GetApplicationByIdAsync(int applicationId, string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var application = await _context.AdoptionApplications
            .Where(aa => aa.Id == applicationId && aa.ApplicantUserId == userId)
            .Include(aa => aa.Listing)
                .ThenInclude(l => l.Pet)
            .Include(aa => aa.StatusHistory)
            .FirstOrDefaultAsync();

        return application is null ? null : MapToApplicationDto(application);
    }

    public async Task<Result<bool>> WithdrawApplicationAsync(int applicationId, string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var application = await _context.AdoptionApplications.FindAsync(applicationId);
        if (application is null)
            return Result<bool>.Failure("Application not found.");

        if (application.ApplicantUserId != userId)
            return Result<bool>.Failure("You are not authorized to withdraw this application.");

        if (application.Status != ApplicationStatus.Submitted)
            return Result<bool>.Failure("Only submitted applications can be withdrawn.");

        try
        {
            application.UpdateStatus(ApplicationStatus.Withdrawn);
        }
        catch (InvalidOperationException ex)
        {
            return Result<bool>.Failure(ex.Message);
        }

        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<IEnumerable<AdoptionListingDto>> GetShelterListingsAsync(string shelterUserId)
    {
        Guard.Against.NullOrWhiteSpace(shelterUserId, nameof(shelterUserId));

        var listings = await _context.AdoptionListings
            .Where(al => al.ShelterUserId == shelterUserId)
            .Include(al => al.Pet)
            .Include(al => al.Applications)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync();

        return listings.Select(l => MapToListingDto(l));
    }

    public async Task<Result<AdoptionListingDto>> CreateListingAsync(CreateListingDto dto, string shelterUserId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(shelterUserId, nameof(shelterUserId));

        var validation = await _createListingValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<AdoptionListingDto>.Failure(errors);
        }

        var pet = await _context.Pets.FindAsync(dto.PetId);
        if (pet is null)
            return Result<AdoptionListingDto>.Failure("Pet not found.");

        var existingActive = await _context.AdoptionListings
            .AnyAsync(al => al.PetId == dto.PetId && al.Status == ListingStatus.Active);

        if (existingActive)
            return Result<AdoptionListingDto>.Failure("This pet already has an active listing.");

        var listing = AdoptionListing.Create(dto.PetId, shelterUserId, dto.Title, dto.Location, dto.Description);
        _context.AdoptionListings.Add(listing);
        await _context.SaveChangesAsync();

        var created = await _context.AdoptionListings
            .Include(al => al.Pet)
            .FirstAsync(al => al.Id == listing.Id);

        return Result<AdoptionListingDto>.Success(MapToListingDto(created));
    }

    public async Task<Result<AdoptionListingDto>> UpdateListingAsync(int id, UpdateListingDto dto, string shelterUserId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(shelterUserId, nameof(shelterUserId));

        var validation = await _updateListingValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<AdoptionListingDto>.Failure(errors);
        }

        var listing = await _context.AdoptionListings.FindAsync(id);
        if (listing is null)
            return Result<AdoptionListingDto>.Failure("Listing not found.");

        if (listing.ShelterUserId != shelterUserId)
            return Result<AdoptionListingDto>.Failure("You are not authorized to modify this listing.");

        listing.UpdateDetails(dto.Title, dto.Location, dto.Description);
        await _context.SaveChangesAsync();

        var updated = await _context.AdoptionListings
            .Include(al => al.Pet)
            .FirstAsync(al => al.Id == listing.Id);

        return Result<AdoptionListingDto>.Success(MapToListingDto(updated));
    }

    public async Task<Result<bool>> CloseListingAsync(int id, string shelterUserId)
    {
        Guard.Against.NullOrWhiteSpace(shelterUserId, nameof(shelterUserId));

        var listing = await _context.AdoptionListings.FindAsync(id);
        if (listing is null)
            return Result<bool>.Failure("Listing not found.");

        if (listing.ShelterUserId != shelterUserId)
            return Result<bool>.Failure("You are not authorized to modify this listing.");

        if (listing.Status != ListingStatus.Active)
            return Result<bool>.Failure("Only active listings can be closed.");

        listing.UpdateStatus(ListingStatus.Closed);
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<IEnumerable<AdoptionApplicationDto>> GetApplicationsForListingAsync(int listingId, string shelterUserId)
    {
        Guard.Against.NullOrWhiteSpace(shelterUserId, nameof(shelterUserId));

        var listing = await _context.AdoptionListings.FindAsync(listingId);
        if (listing is null)
            return Enumerable.Empty<AdoptionApplicationDto>();

        if (listing.ShelterUserId != shelterUserId)
            return Enumerable.Empty<AdoptionApplicationDto>();

        var applications = await _context.AdoptionApplications
            .Where(aa => aa.ListingId == listingId)
            .Include(aa => aa.StatusHistory)
            .OrderByDescending(aa => aa.CreatedAt)
            .ToListAsync();

        return applications.Select(a => MapToApplicationDto(a));
    }

    public async Task<AdoptionApplicationDto?> GetApplicationForReviewAsync(int applicationId, string shelterUserId)
    {
        Guard.Against.NullOrWhiteSpace(shelterUserId, nameof(shelterUserId));

        var application = await _context.AdoptionApplications
            .Where(aa => aa.Id == applicationId)
            .Include(aa => aa.Listing)
                .ThenInclude(l => l.Pet)
            .Include(aa => aa.StatusHistory)
            .FirstOrDefaultAsync();

        if (application is null)
            return null;

        if (application.Listing.ShelterUserId != shelterUserId)
            return null;

        return MapToApplicationDto(application);
    }

    public async Task<Result<AdoptionApplicationDto>> ReviewApplicationAsync(int applicationId, ReviewApplicationDto dto, string reviewerUserId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(reviewerUserId, nameof(reviewerUserId));

        var validation = await _reviewApplicationValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<AdoptionApplicationDto>.Failure(errors);
        }

        var application = await _context.AdoptionApplications.FindAsync(applicationId);
        if (application is null)
            return Result<AdoptionApplicationDto>.Failure("Application not found.");

        var listing = await _context.AdoptionListings.FindAsync(application.ListingId);
        if (listing is null)
            return Result<AdoptionApplicationDto>.Failure("Listing not found.");

        if (listing.ShelterUserId != reviewerUserId)
            return Result<AdoptionApplicationDto>.Failure("You are not authorized to review applications for this listing.");

        if (application.Status != ApplicationStatus.Submitted && application.Status != ApplicationStatus.UnderReview)
            return Result<AdoptionApplicationDto>.Failure("This application has already been decided.");

        var allowedShelterStatuses = new[] { ApplicationStatus.UnderReview, ApplicationStatus.Approved, ApplicationStatus.Rejected };
        if (!allowedShelterStatuses.Contains(dto.Status))
            return Result<AdoptionApplicationDto>.Failure("Invalid review status.");

        try
        {
            if (dto.Status == ApplicationStatus.UnderReview)
            {
                application.UpdateStatus(ApplicationStatus.UnderReview);
            }
            else
            {
                application.Review(reviewerUserId, dto.Status, dto.ReviewNotes);

                if (dto.Status == ApplicationStatus.Approved)
                {
                    listing.UpdateStatus(ListingStatus.Adopted);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            return Result<AdoptionApplicationDto>.Failure(ex.Message);
        }

        await _context.SaveChangesAsync();

        var updated = await _context.AdoptionApplications
            .Include(aa => aa.Listing)
                .ThenInclude(l => l.Pet)
            .Include(aa => aa.StatusHistory)
            .FirstAsync(aa => aa.Id == application.Id);

        return Result<AdoptionApplicationDto>.Success(MapToApplicationDto(updated));
    }

    private static AdoptionListingDto MapToListingDto(AdoptionListing listing) => new()
    {
        Id = listing.Id,
        PetId = listing.PetId,
        ShelterUserId = listing.ShelterUserId,
        Title = listing.Title,
        Description = listing.Description,
        Location = listing.Location,
        Status = listing.Status,
        CreatedAt = listing.CreatedAt,
        UpdatedAt = listing.UpdatedAt,
        PetName = listing.Pet?.Name ?? string.Empty,
        PetSpecies = listing.Pet?.Species ?? default,
        PetBreed = listing.Pet?.Breed,
        PetAge = listing.Pet?.Age ?? 0,
        PetPhotoPath = listing.Pet?.PhotoPath,
        ApplicationCount = listing.Applications?.Count ?? 0
    };

    private static AdoptionApplicationDto MapToApplicationDto(AdoptionApplication application) => new()
    {
        Id = application.Id,
        ListingId = application.ListingId,
        ApplicantUserId = application.ApplicantUserId,
        Message = application.Message,
        HousingType = application.HousingType,
        HasYard = application.HasYard,
        NumberOfOccupants = application.NumberOfOccupants,
        HasChildren = application.HasChildren,
        PreviousPets = application.PreviousPets,
        CurrentPets = application.CurrentPets,
        ExperienceLevel = application.ExperienceLevel,
        Status = application.Status,
        ReviewedByUserId = application.ReviewedByUserId,
        ReviewNotes = application.ReviewNotes,
        CreatedAt = application.CreatedAt,
        UpdatedAt = application.UpdatedAt,
        ListingTitle = application.Listing?.Title ?? string.Empty,
        PetName = application.Listing?.Pet?.Name ?? string.Empty,
        StatusHistory = application.StatusHistory?.Select(sh => new ApplicationStatusHistoryDto
        {
            Status = sh.Status,
            ChangedAt = sh.ChangedAt
        }).ToList() ?? new List<ApplicationStatusHistoryDto>()
    };
}
