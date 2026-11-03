using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>
/// Checkout found an item whose stock hold has expired; the customer must add it again (BR-06).
/// Mapped to HTTP 409.
/// </summary>
public sealed class CartItemHoldExpiredException : MercatoException
{
    public CartItemHoldExpiredException(Guid cartId, Guid productId)
        : base($"The stock hold for product {productId} in cart {cartId} has expired.")
    {
        CartId = cartId;
        ProductId = productId;
    }

    public Guid CartId { get; }

    public Guid ProductId { get; }
}
