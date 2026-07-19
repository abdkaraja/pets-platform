using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class OrderItem
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? VariantDescription { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public string? ImagePath { get; private set; }

    public Order Order { get; private set; } = null!;

    private OrderItem() { } // EF Core

    public static OrderItem Create(int orderId, string productName, string? variantDescription,
                                   int quantity, decimal unitPrice, string? imagePath = null)
    {
        Guard.Against.NullOrWhiteSpace(productName, nameof(productName));

        return new OrderItem
        {
            OrderId = orderId,
            ProductName = productName,
            VariantDescription = variantDescription,
            Quantity = quantity,
            UnitPrice = unitPrice,
            ImagePath = imagePath
        };
    }
}
