using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IApplicationDbContext _context;

    public InventoryService(IApplicationDbContext context)
    {
        _context = Guard.Against.Null(context, nameof(context));
    }

    public async Task<IEnumerable<ProductVariantDto>> GetAllVariantsAsync(string? searchTerm = null, string? stockFilter = null)
    {
        var query = _context.ProductVariants
            .Include(v => v.Product)
            .AsQueryable();

        // Search across Product.Name and Sku
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(v =>
                v.Product.Name.Contains(searchTerm) ||
                (v.Sku != null && v.Sku.Contains(searchTerm)));

        // Stock filter
        if (!string.IsNullOrWhiteSpace(stockFilter))
        {
            query = stockFilter.ToLowerInvariant() switch
            {
                "instock" => query.Where(v => v.StockQuantity > 0),
                "outofstock" => query.Where(v => v.StockQuantity == 0),
                "lowstock" => query.Where(v => v.StockQuantity > 0 && v.StockQuantity <= 5),
                _ => query
            };
        }

        return await query
            .OrderBy(v => v.Product.Name)
            .Select(v => new ProductVariantDto
            {
                Id = v.Id,
                ProductId = v.ProductId,
                Size = v.Size,
                Color = v.Color,
                Weight = v.Weight,
                PriceMultiplier = v.PriceMultiplier,
                ComputedPrice = v.Product.BasePrice * v.PriceMultiplier,
                StockQuantity = v.StockQuantity,
                Sku = v.Sku
            })
            .ToListAsync();
    }

    public async Task<Result<ProductVariantDto>> UpdateStockAsync(int variantId, int newQuantity)
    {
        var variant = await _context.ProductVariants
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);

        if (variant is null)
            return Result<ProductVariantDto>.Failure("Product variant not found.");

        variant.UpdateStock(newQuantity);
        await _context.SaveChangesAsync();

        return Result<ProductVariantDto>.Success(new ProductVariantDto
        {
            Id = variant.Id,
            ProductId = variant.ProductId,
            Size = variant.Size,
            Color = variant.Color,
            Weight = variant.Weight,
            PriceMultiplier = variant.PriceMultiplier,
            ComputedPrice = variant.Product.BasePrice * variant.PriceMultiplier,
            StockQuantity = variant.StockQuantity,
            Sku = variant.Sku
        });
    }
}
