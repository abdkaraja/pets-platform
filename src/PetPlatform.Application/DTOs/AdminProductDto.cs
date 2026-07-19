namespace PetPlatform.Application.DTOs;

public class AdminProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public int CategoryId { get; set; }
    public int BrandId { get; set; }
    public string PetType { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public ICollection<AdminProductVariantDto> Variants { get; set; } = new List<AdminProductVariantDto>();
    public bool IsActive { get; set; } = true;
}

public class AdminProductVariantDto
{
    public int Id { get; set; } // 0 for new variants
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal? Weight { get; set; }
    public decimal PriceMultiplier { get; set; } = 1.0m;
    public int StockQuantity { get; set; }
    public string? Sku { get; set; }
}
