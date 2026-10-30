using Mercato.Domain.Common;

namespace Mercato.Domain.Catalog;

/// <summary>
/// Something a vendor sells (FR-CT-01, FR-CT-02). Owned by exactly one vendor, referenced by id
/// only (TR-12, TR-15). Deactivation hides the product and is reversible; products are never
/// deleted (BR-12). Uniqueness of <see cref="Sku"/> per vendor cannot be checked from inside one
/// product; it is enforced by a unique index (vendor id + SKU) in the database.
/// </summary>
public sealed class Product : AggregateRoot
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int SkuMaxLength = 50;
    public const int ImageUrlMaxLength = 2048;

    /// <summary>Used by EF Core when materialising a product from the database.</summary>
    private Product()
    {
        Name = string.Empty;
        Description = string.Empty;
        Sku = string.Empty;
        Price = Money.Zero;
    }

    private Product(
        Guid id,
        Guid vendorId,
        Guid categoryId,
        string name,
        string description,
        string sku,
        Money price,
        string? imageUrl)
        : base(id)
    {
        VendorId = vendorId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Sku = sku;
        Price = price;
        ImageUrl = imageUrl;
        IsActive = true;
    }

    /// <summary>Owning vendor (the tenant). Never changes after creation (BR-02).</summary>
    public Guid VendorId { get; private set; }

    public Guid CategoryId { get; private set; }

    public string Name { get; private set; }

    public string Description { get; private set; }

    /// <summary>Stock keeping unit, unique per vendor.</summary>
    public string Sku { get; private set; }

    public Money Price { get; private set; }

    /// <summary>V1: a single image URL, no upload (FR-CT-02). Null when the product has no image.</summary>
    public string? ImageUrl { get; private set; }

    public bool IsActive { get; private set; }

    public static Product Create(
        Guid vendorId,
        Guid categoryId,
        string name,
        string description,
        string sku,
        Money price,
        string? imageUrl)
    {
        return new Product(
            NewId(),
            Guard.NotEmpty(vendorId, nameof(vendorId)),
            Guard.NotEmpty(categoryId, nameof(categoryId)),
            ValidateName(name),
            ValidateDescription(description),
            ValidateSku(sku),
            ValidatePrice(price),
            ValidateImageUrl(imageUrl));
    }

    /// <summary>Changes the descriptive data. Vendor and SKU are fixed once the product exists.</summary>
    public void Update(Guid categoryId, string name, string description, string? imageUrl)
    {
        CategoryId = Guard.NotEmpty(categoryId, nameof(categoryId));
        Name = ValidateName(name);
        Description = ValidateDescription(description);
        ImageUrl = ValidateImageUrl(imageUrl);
    }

    /// <summary>
    /// Changes the price for future carts and orders. Existing order items keep the price they
    /// were created with (BR-04).
    /// </summary>
    public void ChangePrice(Money price) => Price = ValidatePrice(price);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, NameMaxLength, nameof(name));
        return name.Trim();
    }

    private static string ValidateDescription(string description)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(description.Length, DescriptionMaxLength, nameof(description));
        return description.Trim();
    }

    private static string ValidateSku(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sku.Length, SkuMaxLength, nameof(sku));
        return sku.Trim();
    }

    private static Money ValidatePrice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        return price;
    }

    private static string? ValidateImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        ArgumentOutOfRangeException.ThrowIfGreaterThan(imageUrl.Length, ImageUrlMaxLength, nameof(imageUrl));
        return imageUrl.Trim();
    }
}
