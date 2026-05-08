# SpotiFade (Windows)

> *Trop de pub tue la pub.*

🇬🇧 [Read in English](README.en.md)

Coupe automatiquement le son des pubs Spotify sous Windows. Mute par
application via la Windows Audio Session API — seul Spotify est silencé,
tout le reste continue de jouer normalement.

![logo](Logo.png)

## Comment ça marche

- **Détection** — Windows expose les metadata media de toute app en
  cours de lecture via le SMTC (`Windows.Media.Control`). SpotiFade
  s'abonne au `MediaPropertiesChanged` de la session Spotify et inspecte
  titre / artiste / album. Une pub est détectée si :
  - `artist` ou `albumArtist` vaut `Spotify` (auto-promo) ;
  - le titre est un placeholder (`—`, vide…) avec artist/album vides ;
  - un mot-clé de pub localisé apparaît (`Annonce`, `Advertisement`,
    `Werbung`, `广告`, …).
- **Mute** — la session audio de Spotify est trouvée via WASAPI
  (`AudioSessionManager` → matching par nom de processus `Spotify.exe`),
  et `SimpleAudioVolume.Mute` est basculé. Dès que la piste suivante
  démarre, le mute est levé.
- **Tray uniquement** — tourne comme NotifyIcon mono-instance. Clic
  droit pour le statut / quitter. Aucune fenêtre console.

## Prérequis

- Windows 10 1809 ou plus récent (Windows 11 recommandé).
- [SDK .NET 8](https://dotnet.microsoft.com/download) — uniquement pour
  builder. Les utilisateurs finaux n'ont rien à installer si tu publies
  un build self-contained (voir plus bas).

## Build

```powershell
cd SpotiFade
dotnet build -c Release
dotnet run -c Release        # test rapide
```

Pour produire un .exe autonome qui tourne sans .NET installé :

```powershell
.\publish.ps1
```

Le binaire arrive dans `publish\SpotiFade.exe` (~70 Mo, runtime
embarqué). Pour un build plus léger dépendant du framework, édite
`publish.ps1` et retire `--self-contained true` — l'utilisateur final
devra alors avoir installé le .NET 8 Desktop Runtime.

## Lancement au démarrage

Clic droit sur l'icône tray → coche **Lancer au démarrage**. SpotiFade
écrit le toggle dans la clé Run par utilisateur du registre — aucun
privilège admin requis.

Pour une install portable, place `SpotiFade.exe` quelque part de stable
(ex. `%LOCALAPPDATA%\SpotiFade\`) **avant** d'activer l'auto-démarrage,
sinon le registre pointera vers un chemin susceptible de bouger.

## Licence

MIT — voir `LICENSE`.
