using Microsoft.AspNetCore.Http;

namespace PetPlatform.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string subDirectory);
}
