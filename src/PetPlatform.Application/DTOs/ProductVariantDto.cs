namespace PetPlatform.Application.DTOs;

public class ProductVariantDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal? Weight { get; set; }
    public decimal PriceMultiplier { get; set; }
    public decimal ComputedPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Sku { get; set; }
}
