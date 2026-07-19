using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface ICartService
{
    Task<CartDto?> GetCartAsync(string userId);
    Task<Result<CartDto>> AddToCartAsync(string userId, AddToCartDto dto);
    Task<Result<CartDto>> UpdateQuantityAsync(string userId, UpdateCartQuantityDto dto);
    Task<Result<bool>> RemoveItemAsync(string userId, int cartItemId);
    Task<Result<bool>> ClearCartAsync(string userId);
}
