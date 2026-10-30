namespace Mercato.Domain.Common;

/// <summary>
/// An entity that is the single entry point to a group of related entities (its aggregate),
/// guards their invariants and records the domain events that its methods raise.
/// Repositories exist only for aggregate roots (TR-13). Events are dispatched by
/// Infrastructure after the aggregate has been saved (TR-24).
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> domainEvents = [];

    /// <summary>Used by EF Core when materialising an aggregate from the database.</summary>
    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    /// <summary>Events raised since the aggregate was loaded or last cleared, in order.</summary>
    public IReadOnlyCollection<DomainEvent> DomainEvents => domainEvents.AsReadOnly();

    /// <summary>Called by Infrastructure once the events have been dispatched.</summary>
    public void ClearDomainEvents() => domainEvents.Clear();

    protected void Raise(DomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        domainEvents.Add(domainEvent);
    }
}
