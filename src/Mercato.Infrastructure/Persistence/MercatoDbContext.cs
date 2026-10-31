using Mercato.Domain.Catalog;
using Mercato.Domain.Common;
using Mercato.Domain.Inventory;
using Mercato.Domain.Notifications;
using Mercato.Domain.Ordering;
using Mercato.Domain.Payments;
using Mercato.Domain.Shipping;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Persistence;

/// <summary>
/// The EF Core entry point to the database: one instance is one unit of work. Load aggregates,
/// change them through their methods, call SaveChangesAsync, and EF writes everything that changed
/// in one transaction. It exposes one <see cref="DbSet{TEntity}"/> per aggregate root (TR-13);
/// child entities such as cart items are reached through their root and have no DbSet of their own.
/// </summary>
public sealed class MercatoDbContext : DbContext
{
    /// <summary>
    /// The options carry the connection string and the SQL Server provider. They are supplied by
    /// dependency injection when the context is registered in the Api project, never set here.
    /// </summary>
    public MercatoDbContext(DbContextOptions<MercatoDbContext> options)
        : base(options)
    {
    }

    // Catalog module
    public DbSet<Vendor> Vendors => Set<Vendor>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Product> Products => Set<Product>();

    // Inventory module
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    // Ordering module
    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<Order> Orders => Set<Order>();

    // Payments, Shipping and Notifications modules
    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Shipment> Shipments => Set<Shipment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>Called once by EF when it builds its model of tables, columns and keys.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Domain events live only in memory (they are dispatched after saving, TR-24),
        // so EF must not try to map them to tables.
        modelBuilder.Ignore<DomainEvent>();

        // Finds every IEntityTypeConfiguration<T> class in this assembly and applies it, so adding
        // a new aggregate means adding a configuration class and nothing else here.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MercatoDbContext).Assembly);
    }
}
