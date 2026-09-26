# Portfolio Roadmap

**Total phases:** 12
**Current phase:** 10 — Automated Tests (**Current; not started**)
**Completed:** 1–9
**Pending:** 10–12

| Phase | Status | Scope |
| --- | --- | --- |
| 1. Baseline & Roadmap | Completed | Audit the submitted solution, verify its baseline, and maintain this roadmap. |
| 2. Domain & Database | Completed | Add categories/products and evolve the order schema only as needed. |
| 3. Product Management API | Completed | Add category/product DTOs, business logic, and REST endpoints. |
| 4. Product Management UI | Completed | Add simple shared category/product management screens. |
| 5. Order Domain Upgrade | Completed | Add statuses, quantities, product references/snapshots, timestamps, server totals, and order rules. |
| 6. POS / New Order UI | Completed | Load API products by category; support basket quantities, removal/clear, submission, and feedback. |
| 7. Orders & History | Completed | Add list/detail, status/date/search filters, and pending payment/cancellation actions. |
| 8. Dashboard | Completed | Add small current-day metrics and recent orders. |
| 9. API Hardening | Completed | Standardize validation/ProblemDetails, exception handling, logging, contracts, async, and integrity checks. |
| 10. Automated Tests | Current | Add focused unit/integration coverage for product and order rules. |
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

## Phase 4 decisions

- Added one shared `/catalog` page for category and product management, linked from the shared navigation used by WASM and MAUI.
- The page uses Phase 3 request/response contracts and API endpoints for category CRUD, product create/update, category/status filtering, and product activation changes. API validation/conflict messages are shown using the existing safe client error handling.
- Added a shared catalog stylesheet loaded by both hosts. The existing New Order static catalogue remains in place until Phase 6.
- Phase 3 checkpoint commit: `9181cce` (`Phase 3 - complete product management API`) on the original branch. Phase 4 is committed separately on `feature/phase-4-product-management-ui`.

## Phase 5 decisions

- Replaced stored `IsPaid` with `OrderStatus` (`Pending`, `Paid`, `Cancelled`), `CreatedAt`, `PaidAt`, and `CancelledAt`. `OrderDto.IsPaid` remains a derived compatibility property, not stored state.
- New order lines contain a product reference, product-name/unit-price snapshots, and quantity. Totals are calculated from active catalog products on the server, and repeated product requests are combined into one line.
- Migration `20260926091539_UpgradeOrderDomain` maps old `IsPaid=false` rows to Pending and `true` rows to Paid, retains existing item names/prices as snapshots with quantity one, and leaves legacy item product references null when no reliable product mapping exists.
- Pending orders may be paid or cancelled. Repeating the same terminal action is idempotent; Paid → Cancelled and Cancelled → Paid return a conflict. Existing static clients may temporarily resolve a uniquely named active catalog product, while server prices remain authoritative; Phase 6 will replace that fallback with product-ID POS selection.

## Phase 6 decisions

- Replaced the static New Order catalogue with one shared POS page that loads categories and active products through the existing API client. Inactive products are filtered out before display.
- Added a small local form/cart model: repeated product selection increments one line; decrementing at quantity one removes that line; Clear Order preserves the order name; successful submission resets the complete form.
- The POS submits only product IDs and quantities. Its total is a client preview, while the Phase 5 API remains authoritative for price, availability, snapshots, and final totals. No backend changes were required.
- Added `pos.css`, loaded by both hosts, for POS-specific presentation. The unrelated `app.css` polish remains in its existing stash.

## Phase 7 decisions

- Replaced the unpaid-only shared Orders page with status, name/order-number, and date filters; filtering is combined in the existing orders endpoint and results remain newest-first.
- Date bounds use UTC calendar dates: `from` is inclusive at midnight and `to` is inclusive through the day by querying before the following midnight. No pagination or reporting was added.
- Added a shared order detail route. Details use Phase 5 product-name/unit-price snapshots; Pending orders offer Pay and confirmed Cancel actions, and successful transitions update or remove the row to match active filters.
- Added only the required query parameters and client calls (`status`, `search`, `from`, `to`, detail, cancel). No schema changes or Phase 8 dashboard work.

## Phase 8 decisions

