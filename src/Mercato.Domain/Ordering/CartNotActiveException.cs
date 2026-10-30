using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>
/// A cart that has already been checked out was changed or checked out again, for example by a
/// double click on "Pay". Mapped to HTTP 409.
/// </summary>
public sealed class CartNotActiveException : MercatoException
{
    public CartNotActiveException(Guid cartId)
        : base($"Cart {cartId} has already been checked out.")
    {
        CartId = cartId;
    }

    public Guid CartId { get; }
}
