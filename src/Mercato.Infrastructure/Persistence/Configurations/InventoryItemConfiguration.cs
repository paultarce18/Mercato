using Mercato.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>Maps the <see cref="InventoryItem"/> aggregate to the inventory.InventoryItems table.</summary>
internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("InventoryItems", "inventory");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.VendorId).IsRequired();
        builder.Property(item => item.OnHand).IsRequired();
        builder.Property(item => item.Reserved).IsRequired();

        // Optimistic concurrency (TR-43). SQL Server changes this value on every update; when two
        // buyers load the same row and both try to save, the second save finds a different value,
        // updates zero rows, and EF throws DbUpdateConcurrencyException instead of overselling.
        builder.Property(item => item.RowVersion).IsRowVersion();

        // A change to the holds alone would update only StockHolds rows, which RowVersion does not
        // protect. InventoryItem increments Version on every hold mutation, so every such save also
        // updates this row with "WHERE RowVersion = @old" and two buyers cannot both hold the last unit.
        builder.Property(item => item.Version).IsRequired();

        // One stock record per product. ProductId belongs to the Catalog module, so there is no
        // foreign key to Products (TR-12): modules reference each other by id only.
        builder.HasIndex(item => item.ProductId).IsUnique();

        // Every vendor-owned table is filtered by VendorId (TR-15), so it gets an index.
        builder.HasIndex(item => item.VendorId);

        // The holds are children of this aggregate. EF fills the private "holds" list through its
        // field, so the only way to change them stays the methods on InventoryItem. Deleting an
        // item deletes its holds. "InventoryItemId" is a shadow property: the database has the
        // column, but StockHold has no such property because the domain does not need it.
        builder.HasMany(item => item.Holds)
            .WithOne()
            .HasForeignKey("InventoryItemId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(item => item.Holds).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
