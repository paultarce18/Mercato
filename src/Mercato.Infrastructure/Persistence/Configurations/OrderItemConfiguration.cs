using Mercato.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="OrderItem"/>: a snapshot of what was bought, with the name and price as they
/// were at checkout (BR-04). Child of <see cref="VendorOrder"/>.
/// </summary>
internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OrderItems", "ordering");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        // The product lives in the Catalog module: an id, no foreign key (TR-12). The name and price
        // below are copies, so the order stays correct even if the product changes or is deactivated.
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.ProductName).HasMaxLength(CartItem.ProductNameMaxLength).IsRequired();
        builder.Property(item => item.Quantity).IsRequired();

        // Columns UnitPriceAmount and UnitPriceCurrency. LineTotal is calculated in code.
        builder.OwnsMoney(item => item.UnitPrice, "UnitPrice");
    }
}
