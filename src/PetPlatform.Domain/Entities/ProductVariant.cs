using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class ProductVariant
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string? Size { get; private set; }
    public string? Color { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal PriceMultiplier { get; private set; }
    [System.ComponentModel.DataAnnotations.ConcurrencyCheck]
    public int StockQuantity { get; private set; }
    public string? Sku { get; private set; }

    public Product Product { get; private set; } = null!;

    private ProductVariant() { } // EF Core

    public static ProductVariant Create(int productId, decimal priceMultiplier, int stockQuantity,
                                        string? size = null, string? color = null,
                                        decimal? weight = null, string? sku = null)
    {
        Guard.Against.Zero(productId, nameof(productId));

        return new ProductVariant
        {
            ProductId = productId,
            PriceMultiplier = priceMultiplier,
            StockQuantity = stockQuantity,
            Size = size,
            Color = color,
            Weight = weight,
            Sku = sku
        };
    }

    public decimal ComputePrice()
    {
        return Product.BasePrice * PriceMultiplier;
    }

    public void UpdateStock(int quantity)
    {
        StockQuantity = quantity;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity > StockQuantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {StockQuantity}, Requested: {quantity}");

        StockQuantity -= quantity;
    }

    public void SetSku(string sku)
    {
        Guard.Against.NullOrWhiteSpace(sku, nameof(sku));
        Sku = sku;
    }
}
