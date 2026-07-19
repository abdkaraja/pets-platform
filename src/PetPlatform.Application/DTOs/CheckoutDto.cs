namespace PetPlatform.Application.DTOs;

public class CheckoutDto
{
    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public ICollection<CartItemDto> CartItems { get; set; } = new List<CartItemDto>();
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; } = 5.99m;
    public decimal TotalAmount => Subtotal + ShippingCost;
}
