using Mercato.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="StockHold"/>, a child entity of <see cref="InventoryItem"/>. It has no DbSet on
/// the context: it is reached only through its aggregate root. The foreign key to the item is
/// configured on the root side, in <see cref="InventoryItemConfiguration"/>.
/// </summary>
internal sealed class StockHoldConfiguration : IEntityTypeConfiguration<StockHold>
{
    public void Configure(EntityTypeBuilder<StockHold> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("StockHolds", "inventory");

        builder.HasKey(hold => hold.Id);
        builder.Property(hold => hold.Id).ValueGeneratedNever();

        builder.Property(hold => hold.CartId).IsRequired();
        builder.Property(hold => hold.Quantity).IsRequired();
        builder.Property(hold => hold.ExpiresAt).IsRequired();

        // The cart lives in the Ordering module: an index to find holds by cart, no foreign key (TR-12).
        builder.HasIndex(hold => hold.CartId);
    }
}
