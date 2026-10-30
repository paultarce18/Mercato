using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering.Events;

public sealed record OrderPlaced(Guid OrderId, Guid CustomerId) : DomainEvent;
