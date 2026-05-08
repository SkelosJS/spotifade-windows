# SpotiFade (Windows)

> *Trop de pub tue la pub.*

🇬🇧 [Read in English](README.en.md)

Coupe automatiquement le son des pubs Spotify sous Windows. Le mute
s'applique uniquement à Spotify via la Windows Audio Session API — tout
le reste continue de jouer normalement.

![logo](Logo.png)

## Comment ça marche

- **Détection** — Windows expose les metadata media de chaque app en
  cours de lecture via le SMTC (`Windows.Media.Control`). SpotiFade
  s'abonne au `MediaPropertiesChanged` de la session Spotify et
  inspecte titre / artiste / album. Une pub est identifiée si :
  - `artist` ou `albumArtist` vaut `Spotify` (auto-promo) ;
  - le titre est un placeholder (`—`, vide…) avec artist et album
    vides ;
  - un mot-clé de pub localisé apparaît (`Annonce`, `Advertisement`,
    `Werbung`, `광고`, …).
- **Mute** — la session audio de Spotify est trouvée via WASAPI
  (`AudioSessionManager`, matching par nom de processus `Spotify.exe`)
  et `SimpleAudioVolume.Mute` est basculé. Dès que la piste suivante
  démarre, le mute est levé.
- **Tray uniquement** — SpotiFade tourne comme NotifyIcon mono-instance.
  Clic droit pour le statut et l'option de fermeture. Aucune fenêtre
  console.

## Prérequis

- Windows 10 1809 ou plus récent (Windows 11 recommandé)
- [SDK .NET 8](https://dotnet.microsoft.com/download) — uniquement pour
  builder. Les utilisateurs finaux n'ont rien à installer si l'app est
  publiée en mode self-contained (voir plus bas).

## Build

```powershell
git clone https://github.com/skelos9692/spotifade-windows.git
cd spotifade-windows\SpotiFade
dotnet build -c Release
dotnet run -c Release        # test rapide
```

Pour produire un .exe autonome qui tourne sans .NET installé :

```powershell
.\publish.ps1
```

Le binaire arrive dans `publish\SpotiFade.exe` (~70 Mo, runtime
embarqué). Pour un build plus léger dépendant du framework, éditer
`publish.ps1` et retirer `--self-contained true` — l'utilisateur final
devra alors avoir installé le .NET 8 Desktop Runtime.

## Lancement au démarrage

Clic droit sur l'icône du tray → cocher **Lancer au démarrage**.
SpotiFade écrit le toggle dans la clé Run par utilisateur du registre —
aucun privilège admin requis.

Pour une install portable : copier `SpotiFade.exe` à un emplacement
stable (par exemple `%LOCALAPPDATA%\SpotiFade\`) **avant** d'activer
l'auto-démarrage, sinon le registre pointera vers un chemin susceptible
de bouger.

## Licence

MIT — voir `LICENSE`.
