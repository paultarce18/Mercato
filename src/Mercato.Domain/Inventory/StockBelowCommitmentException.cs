using Mercato.Domain.Common;

namespace Mercato.Domain.Inventory;

/// <summary>
/// The vendor tried to set stock on hand lower than the units already promised to customers
/// (reserved plus actively held), which would make the available stock negative (BR-05).
/// Mapped to HTTP 409.
/// </summary>
public sealed class StockBelowCommitmentException : MercatoException
{
    public StockBelowCommitmentException(Guid productId, int requested, int committed)
        : base($"Product {productId}: stock cannot be set to {requested}, {committed} unit(s) are already reserved or held.")
    {
        ProductId = productId;
        Requested = requested;
        Committed = committed;
    }

    public Guid ProductId { get; }

    public int Requested { get; }

    public int Committed { get; }
}
