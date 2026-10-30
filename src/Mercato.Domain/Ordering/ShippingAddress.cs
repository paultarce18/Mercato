namespace Mercato.Domain.Ordering;

/// <summary>Where an order is delivered. Value object: compared by value, immutable.</summary>
public sealed record ShippingAddress
{
    public const int RecipientMaxLength = 100;
    public const int StreetMaxLength = 200;
    public const int PostalCodeMaxLength = 20;
    public const int CityMaxLength = 100;
    public const int CountryMaxLength = 100;

    public ShippingAddress(string recipient, string street, string postalCode, string city, string country)
    {
        Recipient = Validate(recipient, RecipientMaxLength, nameof(recipient));
        Street = Validate(street, StreetMaxLength, nameof(street));
        PostalCode = Validate(postalCode, PostalCodeMaxLength, nameof(postalCode));
        City = Validate(city, CityMaxLength, nameof(city));
        Country = Validate(country, CountryMaxLength, nameof(country));
    }

    public string Recipient { get; }

    public string Street { get; }

    public string PostalCode { get; }

    public string City { get; }

    public string Country { get; }

    private static string Validate(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Length, maxLength, parameterName);
        return value.Trim();
    }
}
