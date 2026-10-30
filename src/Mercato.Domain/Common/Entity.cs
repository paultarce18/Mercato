namespace Mercato.Domain.Common;

/// <summary>
/// Base class for everything that has an identity and a lifecycle.
/// Two entities are equal when they are of the same type and carry the same <see cref="Id"/>;
/// their other values do not matter for equality.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    /// <summary>Used by EF Core when materialising an entity from the database.</summary>
    protected Entity()
    {
    }

    protected Entity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("An entity id must not be empty.", nameof(id));
        }

        Id = id;
    }

    /// <summary>Primary key. Version-7 GUIDs are time-ordered, so they index well (TR-45).</summary>
    public Guid Id { get; private set; }

    public static bool operator ==(Entity? left, Entity? right) => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);

    /// <summary>Creates a new, time-ordered id for an entity that is being created now.</summary>
    protected static Guid NewId() => Guid.CreateVersion7();

    public bool Equals(Entity? other) =>
        other is not null
        && (ReferenceEquals(this, other) || (GetType() == other.GetType() && Id == other.Id));

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
