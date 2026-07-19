using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.DTOs;

public class AdminOrderDto
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public string StatusText => Status.ToString();
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public OrderStatus? NextStatus { get; set; }
}
