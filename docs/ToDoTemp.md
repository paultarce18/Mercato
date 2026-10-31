# Temporary To-Do

## Where we stopped (Phase 2, step 2: EF Core persistence)

The user writes the EF Core code in Visual Studio; Claude explains the concept and reviews (no files
are written into the repo without the user saying "write"). Code given to the user carries
explaining comments in the style of the Domain classes.

- [x] A - `MercatoDbContext` (nine `DbSet`s, `OnModelCreating`, `ApplyConfigurationsFromAssembly`)
- [x] B - `VendorConfiguration`
- [x] C - `CategoryConfiguration`, `ProductConfiguration` (`Money` as owned type, unique `(VendorId, Sku)`)
- [ ] D - `InventoryItem` (rowversion TR-43, `StockHold` as owned collection, unique `ProductId`)
- [ ] E - `Cart` + `Order` (`ConfigureMoney` helper, `ShippingAddress` owned, backing-field collections, filtered unique index for one active cart per customer)
- [ ] F - `Payment`, `Shipment`, `Notification`
- [ ] G - check the generated SQL script (`GenerateCreateScript`) before the first migration
- [ ] Install the local tool in the repo root: `dotnet new tool-manifest`, then `dotnet tool install dotnet-ef --version 10.0.14`
- [ ] Then Phase 2 steps 3-7: SQL Server in Docker, first migration, seed data, SSMS, closing

Note: the full EF model only builds at runtime once D-F exist (`Order` references the unmapped
`ShippingAddress` until then), so do not run the app or create a migration before F is done.

## Phase 2 closing checklist — do before the Phase 2 commit

Update these docs at the end of Phase 2 (GR-04: repo docs are the source of truth). Claude reminds
the user when the last Phase 2 step (SSMS connected) is done.

- [ ] `CLAUDE.md` — project status: Phase 2 done, Phase 3 next; no longer "no domain code yet". Add: a session linked through Claude Desktop cannot delete files on the user's machine (the user deletes them).
- [ ] `docs/PLAN.md` §7 — Phase 2 Status column: Done + date.
- [ ] `docs/DECISIONS.md` — new ADR rows with alternatives considered:
  - `Money` and `ShippingAddress` are records, no `ValueObject` base class
  - `Guard` helper extracted (K-08)
  - events in one `Events` folder per module (namespace `...Events`)
  - `StockHold` on `InventoryItem`; one hold per cart and product, replaced on change
  - `CartItem.HoldExpiresAt` instead of `HoldId`; checked-out cart keeps its items (`CartStatus.CheckedOut`)
  - per-module transition exceptions instead of one shared class
  - `User` deferred to Phase 4
  - `Product` is one vendor's listing (carries `VendorId`); no shared product with several offers in V1
  - EF mapping lives in `IEntityTypeConfiguration<T>` classes (Infrastructure), not attributes on domain classes
  - foreign keys only inside a module (Catalog: Product to Vendor/Category, `Restrict`); across modules ids only (TR-12)
  - `Money` stored as `PriceAmount decimal(18,2)` + `PriceCurrency nchar(3)` via `OwnsOne`
  - plus the EF Core / SQL Server / migration decisions made in steps 2–5
- [ ] `docs/DECISIONS.md` — fill the Phase 2 phase-log block (Status / Built / Decisions / Known issues / TODO / Next).
- [ ] `docs/ARCHITECTURE.md` — §5.1 table: `Shipment` gets `VendorId`, `CartItem` gets `HoldExpiresAt`, `Cart` gets a status; §5 folder tree: replace `ValueObject.cs` / `IDomainEvent.cs` with `DomainEvent.cs`, `Money.cs`, `Guard.cs`, add the `Events` folders.
- [ ] `docs/LearningProcess.md` — add sections for any further explanations asked for.
- [ ] Refresh the Claude project knowledge copies (ARCHITECTURE, DECISIONS, PLAN, LearningProcess, CLAUDE).
- [ ] Decide whether `docs/ToDoTemp.md` stays tracked; empty it once this list is done.
