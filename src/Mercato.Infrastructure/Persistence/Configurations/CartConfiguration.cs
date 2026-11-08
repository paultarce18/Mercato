using Mercato.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>Maps the <see cref="Cart"/> aggregate to the ordering.Carts table.</summary>
internal sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Carts", "ordering");

        builder.HasKey(cart => cart.Id);
        builder.Property(cart => cart.Id).ValueGeneratedNever();

        // The customer is a user, which lives in another module (TR-12): an id, no foreign key.
        builder.Property(cart => cart.CustomerId).IsRequired();

        // Enums are stored as their names ("Active", "CheckedOut") so the data is readable in SSMS
        // and reordering the enum in code can never silently change the meaning of stored rows.
        builder.Property(cart => cart.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // A customer has at most one active cart (FR-CA-01). A filtered unique index enforces it:
        // only rows with Status = 'Active' take part, so checked-out carts can pile up freely.
        builder.HasIndex(cart => cart.CustomerId)
            .IsUnique()
            .HasFilter("[Status] = 'Active'");

        // The items are children of this aggregate; same pattern as the holds on InventoryItem.
        builder.HasMany(cart => cart.Items)
            .WithOne()
            .HasForeignKey("CartId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(cart => cart.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
