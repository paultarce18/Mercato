using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>
/// The part of an <see cref="Order"/> that one vendor fulfils (FR-OR-03). Part of the order
/// aggregate: its status changes only through the order, which guards the lifecycle.
/// </summary>
public sealed class VendorOrder : Entity
{
    private readonly List<OrderItem> items = [];

    /// <summary>Used by EF Core when materialising a vendor order from the database.</summary>
    private VendorOrder()
    {
    }

    private VendorOrder(Guid id, Guid vendorId, IEnumerable<OrderLine> lines)
        : base(id)
    {
        VendorId = vendorId;
        Status = VendorOrderStatus.Pending;
        items.AddRange(lines.Select(OrderItem.Create));
    }

    /// <summary>The vendor that fulfils this part; tenancy key (BR-02, TR-15).</summary>
    public Guid VendorId { get; private set; }

    public VendorOrderStatus Status { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => items.AsReadOnly();

    public Money Subtotal => items.Aggregate(Money.Zero, (sum, item) => sum + item.LineTotal);

    internal static VendorOrder Create(Guid vendorId, IEnumerable<OrderLine> lines) =>
        new(NewId(), Guard.NotEmpty(vendorId, nameof(vendorId)), lines);

    internal void MarkShipped() => MoveTo(VendorOrderStatus.Pending, VendorOrderStatus.Shipped);

    internal void MarkDelivered() => MoveTo(VendorOrderStatus.Shipped, VendorOrderStatus.Delivered);

    private void MoveTo(VendorOrderStatus from, VendorOrderStatus to)
    {
        if (Status != from)
        {
            throw new InvalidOrderTransitionException(nameof(VendorOrder), Id, Status.ToString(), to.ToString());
        }

        Status = to;
    }
}
