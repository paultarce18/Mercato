using System.Linq.Expressions;
using Mercato.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercato.Infrastructure.Persistence.Configurations;

/// <summary>
/// Shared mapping for the <see cref="Money"/> value object (TR-44), so that every money property
/// in the database looks the same and the rule is written once.
/// </summary>
internal static class MoneyMappingExtensions
{
    /// <summary>
    /// Stores a <see cref="Money"/> property as two NOT NULL columns of the owner's table:
    /// <c>{columnPrefix}Amount decimal(18,2)</c> and <c>{columnPrefix}Currency nchar(3)</c>.
    /// </summary>
    public static EntityTypeBuilder<TOwner> OwnsMoney<TOwner>(
        this EntityTypeBuilder<TOwner> builder,
        Expression<Func<TOwner, Money?>> navigation,
        string columnPrefix)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnPrefix);

        builder.OwnsOne(
            navigation,
            money =>
            {
                money.Property(value => value.Amount)
                    .HasColumnName(columnPrefix + "Amount")
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                money.Property(value => value.Currency)
                    .HasColumnName(columnPrefix + "Currency")
                    .HasMaxLength(3)
                    .IsFixedLength()
                    .IsRequired();
            });

        // An owned object is optional by default; money is always present, so the columns are NOT NULL.
        builder.Navigation(navigation).IsRequired();

        return builder;
    }
}
