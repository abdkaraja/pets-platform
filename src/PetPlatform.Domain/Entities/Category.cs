using Ardalis.GuardClauses;

namespace PetPlatform.Domain.Entities;

public class Category
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int? ParentId { get; private set; }

    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { } // EF Core

    public static Category Create(string name, int? parentId = null)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));

        return new Category
        {
            Name = name,
            ParentId = parentId
        };
    }

    public void UpdateName(string name)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Name = name;
    }
}
