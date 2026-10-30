using Mercato.Domain.Common;
using Mercato.Domain.Payments.Events;

namespace Mercato.Domain.Payments;

/// <summary>
/// The charge for one order with the simulated provider (FR-PA-01). The provider answers
/// asynchronously by webhook (FR-PA-04), so a payment starts <see cref="PaymentStatus.Pending"/>
/// and ends <see cref="PaymentStatus.Succeeded"/> or <see cref="PaymentStatus.Declined"/>.
/// Webhooks can arrive twice: repeating the outcome that is already recorded does nothing, while
/// a contradicting outcome throws <see cref="InvalidPaymentTransitionException"/>.
/// The order is referenced by id only (TR-12).
/// </summary>
public sealed class Payment : AggregateRoot
{
    public const int ProviderReferenceMaxLength = 100;
    public const int FailureReasonMaxLength = 200;

    /// <summary>Used by EF Core when materialising a payment from the database.</summary>
    private Payment()
    {
        Amount = Money.Zero;
    }

    private Payment(Guid id, Guid orderId, Money amount, DateTimeOffset createdAt)
        : base(id)
    {
        OrderId = orderId;
        Amount = amount;
        CreatedAt = createdAt;
        Status = PaymentStatus.Pending;
    }

    public Guid OrderId { get; private set; }

    public Money Amount { get; private set; }

    public PaymentStatus Status { get; private set; }

    /// <summary>The provider's transaction id; set when the payment succeeds.</summary>
    public string? ProviderReference { get; private set; }

    /// <summary>Why the provider declined; set when the payment is declined.</summary>
    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Payment Create(Guid orderId, Money amount, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(amount);
        ArgumentOutOfRangeException.ThrowIfZero(amount.Amount, nameof(amount));
        return new Payment(NewId(), Guard.NotEmpty(orderId, nameof(orderId)), amount, createdAt);
    }

    /// <summary>The provider reported success. A repeated report is ignored.</summary>
    public void MarkSucceeded(string providerReference)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerReference);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(providerReference.Length, ProviderReferenceMaxLength, nameof(providerReference));

        if (Status == PaymentStatus.Succeeded)
        {
            return;
        }

        EnsurePending(PaymentStatus.Succeeded);
        Status = PaymentStatus.Succeeded;
        ProviderReference = providerReference.Trim();
        Raise(new PaymentSucceeded(Id, OrderId));
    }

    /// <summary>The provider declined. A repeated report is ignored.</summary>
    public void MarkDeclined(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(reason.Length, FailureReasonMaxLength, nameof(reason));

        if (Status == PaymentStatus.Declined)
        {
            return;
        }

        EnsurePending(PaymentStatus.Declined);
        Status = PaymentStatus.Declined;
        FailureReason = reason.Trim();
        Raise(new PaymentDeclined(Id, OrderId));
    }

    private void EnsurePending(PaymentStatus target)
    {
        if (Status != PaymentStatus.Pending)
        {
            throw new InvalidPaymentTransitionException(Id, Status, target);
        }
    }
}
