using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface IPaymentService
{
    Task<Result<CheckoutSessionDto>> CreateCheckoutSessionAsync(string userId, string successUrl, string cancelUrl);
    Task HandleCheckoutSessionCompletedAsync(string sessionId);
    Task<OrderDto?> GetOrderAfterPaymentAsync(string sessionId);
}
