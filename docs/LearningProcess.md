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
