using Mercato.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>Maps the <see cref="Notification"/> aggregate to the notifications.Notifications table.</summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Notifications", "notifications");

        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();

        // The recipient is a user (Identity module, Phase 4): an id, no foreign key. Indexed
        // because a user's recent notifications are looked up by it.
        builder.Property(notification => notification.UserId).IsRequired();
        builder.HasIndex(notification => notification.UserId);

        builder.Property(notification => notification.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(Notification.MessageMaxLength).IsRequired();
        builder.Property(notification => notification.CreatedAt).IsRequired();

        // Optional: stays NULL until the message has been sent.
        builder.Property(notification => notification.SentAt);
    }
}
