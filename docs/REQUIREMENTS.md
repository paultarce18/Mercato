# Mercato – Requirements (final goal)

> **This is the end state, not the first build.** The version being built is in
> `REQUIREMENTS-V1.md`; items move from here to there one at a time (see its §8).
>
> What the application must do, from the user's point of view, and the rules it must enforce.
> Companion to `PLAN.md` (how and when it is built) and `DECISIONS.md` (what was decided).
> Status: **draft v0.1** (Phase 1). Refined at the start of each phase that implements an area;
> an implemented requirement gets its phase number in the "Phase" column.
> Last updated: 2026-10-27

Requirement IDs: `FR-<Area>-<nn>` functional, `BR-<nn>` business rule, `NFR-<nn>` non-functional.
Priority: **M** must (needed for the demo), **S** should, **C** could (if time allows).

---

## 1. Purpose and scope

Mercato is an online marketplace. Independent **vendors** list products; **customers** buy
products from several vendors in a single checkout; the platform collects payment, splits the
order per vendor, tracks fulfillment from warehouse to doorstep, handles returns and refunds,
and pays vendors out. It exists as a portfolio project: the goal is a realistic, demonstrable
system, not a commercial product (see `PLAN.md` §1 and §8 for scope limits).

External parties (payment provider, warehouse scanners, couriers, e-mail) are **simulated**:
a console tool (`tools/Mercato.Simulator`) sends the webhooks and events a real integration
would send, and e-mails are written to the log instead of delivered.

---

## 2. Actors

| Actor | Who | Main goals |
|---|---|---|
| **Customer** | a registered shopper | browse, search, add to cart, check out, track orders, return items, review products |
| **Vendor** | a seller organisation; one or more vendor users | manage own products and stock, see and fulfil own orders, see payouts |
| **Warehouse Operator** | picks and packs items for a vendor | see pick lists, mark items picked/packed |
| **Courier** | delivers shipments | receive shipments, report tracking events (simulated) |
| **Support Agent** | platform staff | see all customers/orders, handle return requests and complaints, cannot change prices |
| **Admin** | platform operator | manage vendors and users, platform configuration, approve vendor onboarding |
| **System** | background jobs and the simulator | cart expiry, payouts, abandoned-cart reminders, index rebuilds, external events |

A user has exactly one role in the first version (BR-01). Vendor-bound roles (Vendor,
Warehouse Operator) always belong to exactly one vendor.

---

## 3. Core concepts (glossary)

| Term | Meaning |
|---|---|
| **Vendor** | A seller on the platform. The **tenant**: everything a vendor owns carries its `VendorId`. |
| **Product** | A sellable item owned by one vendor: name, description, category, price, images, active flag. |
| **Inventory** | Stock on hand per product, plus the amount currently **reserved** by carts and orders. |
| **Cart** | A customer's intended purchase; items may come from many vendors. Holds stock for a limited time. |
| **Stock hold** | A timed reservation of a quantity of a product for one cart. Expires if not checked out. |
| **Order** | A customer's confirmed purchase from one checkout; the unit of payment. |
| **VendorOrder** | The part of an Order belonging to one vendor; the unit of fulfilment and payout. |
| **OrderItem** | One product line in a VendorOrder: product, quantity, unit price at purchase time. |
| **Payment** | Authorisation, capture and refund of money for one Order via the (simulated) provider. |
| **Shipment** | A VendorOrder on its way: carrier, tracking number, tracking events. |
| **Return** | A customer's request to send items back, leading to a refund if accepted. |
| **Review** | A customer's rating and text for a product they bought. |
| **Notification** | A message to a user (e-mail, in-app) triggered by an event. |
| **Payout** | The periodic transfer of a vendor's earned amount (minus platform commission). |

---

## 4. Functional requirements

