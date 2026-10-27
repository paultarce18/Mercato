# Mercato – Requirements, Version 1 (learning scope)

> The **first buildable version**. Every module exists, but each has only the use cases needed
> for one complete path: a customer buys products from two vendors, pays (simulated), the
> vendors ship, the customer sees delivery and gets notified.
> `REQUIREMENTS.md` is the final goal; this file is the subset we build first. Items are
> added from the final file one at a time once V1 works end to end (see §8).
> Status: **draft v1.0** (Phase 1). Last updated: 2026-10-27

IDs reuse the final file's IDs (`FR-CT-01` here means the same thing as in `REQUIREMENTS.md`),
so nothing has to be renumbered later. Where V1 simplifies a requirement, the row says so.

---

## 1. What V1 is

A marketplace where:

1. an **Admin** creates vendors and their vendor users;
2. a **Vendor** manages its products and stock;
3. a **Customer** registers, browses, puts products of several vendors in a cart (stock is held),
   checks out and pays (simulated);
4. the order is split per vendor, each **Vendor** marks its part shipped;
5. the (simulated) courier reports delivery;
6. the customer is notified at each step and can see the order status.

That is the whole product. It touches every module and every technology in `PLAN.md` §5.

---

## 2. Actors (3 instead of 6)

| Actor | Does in V1 | Deferred |
|---|---|---|
| **Customer** | register, log in, browse, cart, checkout, see orders | returns, reviews |
| **Vendor** | manage own products and stock, see own orders, mark them shipped | payouts, profile; warehouse operator and courier are folded into "Vendor marks shipped" + Simulator |
| **Admin** | create vendors and vendor users, manage categories | platform metrics |
| **System** | hold expiry job, Simulator (payment webhooks, delivery events) | payouts job, abandoned-cart job, index rebuild |

Not in V1: Warehouse Operator, Courier, Support Agent. The roles exist as enum values from the
start (cheap), but no screens or endpoints are built for them.

---

## 3. Core concepts

Vendor, Product, Category, InventoryItem (+ StockHold), Cart (+ CartItem), Order,
VendorOrder, OrderItem, Payment, Shipment, Notification. Same meaning as `REQUIREMENTS.md` §3.
Not in V1: Return, Review, Payout.

---

## 4. Functional requirements (V1)

### 4.1 Identity

| ID | Requirement | Phase |
|---|---|---|
| FR-ID-01 | Register as Customer (e-mail + password). | 4 |
| FR-ID-02 | Log in → access token + refresh token; refresh works. | 4 |
| FR-ID-03 | Log out (refresh token revoked). | 4 |
| FR-ID-04 | Admin creates vendor users for a vendor. *(V1: only vendor users; no warehouse/courier/support users)* | 4 |
| FR-ID-05 | Every endpoint declares role/policy; 401/403 on violation. | 4 |
| FR-ID-06 | Login via Keycloak (OIDC + PKCE) with the same three roles. | 7 |

### 4.2 Vendors & categories

| ID | Requirement | Phase |
|---|---|---|
| FR-VN-01 | Admin creates a vendor (name, contact) and activates/deactivates it. *(V1: no commission rate)* | 2–3 |
| FR-CT-06 | Admin manages a flat list of categories. | 2–3 |

### 4.3 Catalog

| ID | Requirement | Phase |
|---|---|---|
| FR-CT-01 | Vendor creates, edits, activates/deactivates its own products. | 2–3 |
| FR-CT-02 | Product: name, description, category, price, SKU, active flag, vendor. *(V1: one image URL, no upload)* | 2 |
| FR-CT-03 | Anyone can list active products and view a product. | 3 |
| FR-CT-04 | List supports text search (name contains), category filter, paging. *(V1: no price range, no sorting options)* | 3 |

### 4.4 Inventory & stock holds

