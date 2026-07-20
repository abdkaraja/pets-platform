using Microsoft.AspNetCore.Http;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface ILostPetService
{
    // Public-facing (browse/search — D-16 all users can browse)
    Task<PagedResultDto<LostPetReportDto>> SearchReportsAsync(LostPetReportFilterDto filter);
    Task<LostPetReportDto?> GetReportByIdAsync(int id);

    // Reporter-facing
    Task<Result<LostPetReportDto>> CreateReportAsync(CreateLostPetReportDto dto, string reporterUserId, List<IFormFile>? photos = null);
    Task<Result<LostPetReportDto>> UpdateReportAsync(int id, UpdateLostPetReportDto dto, string reporterUserId, List<IFormFile>? photos = null);
    Task<Result<bool>> ResolveReportAsync(int id, string reporterUserId);
    Task<IEnumerable<LostPetReportDto>> GetMyReportsAsync(string reporterUserId);

    // Notifications (D-05, D-07)
    Task<IEnumerable<MatchNotificationDto>> GetMyNotificationsAsync(string userId);
    Task<Result<bool>> MarkNotificationReadAsync(int notificationId, string userId);
}
