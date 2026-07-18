using Microsoft.AspNetCore.Http;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Application.DTOs;

public class PetDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public PetSpecies Species { get; init; }
    public string? Breed { get; init; }
    public int Age { get; init; }
    public decimal Weight { get; init; }
    public string? PhotoPath { get; init; }
    public string OwnerId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public class CreatePetDto
{
    public string Name { get; set; } = string.Empty;
    public PetSpecies Species { get; set; }
    public string? Breed { get; set; }
    public int Age { get; set; }
    public decimal Weight { get; set; }
    public IFormFile? Photo { get; set; }
}

public class UpdatePetDto
{
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public int Age { get; set; }
    public decimal Weight { get; set; }
}
