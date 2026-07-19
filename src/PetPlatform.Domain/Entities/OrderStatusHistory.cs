using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class OrderStatusHistory
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public Order Order { get; private set; } = null!;

    private OrderStatusHistory() { } // EF Core

    public static OrderStatusHistory Create(int orderId, OrderStatus status)
    {
        return new OrderStatusHistory
        {
            OrderId = orderId,
            Status = status,
            ChangedAt = DateTime.UtcNow
        };
    }
}
