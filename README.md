# Keep Alive

Keep Alive is a Windows tray utility for timed keep-awake sessions with manual controls, a countdown, automatic stop, and local audit history.

> [!IMPORTANT]
> Keep Alive is designed to prevent automatic system sleep during an explicitly started session. It does not bypass administrator-enforced locking, suppress screen savers, or simulate keyboard or mouse input.

## Current status

Phase 4 provides the complete timed-session interface, system-tray experience, persistent settings, and local session history. Users can start and stop sessions, select a preset or custom duration, watch the live countdown, minimize the app to the tray, and configure notifications.

The app supports 15-minute, 30-minute, 1-hour, 2-hour, and 4-hour presets plus custom durations from 1 minute through 8 hours. Settings include a default duration, notification preferences, and optional per-user launch at Windows sign-in. Session history is stored locally for up to 90 days or 1,000 records.

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
