# AutoClicker — Portable Safe Fork

AutoClicker is a Windows desktop application for simple, local mouse automation. This fork is based on [oriash93/AutoClicker](https://github.com/oriash93/AutoClicker) and keeps the upstream MIT license while focusing on reliability, portability, and predictable stop behavior.

## What this fork adds

- Portable Windows x64 release with the .NET 8 runtime included.
- Cancellation-aware click engine with one active run at a time.
- Checked Windows `SendInput` mouse dispatch instead of the legacy `mouse_event` API.
- Left, right, middle, X1 (Mouse 4), and X2 (Mouse 5) buttons.
- Single and double click actions.
- Infinite or fixed repeat counts.
- Current cursor position or fixed screen coordinates.
- Global Start, Stop, and Toggle hotkeys with conflict validation and rollback when Windows refuses a registration.
- Optional bounded timing variance.
- Safe minimum possible delay of 25 ms to avoid accidental event floods.
- Portable local settings with recovery from missing or corrupt JSON.
- Locked dependencies, automated tests, Windows publish checks, startup smoke testing, and SHA-256 release hashes.

## Quick start

1. Download `AutoClicker-Portable-win-x64.zip` from the [BreezeDelegate releases](https://github.com/BreezeDelegate/AutoClicker/releases) page.
2. Extract the ZIP anywhere writable.
3. Run `AutoClicker.exe`.
4. Choose an interval, mouse button, click type, repeat mode, and cursor mode.
5. Start with the UI or the default **F6** hotkey. Stop with **F7**. Toggle with **F8**.

No installer or separately installed .NET runtime is required.

## Timing and variance

The base interval must be at least **25 ms**. Optional variance is expressed in milliseconds and is applied around the base interval, but configuration is rejected if it could produce a delay below 25 ms.

Example: a 100 ms interval with ±25 ms variance produces delays between 75 ms and 125 ms.

## Settings and logs

The app is local-only and has no telemetry, updater, cloud account, or background network dependency.

It first tries to use a `data` directory next to `AutoClicker.exe`. If that location is not writable, it falls back to `%APPDATA%\AutoClicker`.

Settings are written atomically. Missing, unreadable, or corrupt settings fall back to safe defaults instead of crashing startup.

## Hotkeys

Defaults:

- **F6** — Start
- **F7** — Stop
- **F8** — Toggle

Start, Stop, and Toggle may be customized. Conflicting bindings are rejected. Hotkey changes are disabled while a click run is active so the Stop path remains stable.

## Safety model

This fork uses ordinary Windows desktop input APIs only. It does **not** use DLL injection, kernel drivers, process-memory access, anti-cheat bypasses, stealth techniques, hidden persistence, autostart, or elevated privileges.

It is intended for ordinary desktop automation. Do not use automation for destructive or irreversible actions without supervision.

## Build and test

Requirements: .NET 8 SDK. Building the WPF application requires Windows or Windows targeting support.

```powershell
dotnet restore AutoClicker/AutoClicker.sln --locked-mode
dotnet test AutoClicker.Tests/AutoClicker.Tests.csproj -c Release --no-restore
dotnet publish AutoClicker/AutoClicker.csproj -c Release --no-restore -o publish
./scripts/package-release.ps1 -PublishDirectory publish -OutputDirectory artifacts
```

The portable publish target is `win-x64`, self-contained, single-file, and non-trimmed. Native WPF runtime libraries and application resources remain beside the executable where required by the runtime.

## Verification

Each release includes:

- `AutoClicker-Portable-win-x64.zip`
- `SHA256SUMS.txt`

Verify the ZIP against the published SHA-256 before running it if you want to confirm the download is intact.

## Upstream and license

This repository is a maintained fork of [oriash93/AutoClicker](https://github.com/oriash93/AutoClicker). The original project copyright and MIT license are retained in `LICENSE`. Fork-specific changes are maintained by BreezeDelegate and do not imply endorsement by the upstream project.