### 4.1 Identity & access (Identity module)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-ID-01 | A visitor can register as a Customer with e-mail and password. | M | 4 |
| FR-ID-02 | A user can log in and receives an access token plus refresh token; tokens expire and can be refreshed. | M | 4 |
| FR-ID-03 | A user can log out (refresh token revoked). | M | 4 |
| FR-ID-04 | An Admin can create vendor users, warehouse operators, couriers and support agents and assign them to a vendor where applicable. | M | 4 |
| FR-ID-05 | Every API endpoint declares who may call it (role and/or policy); unauthorised calls are rejected with 401/403. | M | 4 |
| FR-ID-06 | Login later works via an external identity provider (Keycloak, OIDC Authorization Code + PKCE) with the same roles. | M | 7 |
| FR-ID-07 | A user can change their password and basic profile data. | S | 4 |

### 4.2 Vendor management (Catalog module)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-VN-01 | An Admin can create a vendor (name, contact, commission rate) and activate/deactivate it. | M | 2–3 |
| FR-VN-02 | A Vendor can view and edit its own profile (description, logo, return policy text). | S | 3 |
| FR-VN-03 | A vendor applicant can request onboarding; an Admin approves or rejects. | C | later |

### 4.3 Catalog (Catalog module)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-CT-01 | A Vendor can create, edit, activate and deactivate its own products. | M | 2–3 |
| FR-CT-02 | A product has: name, description, category, price (currency), SKU, images, active flag, vendor. | M | 2 |
| FR-CT-03 | Anyone (including anonymous visitors) can browse active products and view product details. | M | 3 |
| FR-CT-04 | Products can be searched by text and filtered by category, vendor and price range, with paging and sorting. | M | 3 |
| FR-CT-05 | A product page shows current availability (in stock / low stock / out of stock) without exposing exact counts. | S | 3 |
| FR-CT-06 | Categories form a flat list managed by Admin (tree is out of scope). | S | 2 |
| FR-CT-07 | A search index is rebuilt by a scheduled job after catalog changes. | C | 10 |

### 4.4 Inventory & stock reservation (Inventory module)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-IN-01 | A Vendor (or its Warehouse Operator) can set and adjust stock on hand per product. | M | 2–3 |
| FR-IN-02 | Adding a product to a cart places a **timed hold** on that quantity; held stock is not sellable to others. | M | 3 |
| FR-IN-03 | A hold expires after a configured time (default 15 min) if the cart is not checked out; the stock becomes available again. | M | 3, 8 (job) |
| FR-IN-04 | When several customers compete for the last unit, exactly one succeeds; the others get a clear "no longer available" response. | M | 3 (DB concurrency), 10 (Redis lock) |
| FR-IN-05 | Placing an order converts holds into firm reservations; shipping decrements stock on hand. | M | 3, 8 |
| FR-IN-06 | Available quantity = on hand − reserved; it is never negative. | M | 2 |

### 4.5 Cart (Ordering module)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-CA-01 | A Customer has one active cart; items can be added, quantity changed, removed. | M | 3 |
| FR-CA-02 | The cart shows items grouped by vendor with line totals, a per-vendor subtotal and a grand total. | M | 3, 5 |
| FR-CA-03 | Cart prices are refreshed from the catalog at display time; the price is frozen only when the order is placed. | M | 3 |
| FR-CA-04 | An item whose hold expired is flagged in the cart and must be re-added (re-held) before checkout. | M | 3 |
| FR-CA-05 | A cart untouched for a configured period triggers one abandoned-cart reminder e-mail. | C | 8 |

### 4.6 Ordering & checkout (Ordering module)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-OR-01 | A Customer checks out the cart with a shipping address and a (simulated) payment method and receives an order number. | M | 3 |
| FR-OR-02 | Placing an order freezes prices, converts holds to reservations and empties the cart. | M | 3 |
| FR-OR-03 | The order is **split into one VendorOrder per vendor**; each progresses independently. | M | 3 |
| FR-OR-04 | A Customer can see their order history and the status of each VendorOrder, including tracking. | M | 3, 5 |
| FR-OR-05 | A Customer can cancel an order (or a VendorOrder) while it is not yet picked; stock is released and payment voided/refunded. | S | 8 |
| FR-OR-06 | A Vendor sees only its own VendorOrders; Support and Admin see all orders. | M | 4 |
| FR-OR-07 | Order state changes follow the state machine in §5.1; invalid transitions are rejected. | M | 2–3 |
| FR-OR-08 | Order totals: item lines, per-vendor shipping fee (flat, configured per vendor), grand total. Taxes are out of scope. | M | 2 |

