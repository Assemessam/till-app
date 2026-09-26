# Local development

This guide covers the supported local workflow for the ASP.NET Core API, SQL Server, browser client, Android MAUI client, and automated tests. Run commands from the repository root.

## Prerequisites

- .NET SDK `10.0.112` (the version in `global.json`)
- Docker Engine with the Docker Compose plugin
- For Android MAUI: the .NET `maui-android` workload, JDK 21, Android SDK/platform tools, and an Android emulator/device

## SQL Server and local configuration

Create a local environment file and replace the sample password with a strong local-only SQL Server password:

```bash
cp .env.example .env
chmod 600 .env
```

The root `.env` is ignored by Git. Keep credentials there; do not commit it or paste its contents into issues/logs. Start SQL Server and wait for its health check:

```bash
docker compose up -d sqlserver
docker compose ps
```

The Compose service publishes SQL Server at `localhost:1433` and persists data in the `tillapp-sqlserver-data` volume. Stop the container with `docker compose down`; this preserves the database volume. Avoid `docker compose down -v` unless you intentionally want to delete local databases.

Load local settings into the current shell before starting the API or running EF commands:

```bash
set -a
source .env
set +a
export ConnectionStrings__TillApp="$TILLAPP_CONNECTION_STRING"
```

`TILLAPP_CONNECTION_STRING` must point to the local `TillApp` database. The API applies EF migrations at startup only in the Development environment. On first run, it also inserts missing sample categories and products. Seed data is development-only and idempotent: existing rows are not overwritten, and repeated starts do not duplicate the sample rows.

## Start the API and inspect OpenAPI

In a terminal with the local settings loaded:

```bash
dotnet run --project TillApp.Server/TillApp.Server.csproj --launch-profile http
```

The API listens at `http://localhost:5080`. The Development environment enables the OpenAPI JSON document at:

```text
http://localhost:5080/openapi/v1.json
```

The document can be opened directly or supplied to an OpenAPI-compatible client. Development CORS permits the default WASM origins `http://localhost:5074` and `https://localhost:7024`.

## Start the WebAssembly client

In another terminal:

```bash
dotnet run --project TillApp.Client.WASM/TillApp.Client.WASM.csproj --launch-profile http
```

Open `http://localhost:5074`. The host reads its API URL from `TillApp.Client.WASM/wwwroot/appsettings.json` (`Api:BaseUrl`). If you change the WASM origin, update the Development CORS allow-list in `TillApp.Server/Program.cs` too.

## Run the Android MAUI client

The Android host targets `net10.0-android`. Build and optionally deploy/run on a configured emulator or device:

```bash
dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android
dotnet build TillApp.Client.MAUI/TillApp.Client.MAUI.csproj -f net10.0-android -t:Run
```

The default API address is `http://10.0.2.2:5080/`, which maps the Android emulator to the development machine. `TILLAPP_API_BASE_URL` overrides it when needed. Configure Android tooling and an emulator/device before using the run target.

## Run tests

Shared client tests do not need SQL Server:

```bash
dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj
```

Server integration tests use real SQL Server and a separate database named `TillAppTests`. With `.env` loaded, derive that test connection string and run the suite:

```bash
export TILLAPP_TEST_CONNECTION_STRING="${TILLAPP_CONNECTION_STRING/Database=TillApp;/Database=TillAppTests;}"
dotnet test TillApp.Server.Tests/TillApp.Server.Tests.csproj
```

Before running, confirm the test connection string names `TillAppTests`, not `TillApp` or another database containing data you want to keep. The integration-test fixture applies migrations and clears its test data; never point it at a personal or production database.

## EF Core migrations

Development startup applies pending migrations automatically. To inspect or manage migrations explicitly, restore the repository's local tool manifest (if needed), then use the Server project as both target and startup project:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations list \
  --project TillApp.Server/TillApp.Server.csproj \
  --startup-project TillApp.Server/TillApp.Server.csproj
```

EF commands use `ConnectionStrings__TillApp` from the current shell. Review generated migrations before applying them to any non-development database.

## Useful checks

```bash
dotnet build TillApp.sln
dotnet test TillApp.Client.Shared.Tests/TillApp.Client.Shared.Tests.csproj
dotnet test TillApp.Server.Tests/TillApp.Server.Tests.csproj
git diff --check
```
