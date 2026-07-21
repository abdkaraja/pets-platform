using Ardalis.GuardClauses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;
using PetPlatform.Domain.Enums;
using Stripe;
using Stripe.Checkout;
using static PetPlatform.Application.Common.VariantDescriptionBuilder;

namespace PetPlatform.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;
    private readonly IOrderService _orderService;
    private readonly StripeClient _stripeClient;
    private readonly IConfiguration _configuration;

    public PaymentService(
        IApplicationDbContext context,
        IOrderService orderService,
        StripeClient stripeClient,
        IConfiguration configuration)
    {
        _context = Guard.Against.Null(context, nameof(context));
        _orderService = Guard.Against.Null(orderService, nameof(orderService));
        _stripeClient = Guard.Against.Null(stripeClient, nameof(stripeClient));
        _configuration = Guard.Against.Null(configuration, nameof(configuration));
    }

    public async Task<Result<CheckoutSessionDto>> CreateCheckoutSessionAsync(string userId, string successUrl, string cancelUrl)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(ci => ci.ProductVariant)
                    .ThenInclude(v => v.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.Items.Any())
            return Result<CheckoutSessionDto>.Failure("Cart is empty.");

        // Build line items from cart using LockedPrice (convert to cents)
        var lineItems = cart.Items.Select(ci => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "usd",
                UnitAmount = (long)(ci.LockedPrice * 100), // Convert to cents
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = ci.ProductVariant?.Product?.Name ?? "Product",
                    Description = Build(ci.ProductVariant)
                }
            },
            Quantity = ci.Quantity
        }).ToList();

        var options = new SessionCreateOptions
        {
            UiMode = "elements", // D-07: Stripe Elements (embedded)
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "userId", userId }
            }
        };

        var session = await _stripeClient.V1.Checkout.Sessions.CreateAsync(options);

        return Result<CheckoutSessionDto>.Success(new CheckoutSessionDto
        {
            SessionId = session.Id,
            ClientSecret = session.ClientSecret ?? string.Empty,
            PublishableKey = _configuration["Stripe:PublishableKey"] ?? string.Empty
        });
    }

    public async Task HandleCheckoutSessionCompletedAsync(string sessionId)
    {
        Guard.Against.NullOrWhiteSpace(sessionId, nameof(sessionId));

        var session = await _stripeClient.V1.Checkout.Sessions.GetAsync(sessionId);

        if (session?.Metadata?.TryGetValue("userId", out var userId) != true)
            return;

        // Find order by StripePaymentIntentId
        var paymentIntentId = session.PaymentIntentId;
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntentId);

        if (order is not null && order.Status == OrderStatus.Pending)
        {
            await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);
        }
    }

    public async Task<OrderDto?> GetOrderAfterPaymentAsync(string sessionId)
    {
        var session = await _stripeClient.V1.Checkout.Sessions.GetAsync(sessionId);

        if (session?.PaymentIntentId is null) return null;

        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == session.PaymentIntentId);

        if (order is null) return null;

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }
}
