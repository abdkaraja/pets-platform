using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;
using static PetPlatform.Application.Common.VariantDescriptionBuilder;

namespace PetPlatform.Application.Services;

public class CartService : ICartService
{
    private readonly IApplicationDbContext _context;

    public CartService(IApplicationDbContext context)
    {
        _context = Guard.Against.Null(context, nameof(context));
    }

    public async Task<CartDto?> GetCartAsync(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.ProductVariant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        return cart is null ? null : MapToDto(cart);
    }

    public async Task<Result<CartDto>> AddToCartAsync(string userId, AddToCartDto dto)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.Null(dto, nameof(dto));

        // D-10: Get-or-create cart
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
        {
            cart = Cart.Create(userId);
            _context.Carts.Add(cart);
        }

        // D-11: Validate variant exists and has stock
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == dto.ProductVariantId);

        if (variant is null)
            return Result<CartDto>.Failure("Product variant not found.");

        if (variant.StockQuantity <= 0)
            return Result<CartDto>.Failure("This item is currently out of stock.");

        // Check existing item in cart
        var existingItem = cart.Items.FirstOrDefault(ci => ci.ProductVariantId == dto.ProductVariantId);

        if (existingItem is not null)
        {
            // D-12: Cap quantity at stock level
            var newQuantity = existingItem.Quantity + dto.Quantity;
            if (newQuantity > variant.StockQuantity)
                return Result<CartDto>.Failure($"Only {variant.StockQuantity} items available in stock.");

            existingItem.UpdateQuantity(newQuantity);
        }
        else
        {
            // D-12: Validate requested quantity
            if (dto.Quantity > variant.StockQuantity)
                return Result<CartDto>.Failure($"Only {variant.StockQuantity} items available in stock.");

            // D-13: Lock price at add-to-cart time
            var lockedPrice = variant.ComputePrice();
            var cartItem = CartItem.Create(cart.Id, dto.ProductVariantId, dto.Quantity, lockedPrice);
            cart.Items.Add(cartItem);
        }

        cart.Touch();
        await _context.SaveChangesAsync();

        return Result<CartDto>.Success(await GetCartAsync(userId) ?? MapToDto(cart));
    }

    public async Task<Result<CartDto>> UpdateQuantityAsync(string userId, UpdateCartQuantityDto dto)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.Null(dto, nameof(dto));

        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .Include(ci => ci.ProductVariant)
            .FirstOrDefaultAsync(ci => ci.Id == dto.CartItemId && ci.Cart.UserId == userId);

        if (cartItem is null)
            return Result<CartDto>.Failure("Cart item not found.");

        // D-12: Quantity must be at least 1 (zero/quantity items should be removed instead)
        if (dto.Quantity <= 0)
            return Result<CartDto>.Failure("Quantity must be at least 1. Use Remove to delete items.");

        // D-12: Validate new quantity
        if (dto.Quantity > cartItem.ProductVariant.StockQuantity)
            return Result<CartDto>.Failure($"Only {cartItem.ProductVariant.StockQuantity} items available in stock.");

        cartItem.UpdateQuantity(dto.Quantity);
        await _context.SaveChangesAsync();

        return Result<CartDto>.Success(await GetCartAsync(userId) ?? MapToDto(cartItem.Cart));
    }

    public async Task<Result<bool>> RemoveItemAsync(string userId, int cartItemId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var cartItem = await _context.CartItems
            .Include(ci => ci.Cart)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

        if (cartItem is null)
            return Result<bool>.Failure("Cart item not found.");

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ClearCartAsync(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
            return Result<bool>.Success(true);

        // D-14: Clear cart
        cart.Clear();
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private static CartDto MapToDto(Cart cart)
    {
        var items = cart.Items.Select(ci => new CartItemDto
        {
            Id = ci.Id,
            ProductVariantId = ci.ProductVariantId,
            ProductName = ci.ProductVariant?.Product?.Name ?? string.Empty,
            VariantDescription = Build(ci.ProductVariant),
            ImagePath = ci.ProductVariant?.Product?.ImagePath,
            Quantity = ci.Quantity,
            LockedPrice = ci.LockedPrice
        }).ToList();

        return new CartDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            Items = items
        };
    }
}
