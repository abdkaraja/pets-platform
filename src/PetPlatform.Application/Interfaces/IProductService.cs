using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface IProductService
{
    Task<PagedResultDto<ProductDto>> GetFilteredProductsAsync(ProductFilterDto filter);
    Task<ProductDto?> GetByIdAsync(int id);
    Task<Result<ProductDto>> CreateAsync(AdminProductDto dto);
    Task<Result<ProductDto>> UpdateAsync(int id, AdminProductDto dto);
    Task<Result<bool>> DeleteAsync(int id);
    Task<IEnumerable<ProductDto>> GetAllAdminAsync(string? searchTerm = null, int? categoryId = null);
}
