namespace Mercato.Domain.Ordering;

/// <summary>V1 order lifecycle (REQUIREMENTS-V1 §5).</summary>
public enum OrderStatus
{
    /// <summary>Checked out, waiting for the payment result.</summary>
    Placed,

    /// <summary>Payment succeeded; vendors can ship.</summary>
    Paid,

    /// <summary>Payment declined; reservations are released.</summary>
    Rejected,

    /// <summary>Every vendor order has been delivered.</summary>
    Completed,
}
