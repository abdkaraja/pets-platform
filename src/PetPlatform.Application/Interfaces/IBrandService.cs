using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface IBrandService
{
    Task<List<BrandDto>> GetBrandsWithProductCountsAsync();
}
