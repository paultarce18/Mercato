using Mercato.Domain.Common;

namespace Mercato.Domain.Inventory;

/// <summary>
/// Stock of one product and the rules that keep it correct. Owns the product's
/// <see cref="StockHold"/>s. Every method that depends on time receives the current time as a
/// parameter, so the rules are testable without waiting (GeneralRules §6).
/// <para>
/// <c>Available = OnHand - Reserved - active holds</c> and never drops below zero (FR-IN-06,
/// BR-05). Two buyers racing for the last unit both pass <see cref="Hold"/> on their own copy of
/// the data; <see cref="RowVersion"/> makes the database accept only one of the two saves
/// (FR-IN-04, TR-43).
/// </para>
/// </summary>
public sealed class InventoryItem : AggregateRoot
{
    /// <summary>How long a cart keeps its claim on stock (FR-IN-03, BR-06).</summary>
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(15);

    private readonly List<StockHold> holds = [];

    /// <summary>Used by EF Core when materialising an item from the database.</summary>
    private InventoryItem()
    {
        RowVersion = [];
    }

    private InventoryItem(Guid id, Guid productId, Guid vendorId, int onHand)
        : base(id)
    {
        ProductId = productId;
        VendorId = vendorId;
        OnHand = onHand;
        RowVersion = [];
    }

    public Guid ProductId { get; private set; }

    /// <summary>Owning vendor (the tenant); copied from the product at creation (BR-02, TR-15).</summary>
    public Guid VendorId { get; private set; }

    /// <summary>Units physically in stock.</summary>
    public int OnHand { get; private set; }

    /// <summary>Units promised to placed orders but not yet shipped.</summary>
    public int Reserved { get; private set; }

    public IReadOnlyCollection<StockHold> Holds => holds.AsReadOnly();

    /// <summary>Optimistic concurrency token; maintained by the database, never set in code.</summary>
    public byte[] RowVersion { get; private set; }

    public static InventoryItem Create(Guid productId, Guid vendorId, int onHand)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(onHand);
        return new InventoryItem(NewId(), Guard.NotEmpty(productId, nameof(productId)), Guard.NotEmpty(vendorId, nameof(vendorId)), onHand);
    }

    /// <summary>Units that another cart could still hold right now.</summary>
    public int AvailableAt(DateTimeOffset now) => OnHand - Reserved - HeldAt(now, excludingCartId: null);

    /// <summary>Vendor sets the stock on hand (FR-IN-01). Cannot go below what is already promised.</summary>
    public void SetOnHand(int quantity, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);

        int committed = Reserved + HeldAt(now, excludingCartId: null);
        if (quantity < committed)
        {
            throw new StockBelowCommitmentException(ProductId, quantity, committed);
        }

        OnHand = quantity;
    }

    /// <summary>
    /// Makes the cart's hold on this product exactly <paramref name="quantity"/> units for another
    /// <see cref="HoldDuration"/> (FR-IN-02). An existing hold of the same cart is replaced, so
    /// changing a quantity in the cart does not count the old hold twice.
    /// </summary>
    /// <exception cref="InsufficientStockException">Not enough stock is available; nothing changes.</exception>
    public void Hold(Guid cartId, int quantity, DateTimeOffset now)
    {
        Guard.NotEmpty(cartId, nameof(cartId));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        int available = OnHand - Reserved - HeldAt(now, excludingCartId: cartId);
        if (quantity > available)
        {
            throw new InsufficientStockException(ProductId, quantity, available);
        }

        holds.RemoveAll(hold => hold.CartId == cartId || !hold.IsActiveAt(now));
        holds.Add(StockHold.Create(cartId, quantity, now + HoldDuration));
    }

    /// <summary>Drops the cart's hold, for example when the item is removed from the cart. No-op if there is none.</summary>
    public void ReleaseHold(Guid cartId) => holds.RemoveAll(hold => hold.CartId == cartId);

    /// <summary>Drops every hold that has expired. Called by the hold-expiry job (FR-IN-03).</summary>
    /// <returns>The number of holds removed.</returns>
    public int ReleaseExpiredHolds(DateTimeOffset now) => holds.RemoveAll(hold => !hold.IsActiveAt(now));

    /// <summary>Checkout: turns the cart's hold into a reservation (FR-IN-05).</summary>
    /// <exception cref="StockHoldNotFoundException">The cart has no hold, or it has expired (BR-06).</exception>
    public void ConvertHoldToReservation(Guid cartId, DateTimeOffset now)
    {
        StockHold hold = holds.FirstOrDefault(candidate => candidate.CartId == cartId && candidate.IsActiveAt(now))
            ?? throw new StockHoldNotFoundException(ProductId, cartId);

        holds.Remove(hold);
        Reserved += hold.Quantity;
    }

    /// <summary>Vendor shipped: the units leave the warehouse (FR-IN-05).</summary>
    public void Ship(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        EnsureReserved(quantity);

        Reserved -= quantity;
        OnHand -= quantity;
    }

    /// <summary>The order was rejected (payment declined, FR-PA-05): the units become available again.</summary>
    public void ReleaseReservation(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        EnsureReserved(quantity);

        Reserved -= quantity;
    }

    private int HeldAt(DateTimeOffset now, Guid? excludingCartId) =>
        holds
            .Where(hold => hold.IsActiveAt(now) && hold.CartId != excludingCartId)
            .Sum(hold => hold.Quantity);

    /// <summary>Shipping or releasing more than is reserved is a caller bug, not a business failure.</summary>
    private void EnsureReserved(int quantity)
    {
        if (quantity > Reserved)
        {
            throw new InvalidOperationException($"Cannot take {quantity} unit(s) from a reservation of {Reserved}.");
        }
    }
}
