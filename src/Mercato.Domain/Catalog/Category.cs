using Mercato.Domain.Common;

namespace Mercato.Domain.Catalog;

/// <summary>
/// A node in the flat list of product categories managed by the Admin (FR-CT-06).
/// Deactivation hides the category and is reversible; categories are never deleted (BR-12).
/// </summary>
public sealed class Category : AggregateRoot
{
    public const int NameMaxLength = 100;

    /// <summary>Used by EF Core when materialising a category from the database.</summary>
    private Category()
    {
        Name = string.Empty;
    }

    private Category(Guid id, string name)
        : base(id)
    {
        Name = name;
        IsActive = true;
    }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public static Category Create(string name) => new(NewId(), ValidateName(name));

    public void Rename(string name) => Name = ValidateName(name);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string ValidateName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, NameMaxLength, nameof(name));
        return name.Trim();
    }
}
