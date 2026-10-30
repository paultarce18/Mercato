using Mercato.Domain.Common;

namespace Mercato.Domain.Shipping.Events;

public sealed record ShipmentDelivered(Guid ShipmentId, Guid VendorOrderId) : DomainEvent;
