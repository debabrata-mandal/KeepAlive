# Keep Alive

Keep Alive is a Windows tray utility for timed keep-awake sessions with manual controls, a countdown, automatic stop, and local audit history.

> [!IMPORTANT]
> Keep Alive is designed to prevent automatic system sleep during an explicitly started session. It does not bypass administrator-enforced locking, suppress screen savers, or simulate keyboard or mouse input.

## Current status

Phase 1 establishes the Windows application shell, automatic system-theme styling, single-instance activation, tests, and continuous integration. The timed keep-awake engine and tray controls are planned for later phases.

## Requirements

- Windows 11 x64 (Windows 10 is best effort)
- .NET 10 SDK for development

## Build and test

```powershell
dotnet restore KeepAlive.sln
dotnet build KeepAlive.sln --configuration Release --no-restore
dotnet test KeepAlive.sln --configuration Release --no-build
```

Run the development build with:

```powershell
dotnet run --project src/KeepAlive/KeepAlive.csproj
```

## Repository layout

- `src/KeepAlive` — WPF desktop application
- `tests/KeepAlive.Tests` — automated tests
- `.github/workflows/ci.yml` — Windows build and test workflow
