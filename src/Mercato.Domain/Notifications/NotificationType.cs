namespace Mercato.Domain.Notifications;

/// <summary>The moments at which a customer is notified (FR-NO-01).</summary>
public enum NotificationType
{
    OrderPlaced,
    PaymentSucceeded,
    PaymentDeclined,
    OrderShipped,
    OrderDelivered,
}
