using Mercato.Domain.Common;

namespace Mercato.Domain.Inventory;

/// <summary>
/// More units were requested than are available (BR-05). Mapped to HTTP 409 by the API.
/// </summary>
public sealed class InsufficientStockException : MercatoException
{
    public InsufficientStockException(Guid productId, int requested, int available)
        : base($"Product {productId}: {requested} unit(s) requested but only {available} available.")
    {
        ProductId = productId;
        Requested = requested;
        Available = available;
    }

    public Guid ProductId { get; }

    public int Requested { get; }

    public int Available { get; }
}
