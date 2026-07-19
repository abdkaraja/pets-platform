using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface IInventoryService
{
    Task<IEnumerable<ProductVariantDto>> GetAllVariantsAsync(string? searchTerm = null, string? stockFilter = null);
    Task<Result<ProductVariantDto>> UpdateStockAsync(int variantId, int newQuantity);
}