- Added `GET /api/dashboard` with a dedicated summary DTO/service. EF Core performs filtered counts, a paid-total sum, and a bounded recent-order projection; the endpoint does not load all orders or order items.
- Today's Orders uses `CreatedAt` during the current UTC day. Pending Orders is the current count across all dates. Paid Orders and Cancelled Orders count transitions (`PaidAt`/`CancelledAt`) during today UTC; Today's Revenue sums persisted `Amount` for those orders paid today.
- Recent Orders returns at most eight orders, newest `CreatedAt` first with descending order ID as the tie-breaker. The shared dashboard links directly to Phase 7 order details.
- Added one shared `/dashboard` page and a Dashboard navigation link for WASM and MAUI. No schema changes or auto-refresh behavior.

## Phase 9 decisions

- Registered a centralized `IExceptionHandler` that maps expected catalog/order domain exceptions to consistent 404/409/400 ProblemDetails and logs unexpected exceptions before returning a non-sensitive 500 ProblemDetails response. Existing controllers now delegate business-error translation to it.
- Standardized direct missing-resource responses as 404 ProblemDetails and documented validation/error response types on affected endpoints. No successful response contracts changed.
- Added a shared `OrderQuery` validation model: search is bounded and trimmed, date ranges must be ordered, and conflicting `status`/legacy `isPaid` combinations return validation errors. Legacy compatible filter combinations remain supported.
- Added positive ID/category-filter validation and structured `ILogger` messages for catalog creates, updates, deletes, and activation changes. Existing async EF Core and server-authoritative decimal/UTC calculations were retained; no schema changes were needed.

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

## Phase 4 verification

- `dotnet build TillApp.Client.Shared/TillApp.Client.Shared.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj --no-restore` — **13 passed**, including 4 catalog API client tests.
- `dotnet build TillApp.Client.WASM/TillApp.Client.WASM.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android --no-restore` — **passed**, 0 warnings/errors.
- `git diff --check` — **passed**.

## Phase 5 verification

- `dotnet tool run dotnet-ef migrations has-pending-model-changes ...` — **passed**; no pending model changes.
- `dotnet test TillApp.Server.Tests/TillApp.Server.Tests.csproj --no-restore` — **32 passed** against SQL Server, covering order lifecycle, server totals, quantities, snapshots, inactive/missing products, and category/product APIs.
- `dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj --no-restore` — **13 passed**.
- `dotnet build TillApp.Server/TillApp.Server.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.WASM/TillApp.Client.WASM.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android --no-restore` — **passed**, 0 warnings/errors.
- `git diff --check` — **passed**.

## Phase 6 verification

- `dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj --no-restore` — **14 passed**, including five POS cart/form-state tests.
- `dotnet build TillApp.Client.Shared/TillApp.Client.Shared.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.WASM/TillApp.Client.WASM.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android --no-restore` — **passed**, 0 warnings/errors.
- `git diff --check` — **passed**.

## Phase 7 verification

- `dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj --no-restore` — **17 passed**, including combined filter query encoding, order detail snapshot values, and cancellation endpoint coverage.
- `dotnet build TillApp.Client.Shared/TillApp.Client.Shared.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.WASM/TillApp.Client.WASM.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Server/TillApp.Server.csproj --no-restore` and `dotnet build TillApp.Server.Tests/TillApp.Server.Tests.csproj --no-restore` — **passed**, 0 warnings/errors.
- SQL-backed order query integration tests were added but **not run** because `TILLAPP_TEST_CONNECTION_STRING` is not configured in this environment.
- `git diff --check` — **passed**.

## Phase 8 verification

- `dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj --no-restore` — **21 passed**, including Dashboard rendering of metrics/recent order links, loading/error states, and API contract coverage.
- `dotnet build TillApp.Client.Shared/TillApp.Client.Shared.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.WASM/TillApp.Client.WASM.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Server/TillApp.Server.csproj --no-restore` and `dotnet build TillApp.Server.Tests/TillApp.Server.Tests.csproj --no-restore` — **passed**, 0 warnings/errors.
- The SQL-backed Dashboard API test was added and compiles, but **was not run** because `TILLAPP_TEST_CONNECTION_STRING` is not configured in this environment.
- `git diff --check` — **passed**.

## Phase 9 verification

- `dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj --no-restore` — **24 passed**, including OrderQuery validation tests for date range and legacy-filter consistency.
- `dotnet build TillApp.Server/TillApp.Server.csproj --no-restore`, `TillApp.Server.Tests/TillApp.Server.Tests.csproj --no-restore`, and `TillApp.Client.Shared/TillApp.Client.Shared.csproj --no-restore` — **passed**, 0 warnings/errors.
- `dotnet build TillApp.Client.WASM/TillApp.Client.WASM.csproj --no-restore` and `dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android --no-restore` — **passed**, 0 warnings/errors.
- SQL-backed hardening tests compile but **were not run** because `TILLAPP_TEST_CONNECTION_STRING` is not configured in this environment.
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

