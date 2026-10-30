namespace Mercato.Domain.Common;

/// <summary>
/// A monetary amount with its currency (TR-44, BR-13). Value object: compared by value, immutable.
/// V1 is single-currency (EUR); the currency is stored with the amount so that adding a second
/// currency later is a data change, not a schema change.
/// </summary>
public sealed record Money
{
    public const string DefaultCurrency = "EUR";

    private const int CurrencyCodeLength = 3;

    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        if (currency.Length != CurrencyCodeLength)
        {
            throw new ArgumentException("Currency must be a three-letter ISO 4217 code.", nameof(currency));
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException("Amount must not have more than two decimal places.", nameof(amount));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero => new(0m, DefaultCurrency);

    public static Money Euro(decimal amount) => new(amount, DefaultCurrency);

    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Add(right);
    }

    public static Money operator *(Money money, int quantity)
    {
        ArgumentNullException.ThrowIfNull(money);
        return money.Multiply(quantity);
    }

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(quantity);
        return new Money(Amount * quantity, Currency);
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cannot combine {Currency} with {other.Currency}.");
        }
    }
}