### 4.7 Payments (Payments module → service in Phase 8)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-PA-01 | On checkout the platform **authorises** the order total with the (simulated) payment provider. | M | 3 (stub), 8 |
| FR-PA-02 | Payment is **captured** per VendorOrder when it ships. | M | 8 |
| FR-PA-03 | A refund (full or partial) is issued when a return is accepted or an order is cancelled. | M | 8 |
| FR-PA-04 | The provider reports results asynchronously via webhooks (sent by the Simulator); the platform handles them idempotently. | M | 8 |
| FR-PA-05 | If authorisation fails, the order is rejected and reservations are released (compensation). | M | 8 |

### 4.8 Fulfilment & shipping (Ordering + Shipping module → service in Phase 9)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-SH-01 | A Warehouse Operator sees a pick list of VendorOrders to fulfil for their vendor and marks items **picked** then **packed**. | M | 3, 5 |
| FR-SH-02 | Packing a VendorOrder creates a Shipment with a carrier and tracking number and marks it **shipped**. | M | 8–9 |
| FR-SH-03 | Courier tracking events (in transit, out for delivery, delivered, failed) update the shipment and the VendorOrder (simulated). | M | 9 |
| FR-SH-04 | The customer is notified at shipped and delivered. | M | 9 |

### 4.9 Returns & refunds (Ordering module)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-RT-01 | A Customer can request a return for delivered items within a configured window (default 14 days), with a reason. | M | 3, 8 |
| FR-RT-02 | A Vendor or Support Agent accepts or rejects the request. | M | 3 |
| FR-RT-03 | An accepted return, once the items are received back (simulated event), triggers a refund and restocks the items. | M | 8 |
| FR-RT-04 | The customer can see the return status. | M | 5 |

### 4.10 Reviews (Catalog module)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-RV-01 | A Customer can rate (1–5) and review a product they have received. One review per product per customer. | S | 3 |
| FR-RV-02 | The product page shows the average rating and the reviews. | S | 3, 5 |
| FR-RV-03 | Support can hide a review. | C | later |

### 4.11 Notifications (Notifications module → service in Phase 9)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-NO-01 | Users receive notifications for: order placed, payment result, shipped, delivered, return decision, refund issued. | M | 9 |
| FR-NO-02 | Channels: e-mail (written to the log, not delivered) and in-app real-time (SignalR). | M | 9 |
| FR-NO-03 | A user can see their recent notifications and mark them read. | S | 9 |

### 4.12 Vendor payouts (Ordering/Payments)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-PO-01 | A scheduled job computes each vendor's payout for delivered, non-refunded VendorOrders, minus the platform commission. | S | 8 |
| FR-PO-02 | A Vendor can see its payout statements. | S | 8 |
| FR-PO-03 | Admin can see all payouts. | S | 8 |

### 4.13 Support & administration

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-SU-01 | A Support Agent can look up any customer and any order and see its full history (status changes, payments, shipments). | M | 3–4 |
| FR-SU-02 | A Support Agent can act on returns and cancellations on behalf of a customer but cannot change products or prices. | M | 4 |
| FR-AD-01 | An Admin can manage vendors, users and categories. | M | 3–4 |
| FR-AD-02 | An Admin can view platform health and basic metrics (orders/day, revenue). | C | 10 |

