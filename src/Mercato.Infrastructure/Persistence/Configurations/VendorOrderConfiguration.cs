using Mercato.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="VendorOrder"/>, the part of an order that one vendor fulfils. It is a child of
/// <see cref="Order"/>; the foreign key is configured on the Order side.
/// </summary>
internal sealed class VendorOrderConfiguration : IEntityTypeConfiguration<VendorOrder>
{
    public void Configure(EntityTypeBuilder<VendorOrder> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("VendorOrders", "ordering");

        builder.HasKey(vendorOrder => vendorOrder.Id);
        builder.Property(vendorOrder => vendorOrder.Id).ValueGeneratedNever();

        // The vendor lives in the Catalog module: an id, no foreign key (TR-12). Vendors list their
        // own orders by it, so it is indexed (TR-15).
        builder.Property(vendorOrder => vendorOrder.VendorId).IsRequired();
        builder.HasIndex(vendorOrder => vendorOrder.VendorId);

        builder.Property(vendorOrder => vendorOrder.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // The items are children of this vendor order. Subtotal is calculated in code, no column.
        builder.HasMany(vendorOrder => vendorOrder.Items)
            .WithOne()
            .HasForeignKey("VendorOrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(vendorOrder => vendorOrder.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
