namespace PetPlatform.Application.DTOs;

public class CartItemDto
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantDescription { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public int Quantity { get; set; }
    public decimal LockedPrice { get; set; }
    public decimal LineTotal => LockedPrice * Quantity;
}
