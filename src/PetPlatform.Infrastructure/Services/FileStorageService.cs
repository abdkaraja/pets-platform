using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private static readonly string[] PermittedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public FileStorageService(IWebHostEnvironment env)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
    }

    public async Task<string> SaveFileAsync(IFormFile file, string subDirectory)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(subDirectory);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !PermittedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"File type not permitted. Allowed types: {string.Join(", ", PermittedExtensions)}");
        }

        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException(
                $"File size exceeds the maximum allowed size of {MaxFileSize / (1024 * 1024)}MB.");
        }

        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", subDirectory);
        Directory.CreateDirectory(uploadsDir);

        var filePath = Path.Combine(uploadsDir, safeFileName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{subDirectory}/{safeFileName}";
    }
}
