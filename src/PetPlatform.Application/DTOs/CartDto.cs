namespace PetPlatform.Application.DTOs;

public class CartDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ICollection<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public int ItemCount => Items.Sum(i => i.Quantity);
}
