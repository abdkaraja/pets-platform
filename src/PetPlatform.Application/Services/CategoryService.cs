using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreateCategoryDto> _validator;

    public CategoryService(IApplicationDbContext context, IValidator<CreateCategoryDto> validator)
    {
        _context = Guard.Against.Null(context, nameof(context));
        _validator = Guard.Against.Null(validator, nameof(validator));
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId,
                ParentName = c.Parent != null ? c.Parent.Name : null,
                ProductCount = c.Products.Count
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<CategoryDto>> GetTreeAsync()
    {
        var allCategories = await _context.Categories
            .Include(c => c.Products)
            .ToListAsync();

        var lookup = allCategories.ToLookup(c => c.ParentId);

        return BuildTree(lookup, null);
    }

    private static IEnumerable<CategoryDto> BuildTree(ILookup<int?, Category> lookup, int? parentId)
    {
        return lookup[parentId].OrderBy(c => c.Name).Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentId = c.ParentId,
            ProductCount = c.Products.Count,
            Children = BuildTree(lookup, c.Id).ToList()
        });
    }

    public async Task<CategoryDto?> GetByIdAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Parent)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null) return null;

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            ParentName = category.Parent?.Name,
            ProductCount = category.Products.Count
        };
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryDto dto)
    {
        Guard.Against.Null(dto, nameof(dto));

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<CategoryDto>.Failure(errors);
        }

        var category = Category.Create(dto.Name, dto.ParentId);
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return Result<CategoryDto>.Success(await GetByIdAsync(category.Id) ?? new CategoryDto { Id = category.Id, Name = category.Name });
    }

    public async Task<Result<CategoryDto>> UpdateAsync(int id, CreateCategoryDto dto)
    {
        Guard.Against.Null(dto, nameof(dto));

        var validation = await _validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<CategoryDto>.Failure(errors);
        }

        var category = await _context.Categories.FindAsync(id);
        if (category is null)
            return Result<CategoryDto>.Failure("Category not found.");

        // Validate no circular parent reference
        if (dto.ParentId.HasValue && dto.ParentId.Value == id)
            return Result<CategoryDto>.Failure("Category cannot be its own parent.");

        category.UpdateName(dto.Name);
        await _context.SaveChangesAsync();

        return Result<CategoryDto>.Success(await GetByIdAsync(id) ?? new CategoryDto { Id = category.Id, Name = category.Name });
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category is null)
            return Result<bool>.Failure("Category not found.");

        if (category.Products.Any())
            return Result<bool>.Failure("Cannot delete category with associated products.");

        if (category.Children.Any())
            return Result<bool>.Failure("Cannot delete category with subcategories.");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}
