using Microsoft.EntityFrameworkCore;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Application.Interfaces;

/// <summary>
/// Abstraction over the database context, defined in Application layer
/// to maintain Clean Architecture dependency rules.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Pet> Pets { get; }
    DbSet<CustomerProfile> CustomerProfiles { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
