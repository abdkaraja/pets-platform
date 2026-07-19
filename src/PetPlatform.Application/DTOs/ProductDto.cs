namespace PetPlatform.Application.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImagePath { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string PetType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public decimal MinVariantPrice { get; set; }
    public decimal MaxVariantPrice { get; set; }
    public int TotalStock { get; set; }
    public DateTime CreatedAt { get; set; }
}
