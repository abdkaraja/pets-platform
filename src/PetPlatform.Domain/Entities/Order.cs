using Ardalis.GuardClauses;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class Order
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; private set; } = new List<OrderStatusHistory>();

    private Order() { } // EF Core

    public static Order Create(string userId, decimal totalAmount, string? shippingAddress = null)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var order = new Order
        {
            UserId = userId,
            TotalAmount = totalAmount,
            ShippingAddress = shippingAddress,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        order.StatusHistory.Add(OrderStatusHistory.Create(order.Id, OrderStatus.Pending));

        return order;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        if (Status == newStatus)
            throw new InvalidOperationException($"Order is already in {newStatus} status.");

        if (newStatus <= Status)
            throw new InvalidOperationException($"Invalid status transition from {Status} to {newStatus}. Orders must move forward only.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        StatusHistory.Add(OrderStatusHistory.Create(Id, newStatus));
    }

    public void SetPaymentIntent(string paymentIntentId)
    {
        StripePaymentIntentId = paymentIntentId;
    }
}
