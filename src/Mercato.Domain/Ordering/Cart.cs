using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>
/// A customer's shopping cart (FR-CA-01). Holds the items and turns them into an
/// <see cref="Order"/> at checkout. The stock side, placing and converting holds, belongs to
/// <c>InventoryItem</c>; the application layer coordinates both (TR-12). At most one active cart
/// per customer is enforced by a filtered unique index in the database.
/// </summary>
public sealed class Cart : AggregateRoot
{
    private readonly List<CartItem> items = [];

    /// <summary>Used by EF Core when materialising a cart from the database.</summary>
    private Cart()
    {
    }

    private Cart(Guid id, Guid customerId)
        : base(id)
    {
        CustomerId = customerId;
        Status = CartStatus.Active;
    }

    public Guid CustomerId { get; private set; }

    public CartStatus Status { get; private set; }

    public IReadOnlyCollection<CartItem> Items => items.AsReadOnly();

    public static Cart Create(Guid customerId) => new(NewId(), Guard.NotEmpty(customerId, nameof(customerId)));

    /// <summary>
    /// Adds the product, or replaces the existing item of that product, with the given quantity.
    /// Call it after the stock hold for exactly this quantity has been placed, passing the hold's expiry.
    /// </summary>
    /// <exception cref="CartNotActiveException">The cart was already checked out.</exception>
    public void SetItem(
        Guid productId,
        Guid vendorId,
        string productName,
        Money unitPrice,
        int quantity,
        DateTimeOffset holdExpiresAt)
    {
        EnsureActive();

        CartItem? existing = items.Find(item => item.ProductId == productId);
        if (existing is null)
        {
            items.Add(CartItem.Create(productId, vendorId, productName, unitPrice, quantity, holdExpiresAt));
        }
        else
        {
            existing.Update(productName, unitPrice, quantity, holdExpiresAt);
        }
    }

    /// <summary>Removes the product from the cart. No-op if it is not in the cart.</summary>
    /// <exception cref="CartNotActiveException">The cart was already checked out.</exception>
    public void RemoveItem(Guid productId)
    {
        EnsureActive();
        items.RemoveAll(item => item.ProductId == productId);
    }

    public Money GetTotal() => items.Aggregate(Money.Zero, (sum, item) => sum + item.LineTotal);

    /// <summary>
    /// Freezes the cart into a placed order (FR-OR-01…03). The cart stays readable as
    /// <see cref="CartStatus.CheckedOut"/>, so the caller can convert each item's stock hold into
    /// a reservation; the customer's next cart is a new one.
    /// </summary>
    /// <exception cref="CartNotActiveException">The cart was already checked out.</exception>
    /// <exception cref="EmptyCartException">The cart has no items.</exception>
    /// <exception cref="CartItemHoldExpiredException">An item's stock hold has expired (BR-06).</exception>
    public Order Checkout(string orderNumber, ShippingAddress shippingAddress, DateTimeOffset now)
    {
        EnsureActive();

        if (items.Count == 0)
        {
            throw new EmptyCartException(Id);
        }

        CartItem? expired = items.Find(item => item.IsHoldExpiredAt(now));
        if (expired is not null)
        {
            throw new CartItemHoldExpiredException(Id, expired.ProductId);
        }

        OrderLine[] lines =
        [
            .. items.Select(item => new OrderLine(item.ProductId, item.VendorId, item.ProductName, item.UnitPrice, item.Quantity)),
        ];

        var order = Order.Place(CustomerId, orderNumber, shippingAddress, lines, now);
        Status = CartStatus.CheckedOut;
        return order;
    }

    private void EnsureActive()
    {
        if (Status != CartStatus.Active)
        {
            throw new CartNotActiveException(Id);
        }
    }
}
