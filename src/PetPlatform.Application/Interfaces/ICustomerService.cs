using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerProfileDto> GetProfileAsync(string userId);
    Task<Result<CustomerProfileDto>> UpdateProfileAsync(string userId, UpdateCustomerProfileDto dto);
}
