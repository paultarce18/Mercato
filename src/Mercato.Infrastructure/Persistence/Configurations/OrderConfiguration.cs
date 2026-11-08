using Mercato.Domain.Ordering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>Maps the <see cref="Order"/> aggregate to the ordering.Orders table.</summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Orders", "ordering");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        // The customer lives in another module: an id, no foreign key (TR-12). Indexed because a
        // customer's order history is looked up by it.
        builder.Property(order => order.CustomerId).IsRequired();
        builder.HasIndex(order => order.CustomerId);

        // The human-readable number shown to customers; no two orders may share one.
        builder.Property(order => order.OrderNumber).HasMaxLength(Order.OrderNumberMaxLength).IsRequired();
        builder.HasIndex(order => order.OrderNumber).IsUnique();

        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(order => order.PlacedAt).IsRequired();

        // Optimistic concurrency (TR-43). Order increments Version on every transition, including
        // vendor order transitions, so every save updates this row with "WHERE Version = @old".
        // Two deliveries racing for the last vendor orders cannot both save; the loser gets a
        // DbUpdateConcurrencyException, reloads the order and retries.
        builder.Property(order => order.Version).IsConcurrencyToken();

        // Columns TotalAmount and TotalCurrency.
        builder.OwnsMoney(order => order.Total, "Total");

        // ShippingAddress is a value object like Money: no table, five columns on Orders. The
        // address is copied into the order at checkout, so later changes to a customer's address
        // never alter an existing order.
        builder.OwnsOne(
            order => order.ShippingAddress,
            address =>
            {
                address.Property(value => value.Recipient)
                    .HasColumnName("ShippingRecipient")
                    .HasMaxLength(ShippingAddress.RecipientMaxLength)
                    .IsRequired();

                address.Property(value => value.Street)
                    .HasColumnName("ShippingStreet")
                    .HasMaxLength(ShippingAddress.StreetMaxLength)
                    .IsRequired();

                address.Property(value => value.PostalCode)
                    .HasColumnName("ShippingPostalCode")
                    .HasMaxLength(ShippingAddress.PostalCodeMaxLength)
                    .IsRequired();

                address.Property(value => value.City)
                    .HasColumnName("ShippingCity")
                    .HasMaxLength(ShippingAddress.CityMaxLength)
                    .IsRequired();

                address.Property(value => value.Country)
                    .HasColumnName("ShippingCountry")
                    .HasMaxLength(ShippingAddress.CountryMaxLength)
                    .IsRequired();
            });

        builder.Navigation(order => order.ShippingAddress).IsRequired();

        // One order has one vendor order per vendor involved (BR-03); they are children of the order.
        builder.HasMany(order => order.VendorOrders)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.VendorOrders).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
