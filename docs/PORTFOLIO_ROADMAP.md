# Portfolio Roadmap

**Total phases:** 12
**Current phase:** 4 — Product Management UI (**Current; not started**)
**Completed:** 1–3
**Pending:** 4–12

| Phase | Status | Scope |
| --- | --- | --- |
| 1. Baseline & Roadmap | Completed | Audit the submitted solution, verify its baseline, and maintain this roadmap. |
| 2. Domain & Database | Completed | Add categories/products and evolve the order schema only as needed. |
| 3. Product Management API | Completed | Add category/product DTOs, business logic, and REST endpoints. |
| 4. Product Management UI | Current | Add simple shared category/product management screens. |
| 5. Order Domain Upgrade | Pending | Add statuses, quantities, product references/snapshots, timestamps, server totals, and order rules. |
| 6. POS / New Order UI | Pending | Load API products by category; support basket quantities, removal/clear, submission, and feedback. |
| 7. Orders & History | Pending | Add list/detail, status/date/search filters, and pending payment/cancellation actions. |
| 8. Dashboard | Pending | Add small current-day metrics and recent orders. |
| 9. API Hardening | Pending | Standardize validation/ProblemDetails, exception handling, logging, contracts, async, and integrity checks. |
| 10. Automated Tests | Pending | Add focused unit/integration coverage for product and order rules. |
| 11. Developer Experience | Pending | Add useful seed data, setup/configuration cleanup, OpenAPI, and practical Docker guidance. |
| 12. GitHub Release | Pending | Add CI and portfolio-quality README, architecture, setup, API, test, and screenshot guidance. |

## Baseline recorded in Phase 1

- **Solution:** .NET 10 solution with `TillApp.Server`, `TillApp.Client.WASM`, `TillApp.Client.MAUI` (Android), `TillApp.Client.Shared`, `TillApp.Shared`, and two xUnit test projects.
- **Backend:** ASP.NET Core controller API, EF Core 10/SQL Server, one `OrderService`, DTO contracts in `TillApp.Shared`, initial migration, OpenAPI in Development, and development CORS for the WASM host.
- **Current data/API:** `Orders` and `OrderItems`; orders have a name, computed `Amount`, and `IsPaid`. Existing CRUD/payment routes are `/api/orders`, `/api/orders/{id}`, and `/api/orders/{id}/paid`.
- **Clients:** shared Razor home/new-order/unpaid-order pages used by both hosts. The browser and MAUI hosts supply their own API base URLs. The ten-item `ProductCatalogue` is static client data, not database data.
- **Tests/docs:** 23 SQL Server-backed API integration tests and 9 shared-client tests; README includes Docker SQL Server, migrations, local startup, API, and Android instructions.

## Preserve and evolve

- Keep the existing project boundaries, shared Razor UI, API client/error handling pattern, EF migrations, SQL Server Compose service, and real-SQL integration-test approach.
- Retire the static client catalogue through the planned category/product API work; clients must not remain authoritative for product prices.
- Replace the binary `IsPaid` model in Phase 5 with the requested three-state order status; add quantities, product snapshots/references, and timestamps there rather than prematurely.
- Revisit current unrestricted order update/delete behavior as part of the Phase 5/9 business-rule and API-hardening work. Existing standalone SQL scripts will need to track future migrations when the schema changes.

## Phase 2 decisions

- Added database-only `Categories` and `Products`. A product has category, name, `UnitPrice`, and `IsActive`; category names are unique and product names are unique within a category.
- Product prices use the existing SQL `money` validation range. Category deletion is restricted while it has products.
- Added migration `20260926084813_AddProductCatalogue` and kept both SQL setup scripts aligned. No product seed data, DTOs, endpoints, client calls, UI, or order-table changes were made.
- Product references, historical snapshots, quantities, timestamps, and status remain deliberately deferred to Phase 5.

## Phase 3 decisions

- Added category and product DTOs/requests in `TillApp.Shared`, and a focused `ProductCatalogService` for persistence and business checks.
- Added `/api/categories` list/detail/create/update/delete and `/api/products` list/detail/create/update plus `PATCH /api/products/{id}/active`.
- Product listing supports optional `categoryId` and `isActive` filters. Inactive products are retained for history and can be reactivated.
- Duplicate names return `409 ProblemDetails`; missing categories return `404 ProblemDetails`; request shape and price validation use the shared validation attributes. Categories with products cannot be deleted.
- The shared UI still uses its existing catalogue; replacing it with API-loaded products belongs to Phases 4 and 6.

## Phase 1 verification

- `dotnet test TillApp.Server.Tests/TillApp.Server.Tests.csproj --no-restore` — **23 passed** (SQL Server container healthy).
- `dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj --no-restore` — **9 passed**.
- `dotnet build TillApp.Server/TillApp.Server.csproj --no-restore` — **passed**.
- `dotnet build TillApp.Client.WASM/TillApp.Client.WASM.csproj --no-restore` — **passed**.
- `dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android --no-restore` — **passed**.

## Phase 2 verification

- `dotnet tool run dotnet-ef migrations has-pending-model-changes ...` — **passed**; no pending model changes.
- `dotnet test TillApp.Server.Tests/TillApp.Server.Tests.csproj --no-restore` — **23 passed** against SQL Server after migration application.
- `dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj --no-restore` — **9 passed**.
- `dotnet build TillApp.Server/TillApp.Server.csproj --no-restore` — **passed**, 0 warnings/errors.

## Phase 3 verification

- `dotnet test TillApp.Server.Tests/TillApp.Server.Tests.csproj --no-restore` — **40 passed** against SQL Server, including 17 product/category API tests.
- `dotnet build TillApp.Server/TillApp.Server.csproj --no-restore` — **passed**.
- `git diff --check` — **passed**.

## Files changed by Phase 1

- `docs/PORTFOLIO_ROADMAP.md`
- `AGENTS.md`

## Files changed by Phase 2

- `TillApp.Server/Data/Entities/Category.cs`, `Product.cs`, and `TillAppDbContext.cs`
- `TillApp.Server/Data/Configurations/CategoryConfiguration.cs`, `ProductConfiguration.cs`
- `TillApp.Server/Data/Migrations/20260926084813_AddProductCatalogue.*` and `TillAppDbContextModelSnapshot.cs`
- `database/create-database.sql` and `database/ef-migrations.sql`
- `docs/PORTFOLIO_ROADMAP.md`

## Files changed by Phase 3

- `TillApp.Shared/Catalog/*.cs`
- `TillApp.Server/Controllers/CategoriesController.cs`, `ProductsController.cs`, `Program.cs`
- `TillApp.Server/Services/IProductCatalogService.cs`, `ProductCatalogService.cs`, `ProductCatalogException.cs`
- `TillApp.Server.Tests/ProductManagementApiTests.cs`
- `docs/PORTFOLIO_ROADMAP.md`
