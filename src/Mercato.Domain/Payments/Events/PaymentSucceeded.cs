using Mercato.Domain.Common;

namespace Mercato.Domain.Payments.Events;

public sealed record PaymentSucceeded(Guid PaymentId, Guid OrderId) : DomainEvent;
