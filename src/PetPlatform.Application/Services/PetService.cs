using Ardalis.GuardClauses;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Application.Services;

public class PetService : IPetService
{
    private readonly IApplicationDbContext _context;
    private readonly IValidator<CreatePetDto> _createValidator;
    private readonly IValidator<UpdatePetDto> _updateValidator;

    public PetService(
        IApplicationDbContext context,
        IValidator<CreatePetDto> createValidator,
        IValidator<UpdatePetDto> updateValidator)
    {
        _context = Guard.Against.Null(context, nameof(context));
        _createValidator = Guard.Against.Null(createValidator, nameof(createValidator));
        _updateValidator = Guard.Against.Null(updateValidator, nameof(updateValidator));
    }

    public async Task<Result<PetDto>> CreateAsync(CreatePetDto dto, string ownerId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(ownerId, nameof(ownerId));

        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<PetDto>.Failure(errors);
        }

        var pet = Pet.Create(
            dto.Name,
            dto.Species,
            ownerId,
            breed: dto.Breed,
            age: dto.Age,
            weight: dto.Weight);

        if (!string.IsNullOrEmpty(dto.PhotoPath))
        {
            pet.PhotoPath = dto.PhotoPath;
        }

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();

        return Result<PetDto>.Success(MapToDto(pet));
    }

    public async Task<IEnumerable<PetDto>> GetAllByOwnerAsync(string ownerId)
    {
        Guard.Against.NullOrWhiteSpace(ownerId, nameof(ownerId));

        return await _context.Pets
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    public async Task<IEnumerable<PetDto>> GetAllAsync()
    {
        return await _context.Pets
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => MapToDto(p))
            .ToListAsync();
    }

    public async Task<PetDto?> GetByIdAsync(int id)
    {
        var pet = await _context.Pets.FindAsync(id);
        return pet is null ? null : MapToDto(pet);
    }

    public async Task<Result<PetDto>> UpdateAsync(int id, UpdatePetDto dto, string ownerId)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(ownerId, nameof(ownerId));

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<PetDto>.Failure(errors);
        }

        var pet = await _context.Pets.FindAsync(id);
        if (pet is null)
            return Result<PetDto>.Failure("Pet not found.");

        if (pet.OwnerId != ownerId)
            return Result<PetDto>.Failure("You are not authorized to modify this pet.");

        pet.UpdateDetails(dto.Name, dto.Breed, dto.Age, dto.Weight);
        await _context.SaveChangesAsync();

        return Result<PetDto>.Success(MapToDto(pet));
    }

    public async Task<Result<bool>> DeleteAsync(int id, string ownerId)
    {
        Guard.Against.NullOrWhiteSpace(ownerId, nameof(ownerId));

        var pet = await _context.Pets.FindAsync(id);
        if (pet is null)
            return Result<bool>.Failure("Pet not found.");

        if (pet.OwnerId != ownerId)
            return Result<bool>.Failure("You are not authorized to delete this pet.");

        _context.Pets.Remove(pet);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    private static PetDto MapToDto(Pet pet) => new()
    {
        Id = pet.Id,
        Name = pet.Name,
        Species = pet.Species,
        Breed = pet.Breed,
        Age = pet.Age,
        Weight = pet.Weight,
        PhotoPath = pet.PhotoPath,
        OwnerId = pet.OwnerId,
        CreatedAt = pet.CreatedAt
    };
}
