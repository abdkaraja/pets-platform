using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;

    public OrderService(IApplicationDbContext context)
    {
        _context = Guard.Against.Null(context, nameof(context));
    }

    public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToDto(o))
            .ToListAsync();
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int orderId, string? userId = null)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .AsQueryable();

        var order = await query.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return null;

        // Ownership check for non-admin
        if (!string.IsNullOrEmpty(userId) && order.UserId != userId)
            return null;

        return MapToDto(order);
    }

    public async Task<Result<OrderDto>> CreateOrderFromCartAsync(string userId, string? shippingAddress, string stripePaymentIntentId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(stripePaymentIntentId, nameof(stripePaymentIntentId));

        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.ProductVariant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.Items.Any())
            return Result<OrderDto>.Failure("Cart is empty.");

        // Calculate total from LockedPrice (anti-pattern avoidance — NOT current product price)
        var totalAmount = cart.Items.Sum(ci => ci.LockedPrice * ci.Quantity);

        // Create order with pending status
        var order = Order.Create(userId, totalAmount, shippingAddress);
        order.SetPaymentIntent(stripePaymentIntentId);

        // Copy each CartItem to OrderItem (preserving historical prices)
        foreach (var cartItem in cart.Items)
        {
            var variantDesc = BuildVariantDescription(cartItem.ProductVariant);
            var orderItem = OrderItem.Create(
                order.Id,
                cartItem.ProductVariant?.Product?.Name ?? "Unknown Product",
                variantDesc,
                cartItem.Quantity,
                cartItem.LockedPrice, // Price drift prevention: copy from LockedPrice
                cartItem.ProductVariant?.Product?.ImagePath);
            order.Items.Add(orderItem);

            // Reduce stock for each variant
            cartItem.ProductVariant?.ReduceStock(cartItem.Quantity);
        }

        _context.Orders.Add(order);

        // D-14: Clear cart after checkout
        cart.Clear();

        await _context.SaveChangesAsync();

        return Result<OrderDto>.Success(await GetOrderByIdAsync(order.Id) ?? MapToDto(order));
    }

    public async Task<IEnumerable<AdminOrderDto>> GetAllOrdersAsync(OrderStatus? statusFilter = null, string? searchTerm = null)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(o => o.Status == statusFilter.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(o => o.UserId.Contains(searchTerm));

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new AdminOrderDto
            {
                Id = o.Id,
                CustomerEmail = o.UserId,
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                ItemCount = o.Items.Count,
                NextStatus = GetNextStatus(o.Status)
            })
            .ToListAsync();
    }

    public async Task<Result<OrderDto>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
            return Result<OrderDto>.Failure("Order not found.");

        try
        {
            // D-16: Validates forward-only transitions
            order.UpdateStatus(newStatus);
        }
        catch (InvalidOperationException ex)
        {
            return Result<OrderDto>.Failure(ex.Message);
        }

        await _context.SaveChangesAsync();

        return Result<OrderDto>.Success(MapToDto(order));
    }

    private static OrderStatus? GetNextStatus(OrderStatus current) => current switch
    {
        OrderStatus.Pending => OrderStatus.Processing,
        OrderStatus.Processing => OrderStatus.Shipped,
        OrderStatus.Shipped => OrderStatus.Delivered,
        _ => null
    };

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        ShippingAddress = order.ShippingAddress,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        Items = order.Items.Select(oi => new OrderItemDto
        {
            Id = oi.Id,
            ProductName = oi.ProductName,
            VariantDescription = oi.VariantDescription,
            Quantity = oi.Quantity,
            UnitPrice = oi.UnitPrice,
            ImagePath = oi.ImagePath
        }).ToList(),
        StatusHistory = order.StatusHistory.Select(sh => new OrderStatusHistoryDto
        {
            Status = sh.Status,
            ChangedAt = sh.ChangedAt
        }).ToList()
    };

    private static string BuildVariantDescription(ProductVariant? variant)
    {
        if (variant is null) return string.Empty;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(variant.Size)) parts.Add($"Size: {variant.Size}");
        if (!string.IsNullOrEmpty(variant.Color)) parts.Add($"Color: {variant.Color}");
        return string.Join(", ", parts);
    }
}
