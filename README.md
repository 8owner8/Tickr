# Tickr

Steam trading card idler for Windows — farm cards on multiple accounts at once, with a desktop UI and fully automatic delta updates.

**Keywords:** Steam card farming, Steam trading card farmer, card idler, hour farming, playtime farming, Steam hours booster, multi-account Steam manager, Steam account automation, Steam Guard, Steam 2FA, game idler, AFK hours, Steam tools.

[![release](https://github.com/8owner8/Tickr/actions/workflows/release.yml/badge.svg)](https://github.com/8owner8/Tickr/actions/workflows/release.yml)
[![license](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE.txt)

## Features

- **Multi-account card farming** — idle Steam trading cards on many accounts simultaneously, automatically
- **Desktop app** — custom dark borderless UI (WebView2), system tray support
- **Automatic updates** — the launcher downloads only changed files, every one verified by SHA256
- **Steam Guard / 2FA** — import `.maFile` authenticators, confirm trades, sign in with QR code
- **Web API (IPC)** — full HTTP API for automation and remote control
- **Plugins** — bundled: ItemsMatcher, MobileAuthenticator, Monitoring (Prometheus + Grafana), SteamTokenDumper

## Download & install

1. Download **`Tickr.exe`** from the [latest release](https://github.com/8owner8/Tickr/releases/latest).
2. Put it in an empty folder and run it.
3. On first start the launcher downloads the rest of the application automatically, then starts Tickr. Later starts check for updates in the background and install them in seconds.

### Requirements

- Windows 10/11 (x64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) and **ASP.NET Core Runtime 10** (x64) — the launcher itself needs nothing, the main app runs via `dotnet`

## Updating

No action needed — `Tickr.exe` checks GitHub Releases on every start. Updates are mandatory, downloaded automatically with visible progress, and installed before the application starts.

## Building from source

Requirements: **.NET 10 SDK**

```sh
dotnet build Tickr.slnx
dotnet test Tickr.Tests/Tickr.Tests.csproj
```

Release builds are fully automated: push a version tag and the workflow publishes the release with the update manifest:

```sh
git tag v1.0.0
git push origin v1.0.0
```

## Project layout

| Path | What it is |
|---|---|
| `Tickr/` | Main application (WinForms + WebView2 UI, Kestrel IPC, Steam logic) |
| `Tickr.Launcher/` | Self-contained updater/launcher — ships as the single `Tickr.exe` |
| `Tickr.OfficialPlugins.*` | Bundled official plugins |
| `Tickr.CustomPlugins.*` | Example/utility custom plugins |
| `Tickr.Tests/` | Test suite (MSTest) |
| `scripts/` | Release tooling (update manifest generator) |
| `.github/workflows/` | Release pipeline |

## License

[Apache License 2.0](LICENSE.txt)
