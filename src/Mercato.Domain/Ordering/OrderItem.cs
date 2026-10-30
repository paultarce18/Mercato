using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>
/// One purchased product inside a <see cref="VendorOrder"/>. Name and unit price are the values
/// at checkout and never change afterwards (BR-04).
/// </summary>
public sealed class OrderItem : Entity
{
    /// <summary>Used by EF Core when materialising an item from the database.</summary>
    private OrderItem()
    {
        ProductName = string.Empty;
        UnitPrice = Money.Zero;
    }

    private OrderItem(Guid id, OrderLine line)
        : base(id)
    {
        ProductId = line.ProductId;
        ProductName = line.ProductName;
        UnitPrice = line.UnitPrice;
        Quantity = line.Quantity;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; }

    public Money UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice * Quantity;

    internal static OrderItem Create(OrderLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        Guard.NotEmpty(line.ProductId, nameof(line));
        ArgumentException.ThrowIfNullOrWhiteSpace(line.ProductName, nameof(line));
        ArgumentNullException.ThrowIfNull(line.UnitPrice, nameof(line));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(line.Quantity, nameof(line));
        return new OrderItem(NewId(), line);
    }
}
