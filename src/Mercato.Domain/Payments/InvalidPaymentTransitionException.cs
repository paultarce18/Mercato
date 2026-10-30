using Mercato.Domain.Common;

namespace Mercato.Domain.Payments;

/// <summary>
/// The provider reported an outcome that contradicts the one already recorded, for example
/// "declined" for a payment that succeeded. Mapped to HTTP 409.
/// </summary>
public sealed class InvalidPaymentTransitionException : MercatoException
{
    public InvalidPaymentTransitionException(Guid paymentId, PaymentStatus from, PaymentStatus to)
        : base($"Payment {paymentId} cannot go from {from} to {to}.")
    {
        PaymentId = paymentId;
        From = from;
        To = to;
    }

    public Guid PaymentId { get; }

    public PaymentStatus From { get; }

    public PaymentStatus To { get; }
}
