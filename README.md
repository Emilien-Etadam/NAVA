# NAVA

Contrôleur d'amplificateur audio-vidéo réseau

> **English.** This repository is a French-localized fork of [NAVA by anko (azuki2anko)](https://github.com/azuki2anko/NAVA), published under the MIT license. The Windows app follows the display language: French, English or Japanese, and anything else falls back to French. The choice can also be saved in Settings and is applied on the next launch. A self-contained portable build is uploaded on every push and pull request. Pushing a tag such as `v0.1.0-fr.1` creates a GitHub Release with the zip attached. The full guide below is in French.

NAVA pilote un amplificateur ou une enceinte Yamaha MusicCast compatible avec l'API Yamaha Extended Control, depuis Windows 11. Cette version est un **fork traduit en français** du projet original d'[anko (azuki2anko)](https://github.com/azuki2anko/NAVA). Le comportement est celui de l'amont : seules les chaînes visibles, l'installateur et ce guide ont été traduits.

L'application choisit la langue au démarrage :

- français si Windows est en français, ou si la langue n'est ni l'anglais ni le japonais ;
- anglais si Windows est en anglais ;
- japonais si Windows est en japonais.

Le choix peut être modifié dans **Réglages**, section Comportement. Il est enregistré et pris en compte au prochain démarrage.

**[Télécharger la version portable Windows 11 x64](https://github.com/Emilien-Etadam/NAVA/releases/latest)** une fois la release `v0.1.0-fr.1` publiée. Le fichier s'appelle `NAVA-0.1.0-fr.1-win-x64-portable.zip`. L'installateur Inno Setup, en français et en anglais, se construit localement avec `build/Build-Distributions.ps1` (Inno Setup 6). Il n'est pas joint automatiquement à la release GitHub : la release contient le zip portable et `SHA256SUMS.txt`.

La version portable d'origine, en japonais, reste disponible chez l'auteur : [NAVA 0.1.0](https://github.com/azuki2anko/NAVA/releases/tag/v0.1.0).

Le guide détaillé `doc/USER_GUIDE.md` et les autres documents de `doc/` sont encore ceux de l'amont, en japonais.

### Si Windows affiche « non téléchargé fréquemment » ou « PC protégé »

NAVA n'est pas signé. SmartScreen peut donc avertir au premier téléchargement. Avant d'exécuter le fichier :

1. Vérifiez qu'il provient de la [release de ce dépôt](https://github.com/Emilien-Etadam/NAVA/releases/latest).
2. Comparez son SHA-256 avec `SHA256SUMS.txt` publié à côté du zip.

```powershell
Get-FileHash .\NAVA-0.1.0-fr.1-win-x64-portable.zip -Algorithm SHA256
```

Si l'avertissement parle d'un logiciel malveillant identifié, n'exécutez pas le fichier. Il n'est pas nécessaire de désactiver SmartScreen.

NAVA reste une application de zone de notification, centrée sur l'usage quotidien de la zone Main. Ce n'est pas une copie complète de la télécommande Yamaha.

> [!IMPORTANT]
> L'amont n'a vérifié l'application que sur un RX-V4A. Les autres modèles, dont un MusicCast 50, suivent la spécification Yamaha Extended Control (Basic / Advanced) Rev. 2.00 et la réponse `getFeatures`. Ce fonctionnement compatible n'est pas une garantie.

Projet communautaire non officiel. Yamaha Corporation ne le fournit pas, ne l'approuve pas et ne le garantit pas.

## Fonctions

- Alimentation, veille, source, volume et sourdine
- Champ sonore, 3D Surround, Direct, Pure Direct, Enhancer
- Tonalité, égaliseur et balance
- Découverte SSDP, cache de voisinage Windows, ping avec confirmation, connexion manuelle
- Fenêtre WPF, zone de notification et mini-commande d'alimentation toujours visible
- Réduction dans la zone de notification et lancement à l'ouverture de session
- Raccourcis clavier globaux
- API REST typée limitée à `127.0.0.1`, avec OpenAPI
- Activités, blocage d'allumage et actions enregistrées
- Réglages JSON, journaux structurés, délais, interrogation et reconnexion
- Français, anglais et japonais

Les fonctions, sources, champs sonores et plages viennent de `getFeatures`. Une commande que l'appareil n'annonce pas n'est ni affichée ni acceptée par l'API. Le tuner reste en attente, comme dans l'amont.

## Environnement

- Windows 11 x64
- Le PC et l'amplificateur sur le même réseau local
- Un amplificateur Yamaha pilotable par le réseau

Aucun accès Internet ni télémétrie n'est requis pour l'usage normal.

## Démarrer

Décompressez le zip portable et lancez `NAVA.exe`. L'application se place dans la zone de notification et commence une recherche en lecture seule. Le premier écran sert aux commandes quotidiennes. **Réglages** contient l'adresse, la carte réseau, la langue et les raccourcis.

Depuis les sources :

```powershell
dotnet restore NetworkAVAmp.Controller.sln
dotnet run --project src/RxV4A.Desktop/RxV4A.Desktop.csproj -c Debug
```

## API locale

- API : `http://127.0.0.1:55274/api/v1`
- OpenAPI : `http://127.0.0.1:55274/openapi/v1.json`

L'API n'accepte que des opérations typées. Ce n'est pas un proxy vers n'importe quelle commande Yamaha. Les messages suivent la langue choisie pour l'application. Les codes d'erreur restent stables. Le guide d'exemples est encore en japonais : [doc/API_GUIDE.md](doc/API_GUIDE.md).

## Documents

- [Guide d'utilisation amont, en japonais](doc/USER_GUIDE.md)
- [Guide de l'API locale, en japonais](doc/API_GUIDE.md)
- [Historique](CHANGELOG.md)
- [Contribution, en japonais](CONTRIBUTING.md)
- [Politique de sécurité, en japonais](SECURITY.md)
- Projet original : [azuki2anko/NAVA](https://github.com/azuki2anko/NAVA)

## Build et publication

```powershell
dotnet restore NetworkAVAmp.Controller.sln
dotnet build NetworkAVAmp.Controller.sln -c Release --no-restore
dotnet test NetworkAVAmp.Controller.sln -c Release --no-build --no-restore
dotnet format NetworkAVAmp.Controller.sln --verify-no-changes --no-restore
dotnet publish src/RxV4A.Desktop/RxV4A.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/publish
```

Le zip portable officiel, avec la documentation et les satellites de langue (`en` et `ja` à côté de `NAVA.exe`), se construit ainsi. L'installateur demande Inno Setup 6 ; `-SkipInstaller` produit seulement le zip.

```powershell
.\build\Build-Distributions.ps1 -SkipInstaller
```

GitHub Actions, sur `windows-latest`, vérifie le build, les tests et le format, puis publie le zip en artefact à chaque push et chaque pull request. Un tag `v*` crée en plus une GitHub Release avec ce zip.

## Vie privée

Les réglages et les journaux sont dans `%LocalAppData%\NAVA`. N'ajoutez pas le fichier de réglages ni un journal brut à un ticket. N'y mettez pas d'adresse IP, d'adresse MAC, d'identifiant d'appareil, de jeton, de clé ou de chemin personnel.

La version publique n'exécute pas d'URL, de commande, de séquence de touches ou d'appel Yamaha arbitraire fourni de l'extérieur. Commencez la vérification sur un appareil réel par des lectures seules. Voir [SECURITY.md](SECURITY.md).

## Crédit et licence

Œuvre originale : **anko** ([azuki2anko/NAVA](https://github.com/azuki2anko/NAVA)), Copyright (c) 2026 anko.

Ce dépôt en est un fork traduit. La licence MIT d'origine est conservée : [LICENSE](LICENSE).
