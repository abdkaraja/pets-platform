using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class CartItem
{
    public int Id { get; private set; }
    public int CartId { get; private set; }
    public int ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal LockedPrice { get; private set; }
    public DateTime AddedAt { get; private set; }

    public Cart Cart { get; private set; } = null!;
    public ProductVariant ProductVariant { get; private set; } = null!;

    private CartItem() { } // EF Core

    public static CartItem Create(int cartId, int productVariantId, int quantity, decimal lockedPrice)
    {
        return new CartItem
        {
            CartId = cartId,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            LockedPrice = lockedPrice,
            AddedAt = DateTime.UtcNow
        };
    }

    public void UpdateQuantity(int quantity)
    {
        Quantity = quantity;
    }
}
