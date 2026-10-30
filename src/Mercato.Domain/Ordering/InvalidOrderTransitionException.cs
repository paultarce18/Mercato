using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>
/// A status change that the order lifecycle does not allow (FR-OR-07), for example shipping an
/// unpaid order. Mapped to HTTP 409.
/// </summary>
public sealed class InvalidOrderTransitionException : MercatoException
{
    public InvalidOrderTransitionException(string subject, Guid id, string from, string to)
        : base($"{subject} {id} cannot go from {from} to {to}.")
    {
        Subject = subject;
        Id = id;
        From = from;
        To = to;
    }

    /// <summary>What was being changed, "Order" or "VendorOrder".</summary>
    public string Subject { get; }

    public Guid Id { get; }

    public string From { get; }

    public string To { get; }
}
