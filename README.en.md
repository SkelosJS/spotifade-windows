# SpotiFade (Windows)

> *Trop de pub tue la pub.*

🇫🇷 [Lire en français](README.md)

Mute Spotify ads automatically on Windows. The mute is per-application
via the Windows Audio Session API — only Spotify is silenced, everything
else keeps playing.

![logo](Logo.png)

## How it works

- **Detection** — Windows exposes media metadata for every playing app
  via SMTC (`Windows.Media.Control`). SpotiFade subscribes to the
  Spotify session's `MediaPropertiesChanged` and inspects
  title / artist / album. An ad is flagged when:
  - `artist` or `albumArtist` equals `Spotify` (self-promo);
  - the title is a placeholder (`—`, empty…) with empty artist and
    album;
  - a localized ad keyword appears (`Annonce`, `Advertisement`,
    `Werbung`, `광고`, …).
- **Mute** — Spotify's audio session is located via WASAPI
  (`AudioSessionManager`, matched by process name `Spotify.exe`) and
  `SimpleAudioVolume.Mute` is flipped. As soon as the next track
  starts, the mute is released.
- **Tray-only** — SpotiFade runs as a single-instance NotifyIcon.
  Right-click for status and exit options. No console window.

## Requirements

- Windows 10 1809 or newer (Windows 11 recommended)
- [.NET 8 SDK](https://dotnet.microsoft.com/download) — for building
  only. End-users don't need anything pre-installed if the app is
  published as self-contained (see below).

## Build

```powershell
git clone https://github.com/skelos9692/spotifade-windows.git
cd spotifade-windows\SpotiFade
dotnet build -c Release
dotnet run -c Release        # quick test
```

To produce a self-contained .exe that runs without .NET installed:

```powershell
.\publish.ps1
```

The output lands in `publish\SpotiFade.exe` (~70 MB, runtime bundled).
For a smaller framework-dependent build, edit `publish.ps1` and drop
`--self-contained true` — the end user must then install the .NET 8
Desktop Runtime.

## Run at startup

Right-click the tray icon → check **Lancer au démarrage**. SpotiFade
writes the toggle to the per-user Run registry key — no admin
privileges required.

For a portable install, copy `SpotiFade.exe` to a stable location
(for example `%LOCALAPPDATA%\SpotiFade\`) **before** enabling
auto-start, otherwise the registry will point at a path that may move.

## License

MIT — see `LICENSE`.
