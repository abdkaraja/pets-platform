using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Infrastructure.Services;

public class LostPetService : ILostPetService
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public LostPetService(IApplicationDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<PagedResultDto<LostPetReportDto>> SearchReportsAsync(LostPetReportFilterDto filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _context.LostPetReports
            .Include(r => r.Pet)
            .Include(r => r.Photos)
            .Where(r => r.Status == LostPetReportStatus.Open)
            .AsQueryable();

        if (filter.ReportType.HasValue)
            query = query.Where(r => r.ReportType == filter.ReportType.Value);

        if (filter.Species.HasValue)
            query = query.Where(r => r.Species == filter.Species.Value);

        if (!string.IsNullOrWhiteSpace(filter.Breed))
            query = query.Where(r => r.Breed != null && r.Breed.Contains(filter.Breed));

        if (!string.IsNullOrWhiteSpace(filter.Color))
            query = query.Where(r => r.Color.Contains(filter.Color));

        if (!string.IsNullOrWhiteSpace(filter.Location))
            query = query.Where(r => r.Location.Contains(filter.Location));

        if (filter.DateFrom.HasValue)
            query = query.Where(r => r.DateReported >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(r => r.DateReported <= filter.DateTo.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(r =>
                r.Description.ToLower().Contains(term) ||
                r.Color.ToLower().Contains(term) ||
                (r.Breed != null && r.Breed.ToLower().Contains(term)) ||
                r.Location.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.DateReported)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResultDto<LostPetReportDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<LostPetReportDto?> GetReportByIdAsync(int id)
    {
        var report = await _context.LostPetReports
            .Include(r => r.Pet)
            .Include(r => r.Photos)
            .FirstOrDefaultAsync(r => r.Id == id);

        return report == null ? null : MapToDto(report);
    }

    public async Task<Result<LostPetReportDto>> CreateReportAsync(CreateLostPetReportDto dto, string reporterUserId, List<IFormFile>? photos = null)
    {
        var report = LostPetReport.Create(
            reporterUserId: reporterUserId,
            reportType: dto.ReportType,
            species: dto.Species,
            color: dto.Color,
            location: dto.Location,
            dateReported: dto.DateReported,
            description: dto.Description,
            breed: dto.Breed,
            petId: dto.PetId);

        _context.LostPetReports.Add(report);
        await _context.SaveChangesAsync();

        if (photos != null && photos.Count > 0)
        {
            foreach (var photo in photos)
            {
                var photoPath = await _fileStorage.SaveFileAsync(photo, "lost-pets");
                var reportPhoto = LostPetReportPhoto.Create(report.Id, photoPath);
                _context.LostPetReportPhotos.Add(reportPhoto);
            }
            await _context.SaveChangesAsync();
        }

        await CheckForMatchesAsync(report);
        await _context.SaveChangesAsync();

        return Result<LostPetReportDto>.Success(MapToDto(report));
    }

    public async Task<Result<LostPetReportDto>> UpdateReportAsync(int id, UpdateLostPetReportDto dto, string reporterUserId, List<IFormFile>? photos = null)
    {
        var report = await _context.LostPetReports
            .FirstOrDefaultAsync(r => r.Id == id && r.ReporterUserId == reporterUserId);

        if (report == null)
            return Result<LostPetReportDto>.Failure("Report not found or you are not authorized to edit it.");

        report.UpdateDetails(
            color: dto.Color,
            breed: dto.Breed,
            location: dto.Location,
            dateReported: dto.DateReported,
            description: dto.Description);

        if (photos != null && photos.Count > 0)
        {
            foreach (var photo in photos)
            {
                var photoPath = await _fileStorage.SaveFileAsync(photo, "lost-pets");
                var reportPhoto = LostPetReportPhoto.Create(report.Id, photoPath);
                _context.LostPetReportPhotos.Add(reportPhoto);
            }
        }

        await _context.SaveChangesAsync();
        return Result<LostPetReportDto>.Success(MapToDto(report));
    }

    public async Task<Result<bool>> ResolveReportAsync(int id, string reporterUserId)
    {
        var report = await _context.LostPetReports
            .FirstOrDefaultAsync(r => r.Id == id && r.ReporterUserId == reporterUserId);

        if (report == null)
            return Result<bool>.Failure("Report not found or you are not authorized to resolve it.");

        report.Resolve();
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<IEnumerable<LostPetReportDto>> GetMyReportsAsync(string reporterUserId)
    {
        var reports = await _context.LostPetReports
            .Include(r => r.Pet)
            .Include(r => r.Photos)
            .Where(r => r.ReporterUserId == reporterUserId)
            .OrderByDescending(r => r.DateReported)
            .ToListAsync();

        return reports.Select(MapToDto);
    }

    public async Task<IEnumerable<MatchNotificationDto>> GetMyNotificationsAsync(string userId)
    {
        var notifications = await _context.MatchNotifications
            .Include(n => n.MatchedReport)
            .Where(n => n.ReporterUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(n => new MatchNotificationDto
        {
            Id = n.Id,
            MatchedReportId = n.MatchedReportId,
            TriggeredReportId = n.TriggeredReportId,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            MatchedReportSpecies = n.MatchedReport?.Species.ToString(),
            MatchedReportLocation = n.MatchedReport?.Location,
            MatchedReportDescription = n.MatchedReport?.Description
        });
    }

    public async Task<Result<bool>> MarkNotificationReadAsync(int notificationId, string userId)
    {
        var notification = await _context.MatchNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.ReporterUserId == userId);

        if (notification == null)
            return Result<bool>.Failure("Notification not found or you are not authorized.");

        notification.MarkAsRead();
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private async Task CheckForMatchesAsync(LostPetReport newReport)
    {
        var oppositeType = newReport.ReportType == LostPetReportType.Lost
            ? LostPetReportType.Found
            : LostPetReportType.Lost;

        var candidates = await _context.LostPetReports
            .Where(r =>
                r.ReportType == oppositeType &&
                r.Status == LostPetReportStatus.Open &&
                r.Species == newReport.Species &&
                (r.Location.Contains(newReport.Location) || newReport.Location.Contains(r.Location)))
            .ToListAsync();

        var candidateIds = candidates.Select(c => c.Id).ToList();
        var existingNotificationPairs = await _context.MatchNotifications
            .Where(n =>
                candidateIds.Contains(n.MatchedReportId) || candidateIds.Contains(n.TriggeredReportId))
            .Select(n => new { n.MatchedReportId, n.TriggeredReportId })
            .ToListAsync();
        var existingSet = existingNotificationPairs
            .SelectMany(n => new[] { (n.MatchedReportId, n.TriggeredReportId), (n.TriggeredReportId, n.MatchedReportId) })
            .ToHashSet();

        foreach (var candidate in candidates)
        {
            if (!existingSet.Contains((newReport.Id, candidate.Id)) && !existingSet.Contains((candidate.Id, newReport.Id)))
            {
                var message = $"Potential match: {candidate.Species} reported as {candidate.ReportType} in {candidate.Location}.";
                var notif1 = MatchNotification.Create(newReport.Id, candidate.Id, candidate.ReporterUserId, message);
                var notif2 = MatchNotification.Create(candidate.Id, newReport.Id, newReport.ReporterUserId, message);
                _context.MatchNotifications.AddRange(notif1, notif2);
            }
        }
    }

    private static LostPetReportDto MapToDto(LostPetReport report)
    {
        return new LostPetReportDto
        {
            Id = report.Id,
            ReporterUserId = report.ReporterUserId,
            ReportType = report.ReportType,
            Species = report.Species,
            Breed = report.Breed,
            Color = report.Color,
            Location = report.Location,
            DateReported = report.DateReported,
            Description = report.Description,
            PetId = report.PetId,
            Status = report.Status,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt,
            PetName = report.Pet?.Name,
            PetPhotoPath = report.Pet?.PhotoPath,
            Photos = report.Photos.Select(p => new LostPetReportPhotoDto
            {
                Id = p.Id,
                PhotoPath = p.PhotoPath
            }).ToList()
        };
    }
}
