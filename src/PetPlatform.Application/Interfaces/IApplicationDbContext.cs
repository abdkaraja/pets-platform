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

    // E-Commerce entities
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderStatusHistory> OrderStatusHistory { get; }
    DbSet<Payment> Payments { get; }

    // Adoption entities
    DbSet<AdoptionListing> AdoptionListings { get; }
    DbSet<AdoptionApplication> AdoptionApplications { get; }
    DbSet<ApplicationStatusHistory> ApplicationStatusHistory { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
