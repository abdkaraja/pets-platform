using Ardalis.GuardClauses;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class Payment
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public string StripePaymentIntentId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Order Order { get; private set; } = null!;

    private Payment() { } // EF Core

    public static Payment Create(int orderId, string stripePaymentIntentId, decimal amount)
    {
        Guard.Against.NullOrWhiteSpace(stripePaymentIntentId, nameof(stripePaymentIntentId));

        return new Payment
        {
            OrderId = orderId,
            StripePaymentIntentId = stripePaymentIntentId,
            Amount = amount,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
}