| ID | Requirement | Phase |
|---|---|---|
| FR-IN-01 | Vendor sets stock on hand per product. | 2–3 |
| FR-IN-02 | Adding to cart places a timed hold; held stock is not available to others. | 3 |
| FR-IN-03 | A hold expires after 15 min; a job releases expired holds. | 3 (logic), 8 (job in Worker) |
| FR-IN-04 | Last unit goes to exactly one customer (optimistic concurrency). *(V1: DB only; Redis lock is Phase 10)* | 3 |
| FR-IN-05 | Checkout converts holds to reservations; shipping decrements stock on hand. | 3, 8 |
| FR-IN-06 | Available = on hand − reserved, never negative. | 2 |

### 4.5 Cart

| ID | Requirement | Phase |
|---|---|---|
| FR-CA-01 | One active cart per customer: add, change quantity, remove. | 3 |
| FR-CA-02 | Cart shows items grouped by vendor with subtotals and grand total. | 3, 5 |
| FR-CA-04 | An item whose hold expired is flagged and must be re-added. | 3 |

### 4.6 Ordering

| ID | Requirement | Phase |
|---|---|---|
| FR-OR-01 | Customer checks out with a shipping address; gets an order number. *(V1: payment method is implicit — "simulated card")* | 3 |
| FR-OR-02 | Checkout freezes prices, converts holds, empties the cart. | 3 |
| FR-OR-03 | Order is split into one VendorOrder per vendor. | 3 |
| FR-OR-04 | Customer sees order history and per-VendorOrder status. | 3, 5 |
| FR-OR-06 | Vendor sees only its own VendorOrders; Admin sees all. | 4 |
| FR-OR-07 | State machine in §5; invalid transitions rejected. | 2–3 |
| FR-OR-08 | Totals: item lines + grand total. *(V1: no shipping fee, no tax)* | 2 |

### 4.7 Payments

| ID | Requirement | Phase |
|---|---|---|
| FR-PA-01 | At checkout the order total is **charged** (authorise + capture in one step) with the simulated provider. *(V1 simplification; final: authorise at checkout, capture per VendorOrder at ship)* | 3 (stub), 8 |
| FR-PA-04 | The provider answers asynchronously via webhook (Simulator): `succeeded` or `declined`; handled idempotently. | 8 |
| FR-PA-05 | Declined → order Rejected, reservations released. | 8 |

### 4.8 Shipping

| ID | Requirement | Phase |
|---|---|---|
| FR-SH-02 | Vendor marks a VendorOrder **shipped**; a Shipment with a generated tracking number is created. *(V1: no pick/pack steps, no warehouse role)* | 3 (in-process), 9 (service) |
| FR-SH-03 | Simulator sends a `delivered` event for a shipment → VendorOrder Delivered. *(V1: only "delivered"; no in-transit/failed)* | 9 |

### 4.9 Notifications

| ID | Requirement | Phase |
|---|---|---|
| FR-NO-01 | Customer is notified at: order placed, payment result, shipped, delivered. | 9 |
| FR-NO-02 | Channel: e-mail written to the log. *(V1: SignalR in-app push is an add-on, see §8)* | 9 |

### 4.10 Frontend (Angular)

| ID | Requirement | Phase |
|---|---|---|
| FR-FE-01 | One Angular app; navigation depends on role. | 5 |
| FR-FE-02 | Pages — public: product list, product detail. Customer: register, login, cart, checkout, my orders. Vendor: my products (+edit), stock, my orders (+ mark shipped). Admin: vendors (+ create vendor user), categories. | 5 |
| FR-FE-03 | Token attached to API calls; refresh; redirect to login on 401. | 5 |
| FR-FE-04 | Role guards; lazy-loaded feature areas. | 5 |
| FR-FE-06 | Angular Material, basic responsive layout. | 5 |

### 4.11 Simulator

| ID | Requirement | Phase |
|---|---|---|
| FR-SI-01 | Send a payment webhook (`succeeded` / `declined`) for an order id. | 8 |
| FR-SI-02 | Send a `delivered` event for a shipment id. | 9 |
| FR-SI-03 | Seed demo data: 2 vendors, ~20 products with stock, 1 customer, 1 admin. | 2 (seed), 15 |

