using Mercato.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>
/// Describes how the <see cref="Vendor"/> aggregate maps to the database. Picked up automatically
/// by <see cref="MercatoDbContext"/>. Mapping rules live here and not as attributes on the domain
/// class, so the Domain project stays free of any database knowledge.
/// </summary>
internal sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Table "Vendors" in the "catalog" schema: one schema per module (TR-41).
        builder.ToTable("Vendors", "catalog");

        builder.HasKey(vendor => vendor.Id);

        // Our code creates the id (Guid version 7, TR-45); the database must never generate it.
        builder.Property(vendor => vendor.Id).ValueGeneratedNever();

        // Lengths come from the domain constants, so the limit is defined in exactly one place.
        builder.Property(vendor => vendor.Name).HasMaxLength(Vendor.NameMaxLength).IsRequired();
        builder.Property(vendor => vendor.ContactEmail).HasMaxLength(Vendor.ContactEmailMaxLength).IsRequired();

        builder.Property(vendor => vendor.IsActive).IsRequired();
    }
}
