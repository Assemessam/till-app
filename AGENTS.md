# TillApp repository guidance

- Treat `docs/PORTFOLIO_ROADMAP.md` as the phase source of truth. Work on exactly one phase, update it after verification, and do not start the next phase without an explicit user request.
- Preserve the existing project responsibilities: `TillApp.Shared` holds API contracts, `TillApp.Server` owns EF entities/business logic, and `TillApp.Client.Shared` holds shared Razor UI and API clients. WASM and MAUI are thin hosts.
- Keep API DTOs separate from EF entities. Server code remains authoritative for prices, totals, validation, and state transitions; do not duplicate business rules in clients.
- When schema changes are requested, add EF migrations under `TillApp.Server/Data/Migrations` and keep `database/` scripts/documentation consistent when they are still supported.
- API integration tests require `TILLAPP_TEST_CONNECTION_STRING` for the disposable `TillAppTests` SQL Server database. Do not commit `.env` or credentials.
- The MAUI project is Android-only (`net10.0-android`); use `-f net10.0-android` for direct builds on this Linux workspace.
- Preserve user changes outside the active phase, including unrelated unstaged files.
