using Mercato.Domain.Common;

namespace Mercato.Domain.Ordering;

/// <summary>
/// What <see cref="Order.Place"/> needs to know about one purchased product. Lets an order be
/// created from a cart now and from other sources later without depending on the cart.
/// </summary>
public sealed record OrderLine(Guid ProductId, Guid VendorId, string ProductName, Money UnitPrice, int Quantity);
