using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Application.Services;

public class BrandService : IBrandService
{
    private readonly IApplicationDbContext _context;

    public BrandService(IApplicationDbContext context)
    {
        _context = Guard.Against.Null(context, nameof(context));
    }

    public async Task<List<BrandDto>> GetBrandsWithProductCountsAsync()
    {
        return await _context.Brands
            .Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name,
                ProductCount = b.Products.Count(p => p.IsActive)
            })
            .ToListAsync();
    }
}