### 4.14 Frontend (Angular, `web/mercato-web`)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-FE-01 | A single Angular web app serves all roles; the visible navigation depends on the role. | M | 5 |
| FR-FE-02 | Public pages: catalog browse/search, product detail. Authenticated: cart, checkout, orders, returns, reviews (Customer); products, stock, orders, payouts (Vendor); pick lists (Warehouse); order lookup, returns (Support); vendors, users, categories (Admin). | M | 5 |
| FR-FE-03 | The app attaches the access token to API calls, refreshes it transparently and redirects to login on 401. | M | 5 |
| FR-FE-04 | Routes are protected by role guards; feature areas are lazy-loaded. | M | 5 |
| FR-FE-05 | Order status updates appear in real time (SignalR) without reload. | S | 9 |
| FR-FE-06 | The UI is responsive and uses Angular Material. | S | 5 |

### 4.15 Simulator (`tools/Mercato.Simulator`)

| ID | Requirement | Prio | Phase |
|---|---|---|---|
| FR-SI-01 | A console tool can send payment-provider webhooks (authorised, declined, captured, refunded) for a given order. | M | 8 |
| FR-SI-02 | It can send warehouse scan events (picked, packed) and courier tracking events for a given shipment. | M | 9 |
| FR-SI-03 | It can generate demo data (vendors, products, stock, customers) for a presentation. | S | 15 |

---

## 5. Key workflows

### 5.1 Order lifecycle (saga)

```
Cart (holds) ──checkout──▶ Placed ──auth ok──▶ PaymentAuthorized ──▶ SplitPerVendor
                              │ auth failed                                 │ per VendorOrder:
                              ▼                                             ▼
                          Rejected (holds released)        Pending ──pick──▶ Picked ──pack/ship──▶ Shipped
                                                                                    (capture payment)   │
                                                                                                        ▼
                                                     Refunded ◀──return accepted + received── ReturnRequested ◀── Delivered
                                                                                                        ▲
                                              Cancelled (void/refund, restock) ◀── customer/support, only before Picked
```

- The **Order** carries the overall state (Placed, PaymentAuthorized, Rejected, Completed).
- Each **VendorOrder** carries its own fulfilment state (Pending → Picked → Shipped → Delivered → ReturnRequested → Refunded, or Cancelled).
- Compensation: a failed authorisation releases reservations; a failed capture at ship time puts the VendorOrder on hold for Support; a rejected return leaves the VendorOrder Delivered.
- In Phases 2–7 the transitions run in-process; from Phase 8 the Worker drives them from Kafka events (saga), with the outbox pattern and idempotent consumers.

### 5.2 Stock hold

1. Customer adds quantity *q* of product *p* to the cart.
2. If available (on hand − reserved) ≥ *q*: create a hold (*p*, *q*, cart, expires at now + 15 min), reserved += *q*. Otherwise reject with the available quantity.
3. Checkout converts the cart's holds into order reservations (no expiry).
4. A scheduled job (every minute) releases expired holds: reserved −= *q*.
5. Shipping decrements on hand and reserved by the shipped quantity.
6. Concurrency: the hold step is atomic per product (optimistic concurrency on the inventory row; a Redis lock from Phase 10 so that two Worker instances cannot double-release).

### 5.3 Return and refund

Customer requests return (within window) → Vendor/Support decides → if accepted, customer ships back (simulated courier event "received") → refund issued via provider → items restocked → customer notified.

---

## 6. Business rules

