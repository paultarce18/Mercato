using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>
/// One product in a cart. Part of the <see cref="Cart"/> aggregate. Product name and unit price are
/// copies taken when the item was set, because Ordering must not reach into Catalog (TR-12);
/// checkout freezes them onto the order (BR-04).
/// </summary>
public sealed class CartItem : Entity
{
    public const int ProductNameMaxLength = 200;

    /// <summary>Used by EF Core when materialising an item from the database.</summary>
    private CartItem()
    {
        ProductName = string.Empty;
        UnitPrice = Money.Zero;
    }

    private CartItem(
        Guid id,
        Guid productId,
        Guid vendorId,
        string productName,
        Money unitPrice,
        int quantity,
        DateTimeOffset holdExpiresAt)
        : base(id)
    {
        ProductId = productId;
        VendorId = vendorId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        HoldExpiresAt = holdExpiresAt;
    }

    public Guid ProductId { get; private set; }

    public Guid VendorId { get; private set; }

    public string ProductName { get; private set; }

    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    /// <summary>When the stock hold that backs this item runs out (FR-IN-03).</summary>
    public DateTimeOffset HoldExpiresAt { get; private set; }

    public Money LineTotal => UnitPrice * Quantity;

    /// <summary>An item whose hold ran out is flagged and must be added again (FR-CA-04, BR-06).</summary>
    public bool IsHoldExpiredAt(DateTimeOffset now) => HoldExpiresAt <= now;

    internal static CartItem Create(
        Guid productId,
        Guid vendorId,
        string productName,
        Money unitPrice,
        int quantity,
        DateTimeOffset holdExpiresAt)
    {
        return new CartItem(
            NewId(),
            Guard.NotEmpty(productId, nameof(productId)),
            Guard.NotEmpty(vendorId, nameof(vendorId)),
            ValidateProductName(productName),
            ValidateUnitPrice(unitPrice),
            ValidateQuantity(quantity),
            holdExpiresAt);
    }

    internal void Update(string productName, Money unitPrice, int quantity, DateTimeOffset holdExpiresAt)
    {
        ProductName = ValidateProductName(productName);
        UnitPrice = ValidateUnitPrice(unitPrice);
        Quantity = ValidateQuantity(quantity);
        HoldExpiresAt = holdExpiresAt;
    }

    private static string ValidateProductName(string productName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(productName.Length, ProductNameMaxLength, nameof(productName));
        return productName.Trim();
    }

    private static Money ValidateUnitPrice(Money unitPrice)
    {
        ArgumentNullException.ThrowIfNull(unitPrice);
        return unitPrice;
    }

    private static int ValidateQuantity(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        return quantity;
    }
}
