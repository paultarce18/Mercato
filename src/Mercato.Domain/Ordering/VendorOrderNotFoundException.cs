using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>The order has no vendor order with the given id. Mapped to HTTP 404.</summary>
public sealed class VendorOrderNotFoundException : MercatoException
{
    public VendorOrderNotFoundException(Guid orderId, Guid vendorOrderId)
        : base($"Order {orderId} has no vendor order {vendorOrderId}.")
    {
        OrderId = orderId;
        VendorOrderId = vendorOrderId;
    }

    public Guid OrderId { get; }

    public Guid VendorOrderId { get; }
}
