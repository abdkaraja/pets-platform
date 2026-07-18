using Ardalis.GuardClauses;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class Pet
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public int Age { get; private set; }
    public decimal Weight { get; private set; }
    public string? PhotoPath { get; set; } // settable for file upload convenience
    public string OwnerId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Pet() { } // EF Core

    public static Pet Create(string name, PetSpecies species, string ownerId,
                             string? breed = null, int age = 0, decimal weight = 0)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(ownerId, nameof(ownerId));
        Guard.Against.Negative(age, nameof(age));
        Guard.Against.Negative(weight, nameof(weight));

        return new Pet
        {
            Name = name,
            Species = species,
            OwnerId = ownerId,
            Breed = breed,
            Age = age,
            Weight = weight,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string name, string? breed, int age, decimal weight)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.Negative(age, nameof(age));
        Guard.Against.Negative(weight, nameof(weight));

        Name = name;
        Breed = breed;
        Age = age;
        Weight = weight;
        UpdatedAt = DateTime.UtcNow;
    }
}
