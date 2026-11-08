using Mercato.Domain.Common;

namespace Mercato.Domain.Inventory;

/// <summary>
/// A timed claim of one cart on a quantity of one product (FR-IN-02). Part of the
/// <see cref="InventoryItem"/> aggregate: it is created, replaced and removed only through it.
/// </summary>
public sealed class StockHold : Entity
{
    /// <summary>Used by EF Core when materialising a hold from the database.</summary>
    private StockHold()
    {
    }

    private StockHold(Guid id, Guid cartId, int quantity, DateTimeOffset expiresAt)
        : base(id)
    {
        CartId = cartId;
        Quantity = quantity;
        ExpiresAt = expiresAt;
    }

    public Guid CartId { get; private set; }

    public int Quantity { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    internal static StockHold Create(Guid cartId, int quantity, DateTimeOffset expiresAt) =>
        new(NewId(), cartId, quantity, expiresAt);

    /// <summary>A hold counts against availability until the moment it expires.</summary>
    public bool IsActiveAt(DateTimeOffset now) => ExpiresAt > now;
}
