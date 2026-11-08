using Mercato.Domain.Common;

namespace Mercato.Domain.Catalog;

/// <summary>
/// A seller on the marketplace and the tenant of the system: every vendor-owned row elsewhere
/// carries this aggregate's id as <c>VendorId</c> (BR-02, TR-15).
/// Created by an Admin (FR-VN-01). Deactivation hides the vendor and is reversible; vendors are
/// never deleted (BR-12).
/// </summary>
public sealed class Vendor : AggregateRoot
{
    public const int NameMaxLength = 100;
    public const int ContactEmailMaxLength = 254;

    /// <summary>Used by EF Core when materialising a vendor from the database.</summary>
    private Vendor()
    {
        Name = string.Empty;
        ContactEmail = string.Empty;
    }

    private Vendor(Guid id, string name, string contactEmail)
        : base(id)
    {
        Name = name;
        ContactEmail = contactEmail;
        IsActive = true;
    }

    public string Name { get; private set; }

    public string ContactEmail { get; private set; }

    public bool IsActive { get; private set; }

    public static Vendor Create(string name, string contactEmail) => new(NewId(), ValidateName(name), ValidateContactEmail(contactEmail));

    public void Update(string name, string contactEmail)
    {
        Name = ValidateName(name);
        ContactEmail = ValidateContactEmail(contactEmail);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, NameMaxLength, nameof(name));
        return name.Trim();
    }

    private static string ValidateContactEmail(string contactEmail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contactEmail);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(contactEmail.Length, ContactEmailMaxLength, nameof(contactEmail));
        return contactEmail.Trim();
    }
}
