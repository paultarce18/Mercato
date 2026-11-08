using Mercato.Domain.Common;

namespace Mercato.Domain.Inventory;

/// <summary>
/// A cart has no active hold on a product: it never had one, or it has expired (BR-06). An
/// expired hold cannot be checked out; the customer must add the item again. Mapped to HTTP 409.
/// </summary>
public sealed class StockHoldNotFoundException : MercatoException
{
    public StockHoldNotFoundException(Guid productId, Guid cartId)
        : base($"Cart {cartId} has no active stock hold on product {productId}.")
    {
        ProductId = productId;
        CartId = cartId;
    }

    public Guid ProductId { get; }

    public Guid CartId { get; }
}
