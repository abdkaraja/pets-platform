using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface IAdoptionService
{
    // Public/Customer-facing
    Task<PagedResultDto<AdoptionListingDto>> GetActiveListingsAsync(AdoptionListingFilterDto filter);
    Task<AdoptionListingDto?> GetListingByIdAsync(int id);
    Task<Result<AdoptionApplicationDto>> SubmitApplicationAsync(CreateApplicationDto dto, string applicantUserId);
    Task<IEnumerable<AdoptionApplicationDto>> GetMyApplicationsAsync(string userId);
    Task<Result<bool>> WithdrawApplicationAsync(int applicationId, string userId);

    // Shelter-facing
    Task<IEnumerable<AdoptionListingDto>> GetShelterListingsAsync(string shelterUserId);
    Task<Result<AdoptionListingDto>> CreateListingAsync(CreateListingDto dto, string shelterUserId);
    Task<Result<AdoptionListingDto>> UpdateListingAsync(int id, UpdateListingDto dto, string shelterUserId);
    Task<Result<bool>> CloseListingAsync(int id, string shelterUserId);
    Task<IEnumerable<AdoptionApplicationDto>> GetApplicationsForListingAsync(int listingId, string shelterUserId);
    Task<Result<AdoptionApplicationDto>> ReviewApplicationAsync(int applicationId, ReviewApplicationDto dto, string reviewerUserId);
}
