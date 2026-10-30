using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>Checkout was attempted on a cart without items. Mapped to HTTP 409.</summary>
public sealed class EmptyCartException : MercatoException
{
    public EmptyCartException(Guid cartId)
        : base($"Cart {cartId} is empty.")
    {
        CartId = cartId;
    }

    public Guid CartId { get; }
}