| ID | Rule |
|---|---|
| BR-01 | A user has exactly one role. Vendor and Warehouse Operator users belong to exactly one vendor. |
| BR-02 | **Tenancy:** every vendor-owned entity (Product, Inventory, VendorOrder, Shipment, Payout) carries `VendorId`; vendor-bound users can read and write only rows of their own vendor. Enforced in the application layer on every query and command, not only in the UI. |
| BR-03 | Support Agents can read everything and act on orders/returns but cannot create or change products, prices or stock. |
| BR-04 | Prices are frozen on the OrderItem at checkout; later catalog price changes never affect existing orders. |
| BR-05 | Available quantity never goes below zero; the last unit is sold to exactly one buyer. |
| BR-06 | A stock hold lasts 15 minutes (configurable); an expired hold cannot be checked out. |
| BR-07 | An order can be cancelled only while every VendorOrder is still Pending (not Picked). |
| BR-08 | A return can be requested only for Delivered items and within 14 days (configurable) of delivery. |
| BR-09 | Payment is authorised for the whole order at checkout and captured per VendorOrder at shipment; a refund never exceeds the captured amount. |
| BR-10 | Platform commission is a percentage per vendor, applied to the item subtotal of each delivered VendorOrder at payout time. |
| BR-11 | A product can be reviewed only by a customer whose VendorOrder containing it reached Delivered; one review per customer per product. |
| BR-12 | Deactivating a product or vendor hides it from the catalog but keeps existing orders, history and reviews intact (no hard deletes of business records). |
| BR-13 | All money amounts are stored with currency; the first version uses a single platform currency (EUR). |
| BR-14 | All timestamps are stored in UTC; the clock is injected (never `DateTime.Now`). |

---

## 7. Non-functional requirements

| ID | Requirement | Phase |
|---|---|---|
| NFR-01 | **Security:** all endpoints authenticated except public catalog; role- and policy-based authorisation; vendor tenancy per BR-02; secrets never in the repo (GR-31); HTTPS only. | 4, 7 |
| NFR-02 | **API:** versioned (`/api/v1/…`), documented with OpenAPI per version, errors as RFC 9457 ProblemDetails, every request carries a correlation ID. | 3 |
| NFR-03 | **Reliability:** no lost domain events (outbox), consumers idempotent, retries with Polly for outgoing calls, health checks (liveness/readiness) per deployable. | 8, 10 |
| NFR-04 | **Observability:** structured logs (Serilog → Seq), distributed traces and metrics (OpenTelemetry) across API, Worker and services; correlation ID in every log line. | 3, 10 |
| NFR-05 | **Performance (demo scale):** catalog search < 500 ms for 10k products; checkout < 2 s excluding simulated provider latency; the system runs on one developer laptop via docker-compose. | 3, 6 |
| NFR-06 | **Testability:** domain and application logic unit-tested without a database; integration tests with a real SQL Server via Testcontainers; architecture tests enforce the dependency rule. | 11 |
| NFR-07 | **Deployability:** every deployable has a multi-stage Dockerfile; one `docker compose up` starts the whole system; Helm chart for Kubernetes; Azure deployment via Bicep. | 6, 13, 14 |
| NFR-08 | **CI/CD:** PRs are built and tested automatically; images pushed to GHCR on main; a second pipeline in Azure DevOps builds the same repo. | 12 |
| NFR-09 | **Evolvability:** modules communicate only through application-layer interfaces or events, so Payments, Shipping and Notifications can be extracted into services without changing the others. `Mercato.Contracts` is versioned; breaking changes get a new version. | 8–9 |
| NFR-10 | **Cost:** everything runs on free tiers / open-source software. | all |
| NFR-11 | **Code quality:** rules in `docs/Rules/GeneralRules.md`; analyzers at `latest-recommended` with warnings as errors. | 1 |

---

## 8. Out of scope (first version)

Mobile app, real payment provider, real e-mail delivery, taxes and invoicing, multi-currency,
category trees, product variants (size/colour), promotions and vouchers, recommendation engine,
multi-role users, vendor self-onboarding (C), chat support. See `PLAN.md` §8.

---

## 9. Assumptions and open questions

- Shipping fee: flat amount per vendor per order (assumption; keeps checkout simple).
- Commission rate per vendor is set by Admin at vendor creation (assumption).
- Hold duration 15 min and return window 14 days are configuration values with these defaults.
- Whether cancellation after Picked should be possible with Support override — decide in Phase 8.
- Whether Warehouse Operators are vendor staff or platform staff serving many vendors — modelled as **vendor staff** (one vendor) for now; revisit if a shared-warehouse scenario is wanted.
