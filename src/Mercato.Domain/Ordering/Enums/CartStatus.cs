namespace Mercato.Domain.Ordering.Enums;

public enum CartStatus
{
    /// <summary>The customer's current cart; at most one per customer (FR-CA-01).</summary>
    Active,

    /// <summary>Turned into an order; kept as a record and never reused.</summary>
    CheckedOut,
}
