namespace Mercato.Domain.Common;

/// <summary>
/// Argument checks that the BCL does not provide, shared by the aggregates so each one does not
/// carry its own copy.
/// </summary>
public static class Guard
{
    /// <summary>Rejects <see cref="Guid.Empty"/>, which is never a valid reference to another aggregate.</summary>
    public static Guid NotEmpty(Guid id, string parameterName) =>
        id == Guid.Empty
            ? throw new ArgumentException("A reference id must not be empty.", parameterName)
            : id;
}
