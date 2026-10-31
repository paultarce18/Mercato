using Mercato.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>Maps the <see cref="Product"/> aggregate to the catalog.Products table.</summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Products", "catalog");

        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(product => product.Description).HasMaxLength(Product.DescriptionMaxLength).IsRequired();
        builder.Property(product => product.Sku).HasMaxLength(Product.SkuMaxLength).IsRequired();

        // Optional: a product without an image stores NULL.
        builder.Property(product => product.ImageUrl).HasMaxLength(Product.ImageUrlMaxLength);

        builder.Property(product => product.IsActive).IsRequired();

        // Money is a value object with no identity and no table of its own. "Owned" means its two
        // values (amount, currency) are stored as columns of the Products table (TR-44).
        builder.OwnsOne(
            product => product.Price,
            price =>
            {
                price.Property(money => money.Amount)
                    .HasColumnName("PriceAmount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                price.Property(money => money.Currency)
                    .HasColumnName("PriceCurrency")
                    .HasMaxLength(3)
                    .IsFixedLength()
                    .IsRequired();
            });

        // Every product must have a price, so the two columns above must be NOT NULL.
        builder.Navigation(product => product.Price).IsRequired();

        // A SKU is unique per vendor, not globally (see the remark on Product.Sku).
        builder.HasIndex(product => new { product.VendorId, product.Sku }).IsUnique();

        // Foreign keys inside the Catalog module are allowed (TR-12 only forbids them across
        // modules). Restrict: vendors and categories are never deleted (BR-12), so the database
        // must refuse a delete instead of silently removing products.
        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(product => product.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
