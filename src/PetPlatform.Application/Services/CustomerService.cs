using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _context;

    public CustomerService(IApplicationDbContext context)
    {
        _context = Guard.Against.Null(context, nameof(context));
    }

    public async Task<CustomerProfileDto> GetProfileAsync(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var profile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            // GetOrCreate pattern: create empty profile on first access
            profile = CustomerProfile.Create(userId, "New User");
            _context.CustomerProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        return MapToDto(profile);
    }

    public async Task<Result<CustomerProfileDto>> UpdateProfileAsync(string userId, UpdateCustomerProfileDto dto)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.Null(dto, nameof(dto));

        var profile = await _context.CustomerProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile is null)
        {
            profile = CustomerProfile.Create(
                userId,
                dto.FullName,
                address: dto.Address,
                phone: dto.Phone,
                city: dto.City,
                notificationPreferences: dto.NotificationPreferences);

            _context.CustomerProfiles.Add(profile);
        }
        else
        {
            profile.UpdateDetails(
                dto.FullName,
                dto.Address,
                dto.Phone,
                dto.City,
                dto.NotificationPreferences);
        }

        await _context.SaveChangesAsync();

        return Result<CustomerProfileDto>.Success(MapToDto(profile));
    }

    private static CustomerProfileDto MapToDto(CustomerProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        FullName = profile.FullName,
        Address = profile.Address,
        Phone = profile.Phone,
        City = profile.City,
        NotificationPreferences = profile.NotificationPreferences
    };
}