---

## 5. V1 order lifecycle

```
Cart (holds) ──checkout──▶ Placed ──payment succeeded──▶ Paid ──▶ per VendorOrder: Pending ──vendor ships──▶ Shipped ──simulator──▶ Delivered
                              │
                              └──payment declined──▶ Rejected (reservations released)
```

- Order states: `Placed`, `Paid`, `Rejected`, `Completed` (all VendorOrders delivered).
- VendorOrder states: `Pending`, `Shipped`, `Delivered`.
- Phases 2–7: transitions run in-process in the API. Phase 8+: driven by events via the
  Worker saga (Placed → charge → Paid; Shipped/Delivered from Shipping events).

Final-state additions later: `PaymentAuthorized`/capture split, `Picked`, `Cancelled`,
`ReturnRequested`, `Refunded`.

---

## 6. Business rules kept in V1

| ID | Rule |
|---|---|
| BR-01 | One role per user; vendor users belong to exactly one vendor. |
| BR-02 | Tenancy: vendor-owned rows carry `VendorId`; vendors read/write only their own, enforced in the application layer. |
| BR-04 | Prices frozen on OrderItem at checkout. |
| BR-05 | Available stock never below zero; last unit to exactly one buyer. |
| BR-06 | Holds last 15 minutes; expired holds cannot be checked out. |
| BR-12 | Deactivation hides, never deletes, products and vendors. |
| BR-13 | Single currency (EUR), stored with amount. |
| BR-14 | UTC timestamps; injected clock. |

Deferred: BR-03 (support), BR-07 (cancellation), BR-08 (returns), BR-09 (authorise/capture split),
BR-10 (commission), BR-11 (reviews).

---

## 7. Non-functional requirements

Unchanged from `REQUIREMENTS.md` §7 — they *are* the learning content (versioning, ProblemDetails,
outbox, idempotency, observability, Testcontainers, Docker, Kubernetes, CI/CD, Azure). The
small functional scope is what makes room for them.

---

## 8. Growth path: from V1 to the final requirements

Suggested order once V1 works end to end. Each item is one small, self-contained increment
that touches an existing module; pick any when there is time.

| Step | Adds | Final IDs | Touches |
|---|---|---|---|
| 1 | Authorise at checkout / capture at ship (two-step payment) | FR-PA-01/02, BR-09 | Payments, saga |
| 2 | Cancel order before shipped (void payment, release stock) | FR-OR-05, BR-07 | Ordering, Payments |
| 3 | Shipping fee per vendor + order totals | FR-OR-08 | Ordering |
| 4 | Pick/pack steps + Warehouse Operator role | FR-SH-01, BR-01 | Ordering, Identity, Angular |
| 5 | Full courier tracking events (in transit, failed) | FR-SH-03 | Shipping, Simulator |
| 6 | In-app real-time notifications (SignalR) | FR-NO-02/03, FR-FE-05 | Notifications, Angular |
| 7 | Returns & refunds | FR-RT-01…04, FR-PA-03, BR-08 | Ordering, Payments, Shipping |
| 8 | Reviews | FR-RV-01/02, BR-11 | Catalog, Angular |
| 9 | Support Agent role + order lookup | FR-SU-01/02, BR-03 | Identity, Ordering, Angular |
| 10 | Vendor commission + payouts job | FR-PO-01…03, BR-10, FR-VN-01 | Ordering, Worker |
| 11 | Abandoned-cart reminder job | FR-CA-05 | Worker, Notifications |
| 12 | Price-range filter, sorting, search index job | FR-CT-04/05/07 | Catalog |
| 13 | Vendor profile, onboarding, admin metrics | FR-VN-02/03, FR-AD-02 | Catalog, Admin |

---

## 9. Out of scope for V1 (and for the final version)

See `REQUIREMENTS.md` §8.
