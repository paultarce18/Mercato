using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering.Events;

public sealed record OrderPaid(Guid OrderId) : DomainEvent;
