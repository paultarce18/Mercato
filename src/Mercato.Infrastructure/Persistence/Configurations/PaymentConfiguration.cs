using Mercato.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>Maps the <see cref="Payment"/> aggregate to the payments.Payments table.</summary>
internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Payments", "payments");

        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();

        // The order lives in the Ordering module: an id, no foreign key (TR-12). Unique because V1
        // charges an order exactly once; this also makes a repeated webhook unable to create a
        // second payment row.
        builder.Property(payment => payment.OrderId).IsRequired();
        builder.HasIndex(payment => payment.OrderId).IsUnique();

        // Columns PaymentAmount and PaymentCurrency. The prefix cannot be empty, and a plain
        // "Amount" would collide with the property name inside Money.
        builder.OwnsMoney(payment => payment.Amount, "Payment");

        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Optional: no property is marked IsRequired, so the columns are nullable. The reference
        // is set when the provider confirms, the reason only when the payment is declined.
        builder.Property(payment => payment.ProviderReference).HasMaxLength(Payment.ProviderReferenceMaxLength);
        builder.Property(payment => payment.FailureReason).HasMaxLength(Payment.FailureReasonMaxLength);

        builder.Property(payment => payment.CreatedAt).IsRequired();
    }
}
