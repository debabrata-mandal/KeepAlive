# Keep Alive

Keep Alive is a Windows tray utility for timed keep-awake sessions with manual controls, a countdown, automatic stop, and local audit history.

> [!IMPORTANT]
> Keep Alive is designed to prevent automatic system sleep during an explicitly started session. It does not bypass administrator-enforced locking, suppress screen savers, or simulate keyboard or mouse input.

## Design goals

- Keep operation explicit through user-initiated, time-limited sessions.
- Stop every session automatically at its configured deadline.
- Make the current state visible through the window and system-tray indicator.
- Keep settings and audit history local to the current Windows user.
- Avoid administrator privileges and respect organization-managed security policies.
- Remain lightweight, predictable, and easy to remove.

## Features

- Manual start and stop controls with a live countdown.
- Preset durations of 15 minutes, 30 minutes, 1 hour, 2 hours, and 4 hours.
- Custom durations from 1 minute through 8 hours.
- System-tray controls and configurable notifications.
- Persistent default duration and launch-at-sign-in preferences.
- Local session history retained for up to 90 days or 1,000 records.

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

## Create a portable Windows package

From PowerShell, run:

```powershell
./build/Publish.ps1 -Version 0.1.0
```

The script creates a self-contained `win-x64` ZIP and SHA-256 checksum under `artifacts/releases`. The target computer does not need a separate .NET installation.

Pushes to `main` and pull requests run the build and test workflow with an automatic CI version such as `0.1.0-ci.27`. CI builds are validation builds and are not published for end users.

Pushing a semantic-version tag creates a GitHub Release with generated release notes and attaches the portable ZIP and checksum:

```powershell
git tag -a v0.1.0 -m "Release 0.1.0"
git push origin v0.1.0
```

Stable tags such as `v1.2.3` are marked as the latest release. Tags such as `v1.2.3-rc.1` are marked as prereleases. Published files can be downloaded directly from the repository’s Releases page.

## Install and remove

1. Extract the ZIP to a stable per-user location, such as `%LOCALAPPDATA%\Programs\KeepAlive`.
2. Run `KeepAlive.exe`.
3. Optionally enable **Start Keep Alive when I sign in** on the Settings tab.

To remove the app, first disable **Start Keep Alive when I sign in**, exit from the tray menu, and delete the extracted folder. User settings and history are stored in `%LOCALAPPDATA%\KeepAlive` and can be deleted separately if they are no longer needed.

The portable executable is currently unsigned. Windows or organizational security tools may display a warning, so distribution should follow the applicable IT approval and code-signing process.

## Repository layout

- `src/KeepAlive` — WPF desktop application
- `tests/KeepAlive.Tests` — automated tests
- `.github/workflows/ci.yml` — Windows build and test workflow
- `.github/workflows/release.yml` — tag-driven GitHub Release workflow
- `build/Publish.ps1` — portable Windows packaging script
