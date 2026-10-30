using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering.Events;

public sealed record VendorOrderDelivered(Guid OrderId, Guid VendorOrderId) : DomainEvent;