## Files changed by Phase 4

- `TillApp.Client.Shared/Pages/Catalog.razor`, `Layout/MainLayout.razor`, `_Imports.razor`
- `TillApp.Client.Shared/Services/IOrdersApiClient.cs`, `OrdersApiClient.cs`
- `TillApp.Client.Shared/wwwroot/catalog.css`
- `TillApp.Client.WASM/wwwroot/index.html`, `TillApp.Client.MAUI/wwwroot/index.html`
- `TillApp.Client.Shared.Tests/CatalogApiClientTests.cs`
- `docs/PORTFOLIO_ROADMAP.md`

## Files changed by Phase 5

- `TillApp.Shared/Orders/*.cs`
- `TillApp.Server/Data/Entities/Order.cs`, `OrderItem.cs`, and `Data/Configurations/Order*.cs`
- `TillApp.Server/Data/Migrations/20260926091539_UpgradeOrderDomain.*` and `TillAppDbContextModelSnapshot.cs`
- `TillApp.Server/Services/IOrderService.cs`, `OrderService.cs`, `OrderDomainException.cs`, and `Controllers/OrdersController.cs`
- `TillApp.Server.Tests/OrdersApiTests.cs`, `ProductManagementApiTests.cs`, and `TillApp.Client.Shared.Tests/OrdersApiClientTests.cs`
- `database/create-database.sql`, `database/ef-migrations.sql`, and `docs/PORTFOLIO_ROADMAP.md`

## Files changed by Phase 6

- `TillApp.Client.Shared/Pages/NewOrder.razor` and `Models/NewOrderFormModel.cs`
- `TillApp.Client.Shared/wwwroot/pos.css`
- `TillApp.Client.WASM/wwwroot/index.html` and `TillApp.Client.MAUI/wwwroot/index.html`
- `TillApp.Client.Shared.Tests/NewOrderFormModelTests.cs`; removed the static catalogue models/tests
- `docs/PORTFOLIO_ROADMAP.md`

## Files changed by Phase 7

- `TillApp.Client.Shared/Pages/OrderList.razor`, `OrderDetails.razor`, and `Home.razor`
- `TillApp.Client.Shared/Services/IOrdersApiClient.cs`, `OrdersApiClient.cs`, and `wwwroot/order-history.css`
- `TillApp.Server/Controllers/OrdersController.cs`, `Services/IOrderService.cs`, and `Services/OrderService.cs`
- `TillApp.Server.Tests/OrdersApiTests.cs`, `TillApp.Client.Shared.Tests/OrdersApiClientTests.cs`
- `TillApp.Client.WASM/wwwroot/index.html`, `TillApp.Client.MAUI/wwwroot/index.html`, and `docs/PORTFOLIO_ROADMAP.md`

## Files changed by Phase 8

- `TillApp.Shared/Dashboard/DashboardSummaryDto.cs`, `RecentOrderDto.cs`
- `TillApp.Server/Controllers/DashboardController.cs`, `Services/IDashboardService.cs`, `DashboardService.cs`, and `Program.cs`
- `TillApp.Client.Shared/Pages/Dashboard.razor`, `Layout/MainLayout.razor`, `_Imports.razor`, and `wwwroot/dashboard.css`
- `TillApp.Client.Shared/Services/IOrdersApiClient.cs`, `OrdersApiClient.cs`, and `TillApp.Client.Shared.Tests/DashboardComponentTests.cs`, `OrdersApiClientTests.cs`
- `TillApp.Server.Tests/DashboardApiTests.cs`
- `TillApp.Client.WASM/wwwroot/index.html`, `TillApp.Client.MAUI/wwwroot/index.html`, and `docs/PORTFOLIO_ROADMAP.md`

## Files changed by Phase 9

- `TillApp.Server/Infrastructure/ApiExceptionHandler.cs`, `ApiProblems.cs`, `Program.cs`, and category/product/order controllers
- `TillApp.Server/Services/IOrderService.cs`, `OrderService.cs`, and `ProductCatalogService.cs`
- `TillApp.Shared/Orders/OrderQuery.cs`
- `TillApp.Server.Tests/OrdersApiTests.cs` and `TillApp.Client.Shared.Tests/OrderQueryValidationTests.cs`
- `docs/PORTFOLIO_ROADMAP.md`
