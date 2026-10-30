using Mercato.Domain.Common;
using Mercato.Domain.Ordering.Events;

namespace Mercato.Domain.Ordering;

/// <summary>
/// A customer's purchase, split into one <see cref="VendorOrder"/> per vendor (FR-OR-03), with
/// prices frozen at checkout (BR-04). Owns the lifecycle (FR-OR-07):
/// <code>
/// Placed -> Paid -> (each VendorOrder: Pending -> Shipped -> Delivered) -> Completed
///    \-> Rejected
/// </code>
/// Every transition raises a domain event; any other transition throws
/// <see cref="InvalidOrderTransitionException"/>.
/// </summary>
public sealed class Order : AggregateRoot
{
    public const int OrderNumberMaxLength = 30;

    private readonly List<VendorOrder> vendorOrders = [];

    /// <summary>Used by EF Core when materialising an order from the database.</summary>
    private Order()
    {
        OrderNumber = string.Empty;
        ShippingAddress = null!; // Owned type; EF Core always fills it from the database.
        Total = Money.Zero;
    }

    private Order(Guid id, Guid customerId, string orderNumber, ShippingAddress shippingAddress, DateTimeOffset placedAt)
        : base(id)
    {
        CustomerId = customerId;
        OrderNumber = orderNumber;
        ShippingAddress = shippingAddress;
        PlacedAt = placedAt;
        Status = OrderStatus.Placed;
        Total = Money.Zero;
    }

    public Guid CustomerId { get; private set; }

    /// <summary>Human-facing number, generated outside the domain (TR-45).</summary>
    public string OrderNumber { get; private set; }

    public OrderStatus Status { get; private set; }

    public ShippingAddress ShippingAddress { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    /// <summary>Sum of all vendor order subtotals at checkout (FR-OR-08; V1 has no shipping fee or tax).</summary>
    public Money Total { get; private set; }

    public IReadOnlyCollection<VendorOrder> VendorOrders => vendorOrders.AsReadOnly();

    /// <summary>Creates a placed order and splits the lines into one vendor order per vendor.</summary>
    public static Order Place(
        Guid customerId,
        string orderNumber,
        ShippingAddress shippingAddress,
        IReadOnlyCollection<OrderLine> lines,
        DateTimeOffset placedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(orderNumber.Length, OrderNumberMaxLength, nameof(orderNumber));
        ArgumentNullException.ThrowIfNull(shippingAddress);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentOutOfRangeException.ThrowIfZero(lines.Count, nameof(lines));

        var order = new Order(NewId(), Guard.NotEmpty(customerId, nameof(customerId)), orderNumber.Trim(), shippingAddress, placedAt);

        foreach (IGrouping<Guid, OrderLine> vendorLines in lines.GroupBy(line => line.VendorId))
        {
            order.vendorOrders.Add(VendorOrder.Create(vendorLines.Key, vendorLines));
        }

        order.Total = order.vendorOrders.Aggregate(Money.Zero, (sum, vendorOrder) => sum + vendorOrder.Subtotal);
        order.Raise(new OrderPlaced(order.Id, customerId));
        return order;
    }

    /// <summary>The payment provider reported success.</summary>
    public void MarkPaid()
    {
        MoveTo(OrderStatus.Placed, OrderStatus.Paid);
        Raise(new OrderPaid(Id));
    }

    /// <summary>The payment provider declined. The caller releases the reservations (FR-PA-05).</summary>
    public void Reject()
    {
        MoveTo(OrderStatus.Placed, OrderStatus.Rejected);
        Raise(new OrderRejected(Id));
    }

    /// <summary>A vendor shipped its part (FR-SH-02). Only a paid order can be shipped.</summary>
    /// <exception cref="VendorOrderNotFoundException">The order has no such vendor order.</exception>
    public void MarkVendorOrderShipped(Guid vendorOrderId)
    {
        VendorOrder vendorOrder = FindVendorOrder(vendorOrderId);
        EnsureStatus(OrderStatus.Paid, nameof(VendorOrderStatus.Shipped));

        vendorOrder.MarkShipped();
        Raise(new VendorOrderShipped(Id, vendorOrderId));
    }

    /// <summary>
    /// The courier delivered a vendor's part (FR-SH-03). When the last vendor order is delivered
    /// the whole order becomes <see cref="OrderStatus.Completed"/>.
    /// </summary>
    /// <exception cref="VendorOrderNotFoundException">The order has no such vendor order.</exception>
    public void MarkVendorOrderDelivered(Guid vendorOrderId)
    {
        VendorOrder vendorOrder = FindVendorOrder(vendorOrderId);
        EnsureStatus(OrderStatus.Paid, nameof(VendorOrderStatus.Delivered));

        vendorOrder.MarkDelivered();
        Raise(new VendorOrderDelivered(Id, vendorOrderId));

        if (vendorOrders.All(candidate => candidate.Status == VendorOrderStatus.Delivered))
        {
            Status = OrderStatus.Completed;
        }
    }

    private VendorOrder FindVendorOrder(Guid vendorOrderId) =>
        vendorOrders.Find(candidate => candidate.Id == vendorOrderId)
        ?? throw new VendorOrderNotFoundException(Id, vendorOrderId);

    private void MoveTo(OrderStatus from, OrderStatus to)
    {
        EnsureStatus(from, to.ToString());
        Status = to;
    }

    private void EnsureStatus(OrderStatus required, string target)
    {
        if (Status != required)
        {
            throw new InvalidOrderTransitionException(nameof(Order), Id, Status.ToString(), target);
        }
    }
}
