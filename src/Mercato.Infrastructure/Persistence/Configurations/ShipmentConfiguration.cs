using Mercato.Domain.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>Maps the <see cref="Shipment"/> aggregate to the shipping.Shipments table.</summary>
internal sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Shipments", "shipping");

        builder.HasKey(shipment => shipment.Id);
        builder.Property(shipment => shipment.Id).ValueGeneratedNever();

        // The vendor order lives in the Ordering module: an id, no foreign key (TR-12). Unique
        // because V1 creates one shipment per vendor order.
        builder.Property(shipment => shipment.VendorOrderId).IsRequired();
        builder.HasIndex(shipment => shipment.VendorOrderId).IsUnique();

        // Vendor-owned data carries the tenant id and an index on it (TR-15).
        builder.Property(shipment => shipment.VendorId).IsRequired();
        builder.HasIndex(shipment => shipment.VendorId);

        builder.Property(shipment => shipment.TrackingNumber).HasMaxLength(Shipment.TrackingNumberMaxLength).IsRequired();
        builder.Property(shipment => shipment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(shipment => shipment.ShippedAt).IsRequired();

        // Optional: stays NULL until the delivery event arrives.
        builder.Property(shipment => shipment.DeliveredAt);
    }
}
