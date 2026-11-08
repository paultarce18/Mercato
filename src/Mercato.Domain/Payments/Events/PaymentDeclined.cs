using Mercato.Domain.Common;

namespace Mercato.Domain.Payments.Events;

public sealed record PaymentDeclined(Guid PaymentId, Guid OrderId) : DomainEvent;
