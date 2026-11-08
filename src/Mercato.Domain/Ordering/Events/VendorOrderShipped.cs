using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering.Events;

public sealed record VendorOrderShipped(Guid OrderId, Guid VendorOrderId) : DomainEvent;
