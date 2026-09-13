# TillApp

TillApp is a take-home full-stack food-ordering application. It provides an ASP.NET Core REST API backed by SQL Server, a Blazor WebAssembly browser client, and a .NET MAUI Blazor Hybrid Android client that reuse a shared Razor UI.

## Technology stack

- .NET 10 and C#
- ASP.NET Core controllers and built-in OpenAPI
- Entity Framework Core 10 with SQL Server 2022 Express
- Blazor WebAssembly
- .NET MAUI Blazor Hybrid for Android
- Razor Class Library for shared client UI
- xUnit with ASP.NET Core's integration-test host

## Solution architecture

| Project | Responsibility |
| --- | --- |
| `TillApp.Shared` | Transport contracts and validation shared across process boundaries; no EF Core or hosting dependencies. |
| `TillApp.Server` | ASP.NET Core API, server-only entities, EF configuration, migrations, and order service. |
| `TillApp.Client.Shared` | Razor pages, layouts, components, client services, presentation models, and shared static assets. |
| `TillApp.Client.WASM` | Thin browser host responsible for startup, DI, routing, and browser API configuration. |
| `TillApp.Client.MAUI` | Thin Android host responsible for native startup, DI, and emulator/device API configuration. |
| `TillApp.Server.Tests` | API integration tests backed by the real SQL Server container. |

Clients never reference the server. EF entities stay in the server, while API request and response types stay in `TillApp.Shared`.

## Local SQL Server

Prerequisites are Docker with Compose and the .NET 10 SDK pinned by `global.json`.

Create the ignored local environment file and replace both placeholders with the same strong development password:

```bash
cp .env.example .env
chmod 600 .env
```

Start only the project SQL Server service and wait until it reports `healthy`:

```bash
docker compose up -d sqlserver
docker compose ps
```

The Compose project uses the official `mcr.microsoft.com/mssql/server:2022-latest` image in Express mode, container `tillapp-sqlserver`, volume `tillapp-sqlserver-data`, and host port `1433`.

## Connection configuration and migrations

Load the ignored development values into the current shell, map the local connection string to ASP.NET Core's hierarchical configuration name, and apply migrations:

```bash
set -a
source .env
set +a
export ConnectionStrings__TillApp="$TILLAPP_CONNECTION_STRING"

dotnet tool run dotnet-ef database update \
  --project TillApp.Server/TillApp.Server.csproj \
  --startup-project TillApp.Server/TillApp.Server.csproj
```

Create a future migration with:

```bash
dotnet tool run dotnet-ef migrations add MigrationName \
  --project TillApp.Server/TillApp.Server.csproj \
  --startup-project TillApp.Server/TillApp.Server.csproj \
  --output-dir Data/Migrations
```

The reviewer-friendly standalone schema is in `database/create-database.sql`. It can be applied independently of EF Core:

```bash
docker exec -i tillapp-sqlserver sh -c \
  '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b' \
  < database/create-database.sql
```

`database/ef-migrations.sql` is the separately generated idempotent EF migration script.

## Run the API

After exporting `ConnectionStrings__TillApp` as above:

```bash
dotnet run --project TillApp.Server/TillApp.Server.csproj --launch-profile http
```

The API listens at `http://localhost:5080`. Development OpenAPI JSON is available at `http://localhost:5080/openapi/v1.json`.

## Run the WebAssembly client

Keep the API running and start the browser client in a second terminal:

```bash
source .env.toolchain
dotnet run --project TillApp.Client.WASM/TillApp.Client.WASM.csproj --launch-profile http
```

Open `http://localhost:5074`. The client reads the API address from `TillApp.Client.WASM/wwwroot/appsettings.json`; shared Razor components contain no hardcoded host address.

The shared UI provides:

- An intentional TillApp landing page.
- A responsive New Order form with ten product choices, duplicate item selection, removal, client validation, a live total, and submission feedback.
- An unpaid-order list with deliberate loading, empty, retry/error, and per-order payment states.
- Immediate removal from the unpaid list after a successful payment request.

For local troubleshooting:

- Confirm SQL Server is ready with `docker compose ps`; it must report `healthy`.
- Confirm the API responds at `http://localhost:5080/openapi/v1.json`.
- Confirm WASM is running at `http://localhost:5074`, which is explicitly allowed by the API's development CORS policy.
- If the UI reports that the service is unavailable, check the API process first; form/order state is retained so the operation can be retried.

## API endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/orders?isPaid={bool}` | List orders, optionally filtered by payment state |
| `GET` | `/api/orders/{id}` | Retrieve one order with its items |
| `POST` | `/api/orders` | Create an unpaid order; the server calculates its amount |
| `PUT` | `/api/orders/{id}` | Replace the editable name/items and recalculate the amount; payment state is preserved |
| `PATCH` | `/api/orders/{id}/paid` | Idempotently mark an order paid |
| `DELETE` | `/api/orders/{id}` | Delete an order; its items cascade-delete |

Invalid request bodies return standard `ValidationProblemDetails`. Unknown identifiers return `404`.

## Tests

The API tests use a separate `TillAppTests` database on the real SQL Server container, not EF InMemory. With `.env` loaded:

```bash
export TILLAPP_TEST_CONNECTION_STRING="${TILLAPP_CONNECTION_STRING/Database=TillApp;/Database=TillAppTests;}"
dotnet test TillApp.Server.Tests/TillApp.Server.Tests.csproj
```

Run the complete build and formatting checks with:

```bash
dotnet restore TillApp.sln
dotnet build TillApp.sln
dotnet format TillApp.sln --verify-no-changes --no-restore
```

## Shared UI hosting and platform scope

Routable components and layouts live in `TillApp.Client.Shared`. Both hosts include that Razor Class Library as an additional routing assembly and consume its `_content/TillApp.Client.Shared/` static assets.

The browser host reads `Api:BaseUrl` from its `wwwroot/appsettings.json`. The Android host uses the emulator host mapping `10.0.2.2` and permits a `TILLAPP_API_BASE_URL` override. Shared components contain no environment-specific API address.

The MAUI project targets `net10.0-android` for Ubuntu. iOS and Mac Catalyst are not built or tested because no Mac build host is available. The shared UI compiles into the MAUI Android host, but MAUI runtime behavior and detailed Android deployment instructions belong to a later phase.
