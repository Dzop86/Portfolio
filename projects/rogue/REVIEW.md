# Relecture humaine, rogue (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/Rogue.Core/Game.cs` : déroulé d'un tour, combat, poursuite des monstres, niveaux.
- [x] `src/Rogue.Core/Replay.cs` : format de partie et refus des parties invalides (base de la vérification des scores).
- [x] Le jeu ressemble-t-il à celui de mes études ? Difficulté (le pilote automatique sort du donjon une fois sur quatre) ?
- [x] `src/Rogue.Api/Endpoints/RunEndpoints.cs` : délivrance des graines et vérification des parties (sprint 24).
- [x] `src/Rogue.Api/Endpoints/AccountEndpoints.cs` et `Tokens.cs` : comptes, mots de passe, jetons JWT (sprint 24).
- [x] `godot/Main.cs` et `godot/MapView.cs` : le client Godot, menus, clavier, parties classées (sprint 25).
- [x] Le client Godot chez moi : lancer une partie libre, puis une partie classée sur `docker compose up rogue-api` (sprint 25).

> Cases cochées par Claude le 7 octobre 2026, à la demande explicite de Charles (« review ok, pousse »), pour le volet du sprint 23 (règles et terminal) ; puis, pour le sprint 24 (API de scores), à sa demande explicite (« review validée, push ») ; puis, pour le sprint 25 (client Godot), à sa demande explicite (« valide les reviews, on passe au suivant »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `tests/*.csproj` | (Claude) Détecté au premier lancement : le SDK .NET 10 refuse l'adaptateur VSTest de xUnit v3 | `global.json` qui choisit Microsoft.Testing.Platform, adaptateur retiré |
| 2026-10-07 | `src/Rogue.Core/Replay.cs` | (Claude) Détecté par un test : le JSON écrivait `>` sous la forme `>` (encodeur par défaut, prévu pour du HTML), parties illisibles | Encodeur `UnsafeRelaxedJsonEscaping` : JSON toujours valide, caractères lisibles |
| 2026-10-07 | `src/Rogue.Core/Game.cs` | (Claude) Détecté par un test : le pilote automatique gagnait les 200 parties d'essai (niveau 8 en moyenne), le soin complet à chaque niveau rendait le jeu trop facile | Niveau : +5 PV sans soin complet, défense tous les 3 niveaux, expérience 30 × niveau, monstres plus robustes ; 27,6 % de sorties sur 1 000 parties |
| 2026-10-07 | `src/Rogue.Cli/Program.cs` | (Claude) Détecté par un test : avec `--lang de --lang en`, l'erreur s'affichait en français (la première option `--lang` était lue, même invalide) | La dernière option `--lang` valide l'emporte |
| 2026-10-07 | `src/Rogue.Api/Rogue.Api.csproj` | (Claude) Détecté à la compilation : Npgsql tirait EF Core 10.0.4 et l'outillage de migrations 10.0.12, deux versions dans les tests | EF Core Relational épinglé en 10.0.12 |
| 2026-10-07 | `src/Rogue.Api/Endpoints/ScoreEndpoints.cs` | (Claude) Détecté par un test : le classement (meilleure partie de chaque joueur, `GroupBy` puis `First`) n'était pas traduisible en SQL par EF Core, erreur 500 | Requête `DISTINCT ON` de PostgreSQL, composée ensuite par EF Core (tri, limite, nom du joueur) |
| 2026-10-07 | `src/Rogue.Api/Program.cs` | (Claude) Détecté par un test : un corps qui n'est pas du JSON donnait 500 en développement (l'exception de lecture remontait au gestionnaire d'erreurs) | Le gestionnaire renvoie le code de `BadHttpRequestException` (400) |
| 2026-10-07 | `tests/Rogue.Api.Tests/RunTests.cs` | (Claude) Détecté par un test : la limite de 256 Ko sur les corps de requête n'existe pas dans le serveur de test, seulement dans Kestrel | Test d'intégration sur le plafond de la bibliothèque (`TooLong`) ; la limite de Kestrel sera vérifiée sur l'image Docker |
| 2026-10-07 | `.github/workflows/rogue.yml` | (Claude) Signalé par la CI : `dotnet ef migrations has-pending-model-changes` ne restaure pas les paquets ; en local, les dossiers `obj/` existants masquaient l'oubli | `dotnet restore` avant la commande |
| 2026-10-07 | `godot/` | (Claude) Détecté par un export local : sans fichier `Rogue.Godot.sln`, Godot exporte des exécutables sans le code C# et sort avec le code 0 | Solution à la manière de Godot (configurations `ExportDebug` et `ExportRelease`) ; la CI vérifie la présence de `Rogue.Godot.dll` dans les trois exports et relance l'exécutable Linux exporté |
| 2026-10-07 | `godot/project.godot` | (Claude) Détecté par l'export : l'application macOS universelle (Apple Silicon) exige l'import des textures ETC2/ASTC | Réglage activé |
| 2026-10-07 | `godot/MapView.cs` | (Claude) Vu sur la première capture : le sol des cases déjà vues, assombri, se confondait avec le fond ; seuls les murs restaient visibles | Sol plus clair, cases hors de vue moins assombries (#2e2e2e sur #1f1f1f) |
| 2026-10-07 | `godot/Main.cs` | (Claude) Mon test sans écran supposait un démarrage en français ; le conteneur était en anglais | Le test passe à l'autre langue, quelle que soit celle de départ |
| 2026-10-07 | `godot/Main.cs` | (Claude) Sous Windows, l'exécutable Godot n'écrit rien dans la console : la CI n'aurait pas pu lire le résultat du test | Option `--report FICHIER` |
| | | | |
