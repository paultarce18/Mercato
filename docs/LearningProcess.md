# Mercato – Learning Notes

> Explanations captured during the phases, for re-reading later and for interview preparation.
> Append a section per topic; keep each one self-contained.

---

## 1. Where the layers and modules come from (Phase 1)

**Question:** How does one come up with Domain / Application / Infrastructure / Api / Worker /
Contracts and the modules (Identity, Catalog, Inventory, Ordering, Payments, Shipping,
Notifications)? Is it a standard? How is a use case turned into this structure?

**Short answer:** the structure was not invented for Mercato. It is two standard cuts that the
industry converged on over ~20 years, applied to a problem of a familiar shape ("business
application with a database, an API and rules that must stay correct"). The project-specific
thinking is only *where the boundaries lie* and *what the rules are*; the rest is pattern.

### 1.1 Cut 1 — horizontal, by "reason to change": the layers

Origin: **Clean Architecture** (Robert C. Martin, 2012) = **Onion Architecture** (Jeffrey
Palermo, 2008) = **Hexagonal / Ports & Adapters** (Alistair Cockburn, 2005). All three say one
thing: *business rules must not depend on technical details.*

The questions that drive the cut:

| Question | Answer → layer |
|---|---|
| If we swapped SQL Server for PostgreSQL, what should **not** need to change? | **Domain** (entities, rules) and **Application** (use cases). Whatever would change is **Infrastructure**. |
| If we replaced the REST API with gRPC or a CLI, what should not change? | Everything except the entry point. So **Api** / **Worker** are thin outer shells (composition roots). |
| Can I test the rule "an order cannot ship before payment" without a database? | If not, the rule sits in the wrong layer. |

The dependency direction (outer → inner, never inner → outer) is just the enforcement of these
answers, done by the compiler through project references.

**Contracts** is a small extra from the microservices world: when two processes exchange
messages, the message shapes need a home that *both* can reference without either depending
on the other's internals.

### 1.2 Cut 2 — vertical, by "what changes together": the modules

Origin: **Domain-Driven Design** (Eric Evans, 2003), specifically **bounded contexts**, packaged
today as the **modular monolith**.

The questions that drive the cut:

- Which nouns and rules are always touched together? Product, price, category → Catalog.
  Order, cart, order items → Ordering. They rarely change in the same ticket as each other.
- Does the same word mean different things in different places? A "product" in the catalog has
  a description and images; a "product" in a shipment is a SKU and a weight. Two meanings →
  two modules, each with its own type.
- Who asks for changes? Vendors drive catalog changes; finance drives payments; operations
  drives shipping. Different stakeholders → different modules.
- Could this become its own service one day, or a bought component? Payments and Shipping:
  classic yes.

The layers are the same for every module; the modules are the same across every layer.
That is why the folder tree in `ARCHITECTURE.md` §5 is a grid.

### 1.3 From a use case to the structure — the method

Take one sentence from `REQUIREMENTS-V1.md` and walk it through four steps.

Example: **"Adding to cart places a timed hold on that quantity; held stock is not available
to others."** (FR-IN-02)

1. **Nouns → candidate entities.** Cart, product, quantity, hold, stock. Ask of each: does it
   have identity and a lifecycle? Cart yes, Hold yes, Stock (per product) yes. Quantity no — it
   is a value. → `Cart`, `StockHold`, `InventoryItem`.
2. **Verbs → use cases.** "Add to cart", "place hold", "release hold". Each use case becomes one
   Application handler, e.g. `AddItemToCartHandler`. That is what a button or an endpoint calls.
3. **Rules → domain methods.** "Held stock is not available to others" is a rule about
   `InventoryItem`: `inventoryItem.Hold(quantity, cartId, expiresAt)` throws if
   `available < quantity`. "Timed" means the hold carries an expiry — a property on `StockHold`.
   Rules live on the entity that owns the data they protect.
4. **What the use case needs from the outside → ports.** To do its job the handler must *load*
   the cart and the inventory item, *know the time* (for the expiry) and *save*. Each of those is
   a question to the outside world, so each becomes an interface in Application:
   `ICartRepository`, `IInventoryRepository`, `TimeProvider`, `IUnitOfWork`. Infrastructure
   answers them with EF Core and the system clock. The interfaces were not designed up front;
   the use case *told us* which ones it needs.

Then the module question: `Cart` belongs to Ordering, `InventoryItem` to Inventory. The
handler in Ordering needs to touch Inventory. Rule TR-12 says not through Inventory's entity
directly, so the use case needs one more port — `IStockReservationService` — which Inventory
implements. That interface exists *only because the use case crossed a module boundary*; it was
discovered, not planned.

Do this for five or six use cases and the folder tree in `ARCHITECTURE.md` §5 writes itself.
The aggregate list in TR-14 is the union of step 1 across the V1 requirements.

### 1.4 Why it looks like the details were "already known"

After the method has been applied a few times, the layering becomes a reflex and all the real
thinking goes into the two project-specific things: where the module boundaries lie, and what
the rules are. Those are the items in `ARCHITECTURE.md` marked as proposals to confirm.

### 1.5 How to make it stick

- Phase 2 is this method applied for real: take the V1 requirements one by one, derive entities
  and rules, and watch handlers and ports appear from the use cases rather than from a diagram.
- Reading: Vlad Khononov, *Learning Domain-Driven Design* (O'Reilly, 2021) — short and modern,
  covers bounded contexts and the layering together. Robert C. Martin, *Clean Architecture*
  (2017) — the classic for cut 1.
- A test to apply to any design: for every class ask **"what would make me change this
  file?"** If the answer contains two unrelated things ("the price rule changed" *and* "we moved
  to PostgreSQL"), the cut is in the wrong place.

### 1.6 Vocabulary

| Term | Meaning here |
|---|---|
| Layer | Horizontal cut by reason to change (Domain, Application, Infrastructure, entry points). |
| Module / bounded context | Vertical cut by what changes together (Catalog, Ordering, …). |
| Entity / aggregate | An object with identity and lifecycle; an aggregate is the entity that guards a group of them (`Order` guards `VendorOrder` and `OrderItem`). |
| Value object | Data without identity (a quantity, a money amount). |
| Use case / handler | One action a user or the system can take; one class in Application. |
| Port | An interface Application declares for something it needs from outside (repository, clock, payment gateway). |
| Adapter | The Infrastructure implementation of a port (EF Core repository, system clock). |
| Composition root | The entry-point project that wires ports to adapters (DI) — `Mercato.Api`, `Mercato.Worker`. |
| Contract | A message or DTO shape shared between processes (`Mercato.Contracts`). |

---

## 2. Where Entity, AggregateRoot and Money come from (Phase 2)

**Question:** I would never have come up with base classes like `Entity` and `AggregateRoot`.
How does one arrive at them, and how can I learn to do it? Why does everything have an `Id`?

**Short answer:** nobody invents them from first principles. They are **extracted** from
duplication, the way any base class is, and then named by the industry (Evans, *DDD*, 2003;
Vernon, *Implementing DDD*, 2013). Seeing the finished classes without the problem that
produced them is what makes them feel abstract.

### 2.1 The three rounds that produce the classes

1. **Plain classes.** `Vendor`, `Category`, `Product` each get their own `Id`, an
   empty-id check and a factory that generates a Guid. By the third class the duplication is
   obvious (K-08: extract on the third occurrence). The extracted part is `Entity`.
2. **Equality.** Two `Product` objects loaded separately from the DB are not `==` although
   they are the same row; `HashSet` and `Contains` misbehave. The fix, compare by type + id,
   is needed by every entity, so it moves into `Entity`.
3. **Events.** `cart.Checkout()` must lead to a payment and a notification, but the handler
   may not call four modules (TR-12). Instead the aggregate *records* "OrderPlaced happened"
   and others react after saving. The recorded list is `AggregateRoot`. Only aggregate roots
   announce things (an `OrderItem` never does), so it is a second class, not part of `Entity`.

`Money` follows the same path: `decimal Price`, `decimal UnitPrice`, `decimal Total` all need
the same rules (non-negative, two decimals, never add EUR to USD), so one type owns them.

### 2.2 Why everything has an Id

The business reason comes first: two things must be distinguishable even when their values
look the same (two vendors named "Müller"), and a thing stays *the same thing* when its
values change (a product after a price change). That is what "entity" means: identity outlives
values. The database primary key, `Equals` and references across modules (`Product.VendorId`)
are consequences of that. Things without identity (a `Money` amount, an address) are value
objects and carry no `Id`.

### 2.3 How to learn this

- **Write the naive version first.** When something feels repetitive or awkward, that is the
  signal; the pattern is only the name of the fix.
- **Ask "what would I lose if I deleted this class?"** Without `Entity`: `Id` and `Equals`
  copied into every class. Without `AggregateRoot`: no way to announce events without calling
  other modules. If no concrete loss can be named, the class should not exist (K-02).
- **Read one source:** Khononov, *Learning Domain-Driven Design*, ch. 6 ("Tackling Complex
  Business Logic"), after having touched `Vendor`.
- Most of the difficulty of this project is business logic (where stock holds live, what
  happens when a hold expires mid-checkout), not these ~150 lines of infrastructure.

### 2.4 Vocabulary

| Term | Meaning here |
|---|---|
| Entity | Object with identity that outlives its values; base class `Entity` provides `Id` and equality. |
| Aggregate root | Entity that guards a group of related entities and records domain events (`AggregateRoot`). |
| Value object | Data without identity, compared by value, immutable (`Money`, implemented as a `record`). |
| Domain event | Something that happened inside an aggregate, raised with `Raise(...)`, dispatched after save. |

---

## 3. What a `public sealed record` is (Phase 2)

**Question:** `public sealed record` classes are new to me. What are they and why are they used?

**Short answer:** a `record` is a class for which the compiler generates *value-based* equality, a
readable `ToString()` and copy-with-changes (`with`). `sealed` (nothing can inherit from it) and
`public` are independent keywords and mean what they always mean.

### 3.1 Equality by values

```csharp
class  MoneyClass  { decimal Amount; string Currency; }
record MoneyRecord { decimal Amount; string Currency; }

new MoneyClass(10, "EUR")  == new MoneyClass(10, "EUR")   // false: two different objects
new MoneyRecord(10, "EUR") == new MoneyRecord(10, "EUR")  // true: same values
```

A class compares *which object* it is; a record compares *what it contains*. That is right for
things where two with the same content are interchangeable (an amount, an address, an event) and
wrong for things with identity (two products with the same name are still two products, which is
why `Entity` overrides `Equals` to compare ids).

### 3.2 The one-line (positional) form

```csharp
public sealed record OrderPaid(Guid OrderId) : DomainEvent;
```

is shorthand for a record with a constructor taking `orderId`, a read-only property `OrderId`,
inheriting from the `DomainEvent` record, and no body. Create it with `new OrderPaid(order.Id)`,
read it with `evt.OrderId`.

### 3.3 Records with a body

`Money` and `ShippingAddress` are records written the long way: a normal constructor that
validates, get-only properties. They get value equality and still enforce their rules, and since
the values cannot change afterwards there is never a half-valid instance.

### 3.4 Rule of thumb

- No identity and never changes (value object, event, small data carrier such as `OrderLine`):
  **record**. Less code, no accidental mutation, correct equality.
- Has identity and a lifecycle (`Entity`, `AggregateRoot`): **class**.

### 3.5 Vocabulary

| Term | Meaning here |
|---|---|
| `record` | Class with generated value equality, `ToString()` and `with`. |
| Positional record | One-line form `record Name(Type Property, ...)` that generates constructor and properties. |
| `sealed` | Nothing can inherit from the type; the default for concrete classes in this code base. |
| `init` | Property that can be set only while the object is being created. |

---

## 4. Foreign keys, cascading, unique indexes and join tables in `ProductConfiguration` (Phase 2)

**Question:** What do Index, Foreign Key, Join Table, UNIQUE INDEX, cascading and non-cascading
mean, explained on Mercato's own `ProductConfiguration`?

**Short answer:** each line of an `IEntityTypeConfiguration` becomes a piece of SQL in the
migration. Foreign keys keep references valid, the delete behaviour decides what a parent delete
does to its children, indexes make lookups fast, a unique index also forbids duplicates, and a
join table is how a relational database stores a many-to-many relationship.

### 4.1 What the configuration generates

```sql
CREATE TABLE catalog.Products (
    Id            UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,   -- ValueGeneratedNever: Guid v7 from our code
    VendorId      UNIQUEIDENTIFIER NOT NULL,
    CategoryId    UNIQUEIDENTIFIER NOT NULL,
    Name          NVARCHAR(200)  NOT NULL,
    Description   NVARCHAR(2000) NOT NULL,
    Sku           NVARCHAR(50)   NOT NULL,
    ImageUrl      NVARCHAR(2048) NULL,
    IsActive      BIT NOT NULL,
    PriceAmount   DECIMAL(18,2) NOT NULL,                  -- OwnsOne(Price)
    PriceCurrency NCHAR(3)      NOT NULL,
    CONSTRAINT FK_Products_Vendors_VendorId
        FOREIGN KEY (VendorId)   REFERENCES catalog.Vendors(Id)    ON DELETE NO ACTION,
    CONSTRAINT FK_Products_Categories_CategoryId
        FOREIGN KEY (CategoryId) REFERENCES catalog.Categories(Id) ON DELETE NO ACTION
);

CREATE UNIQUE INDEX IX_Products_VendorId_Sku ON catalog.Products(VendorId, Sku);
CREATE        INDEX IX_Products_CategoryId   ON catalog.Products(CategoryId);
```

### 4.2 Foreign key

`HasOne<Vendor>().WithMany().HasForeignKey(product => product.VendorId)` means: every
`Products.VendorId` must exist as a `Vendors.Id`. The database rejects a product pointing at a
vendor that does not exist, so no product can be orphaned.

`HasOne<Vendor>()` has no navigation property: `Product` holds only `VendorId`, not a `Vendor`
object (aggregates reference each other by id, TR-15). EF still creates the real constraint.

Foreign keys exist only **inside** a module (TR-12). Product → Vendor and Product → Category are
both Catalog, so they get one. `CartItem.ProductId` (Ordering) and `StockHold.CartId`
(Inventory) cross module boundaries: plain Guid columns, no FK; the application keeps them
consistent.

### 4.3 Non-cascading delete (`Restrict`)

`.OnDelete(DeleteBehavior.Restrict)` becomes `ON DELETE NO ACTION`. Deleting a vendor that
still has products fails with error 547.

EF Core's **default for a required FK is `Cascade`**. Without the explicit `Restrict`, deleting a
vendor would silently delete all its products. BR-12 says business records are never deleted
(vendors and products are deactivated instead), so `Restrict` is the database's safety net
against a buggy query or a manual delete in SSMS.

### 4.4 Cascading delete

`ON DELETE CASCADE`: deleting the parent deletes its children automatically. Right when the
child has no meaning without the parent. Mercato's candidates are the parent/child pairs inside
one aggregate: `Cart` → `CartItem`, `InventoryItem` → `StockHold`, `Order` → `VendorOrder` →
`OrderItem`.

Rule of thumb for this code base: **inside an aggregate, cascade; between aggregates, restrict
(or no FK at all across modules).**

A third option, `ON DELETE SET NULL`, keeps the children and clears the link; it needs a
nullable FK column. SQL Server refuses schemas with multiple cascade paths to one table
(error 1785); the fix is to make one of the paths non-cascading.

### 4.5 Unique index

`HasIndex(product => new { product.VendorId, product.Sku }).IsUnique()` is a **composite**
unique index: the *pair* must be unique.

| VendorId | Sku | Insert allowed? |
|---|---|---|
| Vendor A | `PK-200` | yes |
| Vendor B | `PK-200` | yes, different vendor |
| Vendor A | `PK-200` | no, error 2601 (duplicate key) |

It has to live in the database: a single `Product` cannot see the other products, and an
application-side "does this SKU exist?" check races when two requests insert the same SKU at the
same moment. The handler catches the `DbUpdateException` and turns it into a "SKU already exists"
error.

### 4.6 Join table

Relational tables cannot store a list in a column, so a many-to-many relationship needs a third
table. Mercato V1 has none: a product has exactly one category and one vendor (one-to-many, an
FK column on `Products`). If a product could belong to several categories, `CategoryId` would
move into:

```sql
CREATE TABLE catalog.ProductCategories (
    ProductId  UNIQUEIDENTIFIER NOT NULL REFERENCES catalog.Products(Id)   ON DELETE CASCADE,
    CategoryId UNIQUEIDENTIFIER NOT NULL REFERENCES catalog.Categories(Id) ON DELETE CASCADE,
    PRIMARY KEY (ProductId, CategoryId)
);
```

One row = one link. The composite primary key prevents the same link twice. Cascade is right on
both sides: a link whose product or category is gone is meaningless.

### 4.7 Two more concepts in the same file

- **Owned type** (`OwnsOne(product => product.Price, ...)`): `Money` has no identity and no
  table; its `Amount` and `Currency` become columns of `Products` (TR-44).
- **Guid v7 with `ValueGeneratedNever()`**: the primary key is the clustered index (rows are
  stored sorted by it). Random Guids (v4) insert all over it and fragment it; v7 Guids are
  time-ordered, so new rows append at the end (TR-45).

### 4.8 Vocabulary

| Term | Meaning here |
|---|---|
| Foreign key | Constraint: a column's value must exist as the primary key of another table. |
| Referential integrity | The guarantee foreign keys give: no reference points at nothing. |
| `Cascade` | Deleting the parent deletes its children. |
| `Restrict` / `NoAction` | Deleting a parent that still has children fails. |
| `SetNull` | Deleting the parent clears the children's FK column. |
| Unique index | Index that also forbids two rows with the same key value(s). |
| Composite key / index | Key or index made of more than one column; the combination counts. |
| Join table | Third table holding pairs of FKs; models many-to-many. |
| Owned type | Value object stored as columns of its owner's table. |

---

## 5. What an index is, by example (Phase 2)

**Question:** I still don't get indexes. Examples?

**Short answer:** an index is a separate, **sorted** copy of one or more columns, each entry
pointing back to its row. Because it is sorted, SQL Server can jump to the right place instead
of reading the whole table, like the index at the back of a book or a phone book sorted by
surname.

### 5.1 Without an index: table scan

```sql
SELECT * FROM catalog.Products WHERE CategoryId = @cranes;
```

With 1,000,000 products and no index on `CategoryId`, SQL Server reads **every row** and checks
each one. That is a **table scan**: a cookbook without an index, flipping every page.

### 5.2 With an index: index seek

`IX_Products_CategoryId` (EF creates it automatically for the FK) is a sorted list:

| CategoryId (sorted) | → row |
|---|---|
| Cranes | 1 |
| Cranes | 5 |
| Electronics | 4 |
| Parts | 2 |
| Parts | 3 |

SQL Server jumps to where "Cranes" starts, reads the matching entries, stops at "Electronics"
and fetches only rows 1 and 5. That is an **index seek**. The real structure is a B-tree, so
even a million rows take about 3–4 page reads to find the starting point, instead of thousands.

### 5.3 An index helps only queries on its own columns

```sql
SELECT * FROM catalog.Products WHERE Name = 'Winch';   -- scan: nothing is sorted by Name
```

A phone book sorted by surname cannot find "everyone whose number ends in 42". If name search
gets slow, add an index on `Name`.

### 5.4 Composite index and the leftmost-column rule

`IX_Products_VendorId_Sku` is sorted by `VendorId`, then by `Sku` within each vendor (surname,
then first name):

| VendorId | Sku | → row |
|---|---|---|
| Vendor A | HB-10 | 2 |
| Vendor A | PK-200 | 1 |
| Vendor A | PK-300 | 5 |
| Vendor B | OF-9 | 1000000 |
| Vendor B | RC-1 | 4 |

```sql
WHERE VendorId = @a AND Sku = 'PK-200'   -- seek: both columns ("Miller, Anna")
WHERE VendorId = @a                      -- seek: first column only ("all Millers")
WHERE Sku = 'PK-200'                     -- scan: second column only ("all Annas")
```

A composite index helps only when the query filters on its **first** column. That is also why
EF did not create a separate `IX_Products_VendorId`: this index already covers it.

### 5.5 Unique = index + rule

Because the index is sorted, "does Vendor A already have `PK-200`?" is one lookup. A unique
index does that check on every insert and rejects duplicates (§4.5).

### 5.6 The cost

Every index is a sorted copy that must stay sorted. Inserting one product writes the row **and**
an entry in each index. Indexes make **reads faster and writes slower** and use disk space.
Index the columns used in `WHERE`, `JOIN ... ON` and `ORDER BY`; a catalog (read often, written
rarely) benefits; a write-heavy log table wants few indexes.

### 5.7 Clustered vs non-clustered

The primary key `Id` is the **clustered** index: the table rows themselves are stored sorted by
it, which is why `WHERE Id = @id` is always fast. A table has only one clustered index (rows can
be stored in one order only). All others are **non-clustered**: separate sorted lists pointing
back to the rows.

### 5.8 See it in SSMS

```sql
SET STATISTICS IO ON;
SELECT * FROM catalog.Products WHERE Name = 'Winch';         -- note "logical reads"
SELECT * FROM catalog.Products WHERE CategoryId = @someId;   -- compare
```

Turn on *Include Actual Execution Plan* (Ctrl+M) before running: the first shows a scan, the
second an **Index Seek** with far fewer logical reads. With a handful of rows the difference is
invisible; insert ~100,000 test rows and it becomes obvious.

### 5.9 Vocabulary

| Term | Meaning here |
|---|---|
| Index | Separate sorted structure (B-tree) over one or more columns, pointing back to rows. |
| Table scan | Reading every row because no index fits the query. |
| Index seek | Jumping straight to matching entries through an index. |
| Clustered index | The table itself, stored sorted by this key (the primary key by default). |
| Non-clustered index | A separate sorted list with pointers to the rows. |
| Leftmost-column rule | A composite index helps only queries that filter on its first column. |
| Logical reads | Pages read to answer a query; the number to compare. |

---

## 6. What a JOIN is, and what it returns (Phase 2)

**Question:** What is the JOIN operation? Is its result like a table?

**Short answer:** a JOIN combines rows from two or more tables by matching a column in one to a
column in the other, usually following a foreign key. The result is a table-shaped **result
set** (rows and columns) that exists only while the query runs; nothing new is stored.

### 6.1 Why it is needed

`Products` stores `CategoryId`, not the category name. The name lives in one place, so renaming
a category updates one row, not thousands of products. To *show* a product with its category
name, a JOIN puts them back together.

Sample data:

| Categories.Id | Name |
|---|---|
| C1 | Cranes |
| C2 | Parts |
| C3 | Electronics |

| Products.Id | Name | CategoryId | VendorId |
|---|---|---|---|
| P1 | Crane Arm | C1 | V1 |
| P2 | Hook Block | C2 | V1 |
| P3 | Winch | C2 | V2 |
| P4 | Loader Crane | C1 | V2 |

| Vendors.Id | Name |
|---|---|
| V1 | Palfinger Store |
| V2 | Lift Parts SRL |
| V3 | New Vendor Ltd |

### 6.2 INNER JOIN: only rows with a match on both sides

```sql
SELECT p.Name AS Product, c.Name AS Category
FROM catalog.Products p
INNER JOIN catalog.Categories c ON c.Id = p.CategoryId;
```

| Product | Category |
|---|---|
| Crane Arm | Cranes |
| Hook Block | Parts |
| Winch | Parts |
| Loader Crane | Cranes |

`p` and `c` are aliases; `ON` is the matching rule (here exactly the FK). Electronics is missing
because no product matches it.

### 6.3 LEFT JOIN: everything from the left table, matches if any

```sql
SELECT v.Name AS Vendor, COUNT(p.Id) AS ProductCount
FROM catalog.Vendors v
LEFT JOIN catalog.Products p ON p.VendorId = v.Id
GROUP BY v.Name;
```

| Vendor | ProductCount |
|---|---|
| Palfinger Store | 2 |
| Lift Parts SRL | 2 |
| New Vendor Ltd | 0 |

Unmatched right-side columns come back as `NULL`. With INNER JOIN the new vendor would disappear
from an admin page. RIGHT JOIN is the mirror image (rare); FULL OUTER JOIN keeps unmatched rows
from both sides.

### 6.4 Several joins: the catalog list (FR-CT-04)

```sql
SELECT p.Name, p.PriceAmount, p.PriceCurrency, c.Name AS Category, v.Name AS Vendor
FROM catalog.Products p
INNER JOIN catalog.Categories c ON c.Id = p.CategoryId
INNER JOIN catalog.Vendors    v ON v.Id = p.VendorId
WHERE p.IsActive = 1 AND c.Id = @categoryId
ORDER BY p.Name
OFFSET 0 ROWS FETCH NEXT 20 ROWS ONLY;
```

Each JOIN adds another table's columns to the row.

### 6.5 Joins and indexes

To evaluate `ON c.Id = p.CategoryId` quickly, both sides need an index: `Categories.Id` is the
primary key, `Products.CategoryId` has the FK index. That is the main reason EF indexes every
foreign key: **FK columns are what you join on.**

### 6.6 The result is a (virtual) table

- **Nothing is stored.** The source tables are unchanged; run the query tomorrow after a rename
  and the result shows the new name.
- **Rows repeat data.** One row per match, so the "one" side of a one-to-many is copied onto
  each matching row ("Cranes" appears twice; a vendor with 500 products appears 500 times).
- **It can be queried further.** A CTE names a result for the rest of one statement:

  ```sql
  WITH ProductsWithCategory AS (
      SELECT p.Name AS Product, c.Name AS Category
      FROM catalog.Products p
      INNER JOIN catalog.Categories c ON c.Id = p.CategoryId
  )
  SELECT Category, COUNT(*) AS Products
  FROM ProductsWithCategory
  GROUP BY Category;
  ```

- **A view stores the query, not the data.** `CREATE VIEW catalog.ProductsWithCategory AS
  SELECT ...` can then be queried like a table; the JOIN is re-run each time.
- **In C# each row becomes an object** in the list EF Core returns.

### 6.7 The same in EF Core

`Product` has no `Category` navigation property, so the join is explicit:

```csharp
var products = await (
    from product in db.Products
    join category in db.Categories on product.CategoryId equals category.Id
    where product.IsActive
    select new { product.Name, Category = category.Name }
).ToListAsync();
```

EF translates it into the INNER JOIN of §6.2. Left joins: `.LeftJoin(...)` in recent EF Core,
or `join ... into g from x in g.DefaultIfEmpty()` in older versions.

Joins stay **inside a module** (TR-12). Joining `catalog.Products` to `ordering.CartItems` in one
query would couple the modules; Ordering gets product data through an interface or keeps a copy
taken at order time.

### 6.8 Vocabulary

| Term | Meaning here |
|---|---|
| JOIN | Combines rows of two tables whose columns match the `ON` condition. |
| INNER JOIN | Keeps only rows with a match on both sides. |
| LEFT JOIN | Keeps all rows of the left table; missing right-side values are `NULL`. |
| Result set | The table-shaped result of a query; not stored. |
| Alias | Short name for a table in a query (`catalog.Products p`). |
| CTE | `WITH name AS (...)`: a named result usable like a table within one statement. |
| View | A stored query that can be selected from like a table. |
