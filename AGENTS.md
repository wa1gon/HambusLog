# AGENTS.md

## Scope and entry points
- Main desktop app is `HamBusLog` (Avalonia, `net10.0`); startup is `Program.cs` -> `App.OnFrameworkInitializationCompleted()` in `AppLogic.cs`.
- Solution also contains `Wa1gonLib`, `HamBusLog.Tests`, `Wa1gonLib.Tests`, and `ContestLicenseTool` (`HamBusLog.sln`).
- Existing repo guidance found: root `README.md` and `ContestLicenseTool/README.md` (no prior agent-specific rules file).

## Architecture you need to internalize first
- This codebase uses an app-level service locator pattern, not DI container wiring: shared singletons live on `App` (`AppLogic.cs`: `RigctldConnectionManager`, `DxSpotFeed`, `DxClusterReader`, `Toasts`, `DbContext`).
- Persistence boundary is EF Core via `HamBusLogDbContext` + `HamBusLogDbContextFactory`; default runtime DB is SQLite, with optional PostgreSQL factory support (`Data/HamBusLogDbContextFactory.cs`).
- DB context can be hot-swapped at runtime (`App.ReinitializeDbContext(...)`); UI windows subscribe to `App.DbContextReinitialized` and rebuild repositories (`Views/GridWindowLogic.cs`, `Views/DxSpotsWindowLogic.cs`).
- DX flow is TCP reader -> parsed spot feed -> VM subscription: `DxClusterTcpReader` reads ASCII lines, calls `App.DxSpotFeed.PublishLine`, and `DxSpotsWindowViewModel` listens on `SpotReceived`.
- Rig control flow is config-driven worker orchestration: `RigctldConnectionManager` groups radios by endpoint (dedupe localhost aliases) and runs one poll/control worker per unique host:port.
- Contest behavior is data-driven from config (`AppConfiguration.Contests`): `ContestCatalog` and `LogInputViewModel` shape required fields, exchange behavior, and time-window validation.

## Project-specific coding patterns
- Favor `AppConfigurationStore.Load()`/`Save()` for all persisted settings; this layer performs migration/normalization (legacy fields, defaults, profile handling).
- Normalize user-entered ham data aggressively (uppercase callsigns, state/county, mode) in VMs before persistence; follow examples in `ViewModels/LogInputViewModel.cs`.
- For new windows, register placement and toasts: `App.TrackWindowPlacement(this, nameof(WindowType))` and `App.Toasts.RegisterWindow(this)`.
- Prefer updating observable collections in place when preserving DataGrid selection/scroll matters (see `MainWindowViewModel.RefreshRadioStatuses`).
- If adding config fields, implement both load-time and save-time normalization in `AppConfigurationStore`; do not write ad-hoc JSON parsing in VMs.
- Global usings are centralized in `zGlobal.cs`; avoid duplicating common using directives unless file-local clarity requires it.

## Integration points and external dependencies
- QRZ lookups use `QrzLookupProvider` over `https://xmldata.qrz.com/xml/current/`; credentials are stored as `enc:` XOR-obfuscated text via `WeakSecretProtector` (not strong cryptography).
- DX region mapping and Cabrillo contest catalogs support user overrides under `~/.config/hambuslog/` with bundled fallback assets (`Data/DxRegionPrefixCatalog.cs`, `Data/CabrilloContestCatalog.cs`).
- App config file is `~/.config/hambuslog.json` (`Data/AppConfigurationStore.cs`); tests often mutate this real path and serialize access with a static lock.

## Build, test, and packaging workflows
- Build app: `dotnet build /home/darryl/github/Hambus/HamBusLog/HamBusLog.csproj`.
- Run desktop app: `dotnet run --project /home/darryl/github/Hambus/HamBusLog/HamBusLog.csproj`.
- Run all tests: `dotnet test /home/darryl/github/Hambus/HamBusLog/HamBusLog.sln`.
- README-documented release publish uses self-contained single-file output for `linux-x64` and `win-x64` (`README.md`).
- Debian package flow (documented in `README.md`) stages files under `debian/` then runs `dpkg-deb --build ...`.
- Contest license generation and signing workflows are in `ContestLicenseTool/README.md`; keep tool behavior aligned with `Services/ContestImportService.cs` license validation formats.

## High-value guardrails for agent edits
- Do not bypass `App` events (`DbContextReinitialized`, `QsoSaved`) when introducing features that affect grids/progress windows.
- When touching rig or cluster features, verify both runtime behavior and snapshot consumers (`MainWindowViewModel`, `LogInputViewModel`, `DxSpotsWindowViewModel`).
- Preserve backward compatibility shims in `Models/AppConfiguration.cs` and normalization paths in `AppConfigurationStore`.

