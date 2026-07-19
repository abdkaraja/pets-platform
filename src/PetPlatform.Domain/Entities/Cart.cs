using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class Cart
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    private Cart() { } // EF Core

    public static Cart Create(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        return new Cart
        {
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Clear()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}
