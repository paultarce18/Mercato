namespace Mercato.Domain.Common;

/// <summary>
/// Root of all domain exceptions (GeneralRules EXC-02/03). A domain exception means a business
/// rule was violated by a valid request (insufficient stock, invalid state transition); it is
/// never used for programming errors, which stay <see cref="InvalidOperationException"/> or
/// <see cref="ArgumentException"/>. The API maps derived types to HTTP status codes
/// (ARCHITECTURE.md §9).
/// </summary>
public abstract class MercatoException : Exception
{
    protected MercatoException(string message)
        : base(message)
    {
    }

    protected MercatoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
