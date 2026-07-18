using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface IPetService
{
    Task<Result<PetDto>> CreateAsync(CreatePetDto dto, string ownerId);
    Task<IEnumerable<PetDto>> GetAllByOwnerAsync(string ownerId);
    Task<IEnumerable<PetDto>> GetAllAsync();
    Task<PetDto?> GetByIdAsync(int id);
    Task<Result<PetDto>> UpdateAsync(int id, UpdatePetDto dto, string ownerId);
    Task<Result<bool>> DeleteAsync(int id, string ownerId);
}
