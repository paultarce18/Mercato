using Mercato.Domain.Common;
using Mercato.Domain.Shipping.Events;

namespace Mercato.Domain.Shipping;

/// <summary>
/// The parcel for one vendor order, created when the vendor marks it shipped (FR-SH-02) and
/// delivered when the courier reports it (FR-SH-03). V1 knows only these two states. A repeated
/// delivery report is ignored. Carries <see cref="VendorId"/> because it is vendor-owned
/// (TR-15); the vendor order is referenced by id only (TR-12).
/// </summary>
public sealed class Shipment : AggregateRoot
{
    public const int TrackingNumberMaxLength = 50;

    /// <summary>Used by EF Core when materialising a shipment from the database.</summary>
    private Shipment()
    {
        TrackingNumber = string.Empty;
    }

    private Shipment(Guid id, Guid vendorOrderId, Guid vendorId, string trackingNumber, DateTimeOffset shippedAt)
        : base(id)
    {
        VendorOrderId = vendorOrderId;
        VendorId = vendorId;
        TrackingNumber = trackingNumber;
        ShippedAt = shippedAt;
        Status = ShipmentStatus.Created;
    }

    public Guid VendorOrderId { get; private set; }

    public Guid VendorId { get; private set; }

    /// <summary>Generated outside the domain when the shipment is created.</summary>
    public string TrackingNumber { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public DateTimeOffset ShippedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public static Shipment Create(Guid vendorOrderId, Guid vendorId, string trackingNumber, DateTimeOffset shippedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(trackingNumber.Length, TrackingNumberMaxLength, nameof(trackingNumber));
        return new Shipment(
            NewId(),
            Guard.NotEmpty(vendorOrderId, nameof(vendorOrderId)),
            Guard.NotEmpty(vendorId, nameof(vendorId)),
            trackingNumber.Trim(),
            shippedAt);
    }

    /// <summary>The courier reported delivery. A repeated report is ignored.</summary>
    public void MarkDelivered(DateTimeOffset deliveredAt)
    {
        if (Status == ShipmentStatus.Delivered)
        {
            return;
        }

        Status = ShipmentStatus.Delivered;
        DeliveredAt = deliveredAt;
        Raise(new ShipmentDelivered(Id, VendorOrderId));
    }
}
