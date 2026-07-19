using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId);
    Task<OrderDto?> GetOrderByIdAsync(int orderId, string? userId = null);
    Task<Result<OrderDto>> CreateOrderFromCartAsync(string userId, string? shippingAddress, string stripePaymentIntentId);
    Task<IEnumerable<AdminOrderDto>> GetAllOrdersAsync(OrderStatus? statusFilter = null, string? searchTerm = null);
    Task<Result<OrderDto>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
}
