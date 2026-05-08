# SpotiFade (Windows)

> *Trop de pub tue la pub.*

🇫🇷 [Lire en français](README.md)

Mute Spotify ads automatically on Windows. Per-app mute via the Windows Audio
Session API — only Spotify gets silenced, everything else keeps playing.

![logo](Logo.png)

## How it works

- **Detection** — Windows exposes media metadata for any playing app via
  the SMTC (`Windows.Media.Control`). SpotiFade subscribes to the Spotify
  session's `MediaPropertiesChanged` and inspects title/artist/album. Ads
  are flagged when one of those contains a localized ad keyword
  (`Advertisement`, `Annonce`, `Werbung`, `广告`, …).
- **Mute** — when an ad is detected, the Spotify audio session is found
  via WASAPI (`AudioSessionManager` → match by process name `Spotify.exe`)
  and `SimpleAudioVolume.Mute` is flipped. As soon as the next track
  starts, the mute is released.
- **Tray-only** — runs as a single-instance NotifyIcon. Right-click for
  status / quit. No console window.

## Requirements

- Windows 10 1809 or later (Windows 11 recommended).
- [.NET 8 SDK](https://dotnet.microsoft.com/download) — only for building.
  End-users don't need anything pre-installed if you publish a self-contained
  build (see below).

## Build

```powershell
cd SpotiFade
dotnet build -c Release
dotnet run -c Release        # quick test
```

To produce a single self-contained EXE that runs without .NET installed:

```powershell
.\publish.ps1
```

The output lands in `publish\SpotiFade.exe` (~70 MB because it bundles the
runtime). For a smaller framework-dependent build, edit `publish.ps1` and
drop `--self-contained true` — the end user will then need the .NET 8
Desktop Runtime.

## Run on startup

Right-click the tray icon → check **Lancer au démarrage**. SpotiFade writes
the toggle to the per-user Run registry key — no admin privileges needed.

For a portable install, drop `SpotiFade.exe` anywhere (e.g.
`%LOCALAPPDATA%\SpotiFade\`) before enabling auto-start, otherwise the
registry will point at a path that may move.

## License

MIT — see `LICENSE`.
