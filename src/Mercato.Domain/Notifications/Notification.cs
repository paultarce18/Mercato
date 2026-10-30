using Mercato.Domain.Common;

namespace Mercato.Domain.Notifications;

/// <summary>
/// A message to a user about something that happened (FR-NO-01). V1 delivers it as an e-mail
/// written to the log (FR-NO-02). The notification is created when the event is received and
/// marked sent once delivered, so a consumer that crashes in between can retry. The user is
/// referenced by id only (TR-12).
/// </summary>
public sealed class Notification : AggregateRoot
{
    public const int MessageMaxLength = 1000;

    /// <summary>Used by EF Core when materialising a notification from the database.</summary>
    private Notification()
    {
        Message = string.Empty;
    }

    private Notification(Guid id, Guid userId, NotificationType type, string message, DateTimeOffset createdAt)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Message = message;
        CreatedAt = createdAt;
    }

    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Message { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Null until the message has been delivered.</summary>
    public DateTimeOffset? SentAt { get; private set; }

    public static Notification Create(Guid userId, NotificationType type, string message, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(message.Length, MessageMaxLength, nameof(message));
        return new Notification(NewId(), Guard.NotEmpty(userId, nameof(userId)), type, message.Trim(), createdAt);
    }

    /// <summary>Records delivery. Marking an already sent notification again keeps the first time.</summary>
    public void MarkSent(DateTimeOffset sentAt) => SentAt ??= sentAt;
}
