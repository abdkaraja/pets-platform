using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class Brand
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Brand() { } // EF Core

    public static Brand Create(string name)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));

        return new Brand
        {
            Name = name
        };
    }

    public void UpdateName(string name)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Name = name;
    }
}
