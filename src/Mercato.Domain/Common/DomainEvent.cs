namespace Mercato.Domain.Common;

/// <summary>
/// Something that happened inside an aggregate and that other parts of the monolith may react
/// to (for example <c>OrderPlaced</c>). Concrete events are immutable records deriving from this
/// type. Domain events stay in-process; integration events for other services live in
/// <c>Mercato.Contracts</c> and are produced from these by the Application layer.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>Unique id of this occurrence; lets consumers detect duplicates (TR-31).</summary>
    public Guid EventId { get; } = Guid.CreateVersion7();
}
