using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering.Events;

public sealed record OrderRejected(Guid OrderId) : DomainEvent;
