# Historique

Ce fork suit l'esprit de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/) et [SemVer](https://semver.org/lang/fr/).

## [Unreleased]

## [0.1.0-fr.1] - 2026-10-18

Fork traduit de [NAVA](https://github.com/azuki2anko/NAVA). Le comportement n'est pas modifié.

### Added

- Interface en français (langue neutre), anglais et japonais, choisie selon la langue d'affichage de Windows, avec un réglage enregistré et appliqué au redémarrage
- Installateur Inno Setup en français et en anglais
- Artefact portable autonome à chaque push ou pull request, et GitHub Release du zip lorsqu'un tag `v*` est poussé

### Changed

- Version `0.1.0-fr.1`
- README en français, avec un résumé en anglais et le crédit de l'auteur original anko

Cette version reprend aussi les changements amont encore non publiés dans la 0.1.0 :

- la mini-commande d'alimentation est ramenée dans la zone de travail de chaque écran, pour ne pas masquer un clic sur une icône de la barre des tâches ;
- son affichage est mis à jour dès le clic, sans revenir à un état ancien pendant la communication ;
- un réglage l'affiche au démarrage de NAVA.

## [0.1.0] - 2026-09-06

Première publication amont, par anko. Le texte d'origine est en japonais dans [azuki2anko/NAVA](https://github.com/azuki2anko/NAVA/blob/v0.1.0/CHANGELOG.md).

### Added

- Détection pilotée par les capacités et lecture de la zone Main pour les amplificateurs Yamaha Extended Control
- Alimentation, source, volume, sourdine, champ sonore, traitements, tonalité, égaliseur et balance
- Fenêtre WPF, zone de notification et mini-commande d'alimentation au premier plan
- API REST locale et OpenAPI
- Raccourcis globaux, activités, blocage d'allumage et contrat d'actions enregistrées
- Journaux structurés, reconnexion automatique et tests simulés
- Vérification GitHub Actions du build, des tests, du format et de la publication autonome
- Guide, politique de sécurité, modèles de tickets et script de distribution portable et installateur

### Changed

- Nom du produit : NAVA, sous-titre Network AV Amp Controller
- Écran quotidien organisé autour du volume circulaire, de l'alimentation, de la sourdine, de la source et du champ sonore

[Unreleased]: https://github.com/Emilien-Etadam/NAVA/compare/v0.1.0-fr.1...HEAD
[0.1.0-fr.1]: https://github.com/Emilien-Etadam/NAVA/releases/tag/v0.1.0-fr.1
[0.1.0]: https://github.com/azuki2anko/NAVA/releases/tag/v0.1.0
