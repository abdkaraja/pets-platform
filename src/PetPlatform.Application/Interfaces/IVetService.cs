using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface IVetService
{
    // Profile management (D-10)
    Task<VetProfileDto?> GetProfileByUserIdAsync(string userId);
    Task<Result<VetProfileDto>> CreateProfileAsync(CreateVetProfileDto dto, string userId);
    Task<Result<VetProfileDto>> UpdateProfileAsync(int id, UpdateVetProfileDto dto, string userId);

    // Vet discovery (D-02)
    Task<PagedResultDto<VetProfileDto>> SearchVetsAsync(VetSearchFilterDto filter);

    // Assignment workflow (D-01, D-03)
    Task<Result<VetAssignmentDto>> RequestAssignmentAsync(int petId, int vetProfileId, string ownerUserId);
    Task<Result<VetAssignmentDto>> AcceptAssignmentAsync(int assignmentId, string vetUserId);
    Task<Result<VetAssignmentDto>> RejectAssignmentAsync(int assignmentId, string vetUserId, string? reason);
    Task<IEnumerable<VetAssignmentDto>> GetPendingRequestsAsync(string vetUserId);
    Task<IEnumerable<VetAssignmentDto>> GetAcceptedAssignmentsAsync(string vetUserId);
    Task<VetAssignmentDto?> GetActiveAssignmentAsync(int petId, string vetUserId);
    Task<VetAssignmentDto?> GetAssignmentByIdAsync(int assignmentId, string vetUserId);
    Task<IEnumerable<VetAssignmentDto>> GetAssignmentsForPetAsync(int petId);

    // Admin (D-13, D-14)
    Task<PagedResultDto<VetProfileDto>> GetAllVetProfilesAsync(int page, int pageSize, bool? isApproved);
    Task<Result<VetProfileDto>> ApproveVetAsync(int vetProfileId);
    Task<Result<VetProfileDto>> RejectVetAsync(int vetProfileId);

    // Availability (D-10)
    Task<IEnumerable<VetAvailabilityDto>> GetAvailabilityAsync(string vetUserId);
    Task<Result<bool>> UpdateAvailabilityAsync(string vetUserId, List<VetAvailabilityDto> schedule);
}
