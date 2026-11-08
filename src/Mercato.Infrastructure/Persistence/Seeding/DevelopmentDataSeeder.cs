using Mercato.Domain.Catalog;
using Mercato.Domain.Common;
using Mercato.Domain.Inventory;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Persistence.Seeding;

/// <summary>
/// Inserts demo data (2 vendors, 4 categories, 20 products with stock) for local development.
/// Called from Program.cs in the Development environment only; never runs in production.
/// </summary>
public static class DevelopmentDataSeeder
{
    /// <summary>Seeds the database once. Calling it again on a seeded database does nothing.</summary>
    public static async Task SeedAsync(MercatoDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);

        // Idempotent guard: if any vendor exists, the database was seeded before, so we stop.
        if (await db.Vendors.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        // 1. Vendors and categories are created through their factories, so the domain rules are enforced.
        var northwind = Vendor.Create("Northwind Sports", "sales@northwind.example");
        var casaVerde = Vendor.Create("Casa Verde Home", "hello@casaverde.example");

        var shoes = Category.Create("Shoes");
        var clothing = Category.Create("Clothing");
        var home = Category.Create("Home & Garden");
        var electronics = Category.Create("Electronics");

        db.AddRange(northwind, casaVerde, shoes, clothing, home, electronics);

        // 2. Northwind Sports: 10 products (SKU prefix NW). The Trail Gloves have 0 stock to test "out of stock" later.
        AddProduct(db, northwind, shoes, "Trail Runner", "Lightweight trail running shoe with grippy sole.", "NW-SHOE-001", 89.90m, 25);
        AddProduct(db, northwind, shoes, "City Sneaker", "Everyday sneaker with cushioned insole.", "NW-SHOE-002", 64.50m, 40);
        AddProduct(db, northwind, shoes, "Hiking Boot", "Waterproof leather boot for long hikes.", "NW-SHOE-003", 129.00m, 12);
        AddProduct(db, northwind, shoes, "Court Shoe", "Non-marking indoor court shoe.", "NW-SHOE-004", 74.99m, 18);
        AddProduct(db, northwind, clothing, "Running Shirt", "Breathable quick-dry shirt.", "NW-CLTH-001", 29.90m, 50);
        AddProduct(db, northwind, clothing, "Thermal Jacket", "Light insulated jacket for cold mornings.", "NW-CLTH-002", 109.00m, 15);
        AddProduct(db, northwind, clothing, "Sport Shorts", "Stretch shorts with zip pocket.", "NW-CLTH-003", 24.90m, 35);
        AddProduct(db, northwind, clothing, "Trail Gloves", "Windproof gloves for running and cycling.", "NW-CLTH-004", 19.90m, 0);
        AddProduct(db, northwind, electronics, "Sports Watch", "GPS watch with heart-rate sensor.", "NW-ELEC-001", 199.00m, 8);
        AddProduct(db, northwind, electronics, "Wireless Earbuds", "Sweat-resistant Bluetooth earbuds.", "NW-ELEC-002", 59.90m, 30);

        // 3. Casa Verde Home: 10 products (SKU prefix CV). The same product name may exist at another vendor; SKU only has to be unique per vendor.
        AddProduct(db, casaVerde, home, "Ceramic Plant Pot", "Glazed 20 cm pot with drainage hole.", "CV-HOME-001", 18.50m, 45);
        AddProduct(db, casaVerde, home, "Garden Tool Set", "Five-piece stainless steel hand tool set.", "CV-HOME-002", 34.90m, 22);
        AddProduct(db, casaVerde, home, "Herb Starter Kit", "Basil, mint and parsley with pots and soil.", "CV-HOME-003", 21.00m, 38);
        AddProduct(db, casaVerde, home, "Cotton Throw Blanket", "Soft woven throw, 130 x 170 cm.", "CV-HOME-004", 39.90m, 20);
        AddProduct(db, casaVerde, home, "Scented Candle", "Soy wax candle, cedar and lavender.", "CV-HOME-005", 14.90m, 60);
        AddProduct(db, casaVerde, home, "Bamboo Storage Basket", "Foldable basket with handles.", "CV-HOME-006", 27.50m, 16);
        AddProduct(db, casaVerde, electronics, "Smart Plug", "Wi-Fi plug with energy monitoring.", "CV-ELEC-001", 16.90m, 55);
        AddProduct(db, casaVerde, electronics, "LED Desk Lamp", "Dimmable lamp with USB charging port.", "CV-ELEC-002", 44.90m, 14);
        AddProduct(db, casaVerde, electronics, "Robot Vacuum", "Self-charging robot vacuum cleaner.", "CV-ELEC-003", 249.00m, 5);
        AddProduct(db, casaVerde, clothing, "Linen Apron", "Adjustable linen kitchen apron.", "CV-CLTH-001", 22.00m, 28);

        // 4. One SaveChanges = one transaction: either all demo data is stored or none of it.
        await db.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a Product (Catalog module) and its InventoryItem (Inventory module).
    /// The two modules are linked by id only (TR-12), so the stock row just stores the product id.
    /// </summary>
    private static void AddProduct(
        MercatoDbContext db,
        Vendor vendor,
        Category category,
        string name,
        string description,
        string sku,
        decimal priceInEuro,
        int onHand)
    {
        var product = Product.Create(vendor.Id, category.Id, name, description, sku, Money.Euro(priceInEuro), imageUrl: null);
        var stock = InventoryItem.Create(product.Id, vendor.Id, onHand);

        db.AddRange(product, stock);
    }
}
