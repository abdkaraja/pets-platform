using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public string? ImagePath { get; set; } // settable for file upload convenience
    public int CategoryId { get; private set; }
    public int BrandId { get; private set; }
    public string PetType { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Category Category { get; private set; } = null!;
    public Brand Brand { get; private set; } = null!;
    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();

    private Product() { } // EF Core

    public static Product Create(string name, decimal basePrice, int categoryId, int brandId,
                                 string petType, string? description = null)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NegativeOrZero(basePrice, nameof(basePrice));
        Guard.Against.NullOrWhiteSpace(petType, nameof(petType));

        return new Product
        {
            Name = name,
            Description = description,
            BasePrice = basePrice,
            CategoryId = categoryId,
            BrandId = brandId,
            PetType = petType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string name, string? description, decimal basePrice, int categoryId,
                              int brandId, string petType)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NegativeOrZero(basePrice, nameof(basePrice));
        Guard.Against.NullOrWhiteSpace(petType, nameof(petType));

        Name = name;
        Description = description;
        BasePrice = basePrice;
        CategoryId = categoryId;
        BrandId = brandId;
        PetType = petType;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
