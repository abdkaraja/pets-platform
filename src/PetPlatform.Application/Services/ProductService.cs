using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Application.Services;

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<AdminProductDto> _validator;

    public ProductService(IApplicationDbContext context, IValidator<AdminProductDto> validator)
    {
        _context = Guard.Against.Null(context, nameof(context));
        _validator = Guard.Against.Null(validator, nameof(validator));
    }

    public async Task<PagedResultDto<ProductDto>> GetFilteredProductsAsync(ProductFilterDto filter)
    {
        Guard.Against.Null(filter, nameof(filter));

        var query = _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .AsQueryable();

        // Composable filter chain (RESEARCH Pattern 7)
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(p => p.Name.Contains(filter.SearchTerm) || (p.Description != null && p.Description.Contains(filter.SearchTerm)));

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(filter.PetType))
            query = query.Where(p => p.PetType == filter.PetType);

        if (filter.BrandId.HasValue)
            query = query.Where(p => p.BrandId == filter.BrandId.Value);

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);

        // Sorting
        query = filter.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "name" => query.OrderBy(p => p.Name),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return new PagedResultDto<ProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product is null ? null : MapToDto(product);
    }

    public async Task<Result<ProductDto>> CreateAsync(AdminProductDto dto)
    {
        Guard.Against.Null(dto, nameof(dto));

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<ProductDto>.Failure(errors);
        }

        var product = Product.Create(dto.Name, dto.BasePrice, dto.CategoryId, dto.BrandId, dto.PetType, dto.Description);

        if (!string.IsNullOrEmpty(dto.ImagePath))
            product.ImagePath = dto.ImagePath;

        foreach (var variantDto in dto.Variants)
        {
            var variant = ProductVariant.Create(
                product.Id,
                variantDto.PriceMultiplier,
                variantDto.StockQuantity,
                variantDto.Size,
                variantDto.Color,
                variantDto.Weight,
                variantDto.Sku);
            product.Variants.Add(variant);
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Result<ProductDto>.Success(await GetByIdAsync(product.Id) ?? MapToDto(product));
    }

    public async Task<Result<ProductDto>> UpdateAsync(int id, AdminProductDto dto)
    {
        Guard.Against.Null(dto, nameof(dto));

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<ProductDto>.Failure(errors);
        }

        var product = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return Result<ProductDto>.Failure("Product not found.");

        product.UpdateDetails(dto.Name, dto.Description, dto.BasePrice, dto.CategoryId, dto.BrandId, dto.PetType);

        if (!string.IsNullOrEmpty(dto.ImagePath))
            product.ImagePath = dto.ImagePath;

        // Sync variants: add new, update existing, remove deleted
        var existingVariantIds = dto.Variants.Where(v => v.Id > 0).Select(v => v.Id).ToHashSet();
        var variantsToRemove = product.Variants.Where(v => !existingVariantIds.Contains(v.Id)).ToList();
        foreach (var variant in variantsToRemove)
            product.Variants.Remove(variant);

        foreach (var variantDto in dto.Variants)
        {
            if (variantDto.Id > 0)
            {
                var existing = product.Variants.FirstOrDefault(v => v.Id == variantDto.Id);
                if (existing is not null)
                {
                    existing.UpdateStock(variantDto.StockQuantity);
                    if (variantDto.Sku is not null)
                        existing.SetSku(variantDto.Sku);
                }
            }
            else
            {
                var variant = ProductVariant.Create(
                    product.Id,
                    variantDto.PriceMultiplier,
                    variantDto.StockQuantity,
                    variantDto.Size,
                    variantDto.Color,
                    variantDto.Weight,
                    variantDto.Sku);
                product.Variants.Add(variant);
            }
        }

        await _context.SaveChangesAsync();

        return Result<ProductDto>.Success(await GetByIdAsync(id) ?? MapToDto(product));
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null)
            return Result<bool>.Failure("Product not found.");

        product.Deactivate();
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<IEnumerable<ProductDto>> GetAllAdminAsync(string? searchTerm = null, int? categoryId = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(p => p.Name.Contains(searchTerm));

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    private static ProductDto MapToDto(Product product)
    {
        var variants = product.Variants.ToList();
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            ImagePath = product.ImagePath,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name ?? string.Empty,
            PetType = product.PetType,
            IsActive = product.IsActive,
            MinVariantPrice = variants.Any() ? variants.Min(v => product.BasePrice * v.PriceMultiplier) : product.BasePrice,
            MaxVariantPrice = variants.Any() ? variants.Max(v => product.BasePrice * v.PriceMultiplier) : product.BasePrice,
            TotalStock = variants.Sum(v => v.StockQuantity),
            CreatedAt = product.CreatedAt
        };
    }
}
