using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Entities;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    // E-Commerce entities
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Adoption entities
    public DbSet<AdoptionListing> AdoptionListings => Set<AdoptionListing>();
    public DbSet<AdoptionApplication> AdoptionApplications => Set<AdoptionApplication>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistory => Set<ApplicationStatusHistory>();

    // Lost Pets entities
    public DbSet<LostPetReport> LostPetReports => Set<LostPetReport>();
    public DbSet<LostPetReportPhoto> LostPetReportPhotos => Set<LostPetReportPhoto>();
    public DbSet<MatchNotification> MatchNotifications => Set<MatchNotification>();

    // Medical Records & Vet Management entities
    public DbSet<VetProfile> VetProfiles => Set<VetProfile>();
    public DbSet<VetAvailability> VetAvailability => Set<VetAvailability>();
    public DbSet<VetAssignment> VetAssignments => Set<VetAssignment>();
    public DbSet<VaccinationRecord> VaccinationRecords => Set<VaccinationRecord>();
    public DbSet<MedicationRecord> MedicationRecords => Set<MedicationRecord>();
    public DbSet<VetVisitNote> VetVisitNotes => Set<VetVisitNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // MUST call first for Identity tables
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
