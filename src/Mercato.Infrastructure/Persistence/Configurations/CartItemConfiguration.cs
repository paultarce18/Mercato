using Mercato.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="CartItem"/>, a child entity of <see cref="Cart"/>. The foreign key to the cart
/// is configured on the root side, in <see cref="CartConfiguration"/>.
/// </summary>
internal sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("CartItems", "ordering");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        // Product and vendor belong to other modules: ids only, no foreign keys (TR-12).
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.VendorId).IsRequired();

        builder.Property(item => item.ProductName).HasMaxLength(CartItem.ProductNameMaxLength).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.HoldExpiresAt).IsRequired();

        // Columns UnitPriceAmount and UnitPriceCurrency. LineTotal is calculated in code and has no column.
        builder.OwnsMoney(item => item.UnitPrice, "UnitPrice");

        // A cart holds one item per product (Cart.SetItem replaces an existing one). "CartId" is the
        // shadow foreign key declared on the Cart side, so it is referred to by name.
        builder.HasIndex("CartId", nameof(CartItem.ProductId)).IsUnique();
    }
}
