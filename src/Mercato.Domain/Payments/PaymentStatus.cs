namespace Mercato.Domain.Payments;

public enum PaymentStatus
{
    /// <summary>Charge requested; the provider has not answered yet.</summary>
    Pending,

    Succeeded,

    Declined,
}
