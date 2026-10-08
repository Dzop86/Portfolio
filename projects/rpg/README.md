# rpg : un RPG tactique à la manière de Dofus

[![rpg](https://github.com/Dzop86/Portfolio/actions/workflows/rpg.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/rpg.yml)

Un RPG tactique au tour par tour, dans l'esprit de Dofus et de Wakfu, écrit de zéro : aucun code, aucun nom, aucune image d'Ankama (D54). Six sprints : les règles du combat (sprint 45, ce dossier), le combat isométrique dans Godot 4 (46), le serveur de comptes et de personnages (47), la création de personnage (48), la ville d'accueil et ses PNJ (49), le launcher en Rust et les exécutables (50). Solo d'abord ; le multijoueur viendra plus tard, sur ces mêmes règles.

*A turn-based tactical RPG in the style of Dofus and Wakfu, written from scratch. Sprint 46: a Godot 4 C# desktop client (isometric 3D board, Kenney's free animated models, mouse and keyboard play against the AI, a self-test that plays a whole fight through the controls, builds for Windows, macOS and Linux). Sprint 45: the combat rules in a deterministic C# library (isometric grid, action and movement points, A* paths, exact line of sight, spells, initiative, an AI), described by data files, recorded fights that replay roll for roll on Linux, Windows and macOS, and a terminal simulator.*

## Les règles
- **Le plateau** est une grille de cases dessinées en losanges (vue isométrique). On se déplace vers l'une des quatre cases qui partagent un côté ; toutes les distances se comptent en pas, donc une portée dessine un losange autour du lanceur. Trois terrains : le sol, les obstacles (ni passage ni vue) et les trous (pas de passage, mais les sorts passent au-dessus).
- **Un tour** : chaque combattant a des points d'action (PA) et de mouvement (PM), rendus au début de son tour. Un pas coûte 1 PM ; un sort coûte ses PA. On joue dans l'ordre d'initiative.
- **Les sorts** ont une portée minimale et maximale, demandent ou non la ligne de vue, peuvent n'être lancés qu'en ligne droite et un nombre de fois limité par tour. Les dégâts sont tirés entre deux bornes.
- **La ligne de vue** suit exactement le segment entre les centres des deux cases, en nombres entiers : si A voit B, B voit A. Les obstacles et les combattants la coupent, les trous non. Un segment qui passe pile par un coin n'est coupé que si les deux cases de part et d'autre bloquent.
- **La fin** : une équipe qui n'a plus personne a perdu ; au-delà de 50 tours, match nul.

```
...........      a Héros 20/60
.....a.....      B Sanglier 0/30
....#......      C Crapaud 0/22
..~~...#...
...........      # obstacle   ~ trou
```

## Jouer
Avec le SDK .NET 10 et Godot 4.7 (version .NET) :
```sh
dotnet build godot/Rpg.Godot.csproj
godot --path godot -- --lang fr            # options : --scenario duel, --seed 7
```
Ou sans rien installer : les exécutables Windows, macOS et Linux produits par la CI (artefacts du workflow `rpg`, non signés : Windows et macOS avertissent au premier lancement).

- **Souris** : survoler une case montre le chemin (jaune) parmi les cases atteignables (bleu clair) ; cliquer s'y rend. Avec un sort choisi, sa portée est marquée, les cases qu'il peut toucher en bleu, la cible en rouge ; clic droit pour annuler.
- **Clavier** : 1, 2, 3 choisissent un sort, Échap annule, Espace finit le tour. Le bouton FR/EN change de langue.

![Combat](../../src/assets/images/rpg-spell-fr.png)

## Organisation
- **`src/Rpg.Core`** (net10.0, sans dépendance) :
  - `Cell.cs` : cases, voisins, distances.
  - `Board.cs` : le plateau, lu depuis des lignes de caractères.
  - `Pathfinding.cs` : plus court chemin par A* (égalités départagées dans un ordre fixe), cases atteignables par parcours en largeur.
  - `LineOfSight.cs` : les cases traversées par le segment, coins compris, sans arrondi.
  - `Fight.cs`, `Actions.cs` : le combat ; chaque action est vérifiée, une action refusée ne change rien et dit pourquoi ; les événements (déplacement, sort, dégâts, mort, fin) servent à l'affichage.
  - `Ai.cs` : un adversaire déterministe (frapper le plus fort, sinon se placer, sinon s'approcher en contournant les obstacles).
  - `FightRecord.cs` : un combat enregistré (scénario, graine, actions) et son rejeu.
  - `Data.cs` : sorts, cartes et scénarios lus depuis `data/`, refusés s'ils sont incohérents.
- **`src/Rpg.Sim`** : le simulateur en ligne de commande.
- **`src/Rpg.Client`** (sans Godot) : le côté joueur d'un combat, testé sans moteur : sort choisi, aperçu au survol (`Hover`), clic (`Click`), fin de tour, tours de l'IA, textes français et anglais, et `SelfPlay`, qui joue un combat entier en passant par ces commandes.
- **`godot/`** : le client Godot 4.7 en C#. Il ne fait que dessiner : `BoardView` (plateau 3D, surbrillances, case sous la souris par un rayon de la caméra), `FighterView` (modèle animé, anneau d'équipe, PV, dégâts), `Hud` (tour, ordre de jeu, sorts, journal, fin), `Main` (enchaîne les animations, fait jouer l'IA, options `--selftest` et `--screenshot`). Modèles 3D de Kenney (CC0, licences dans `godot/assets/kenney/`).
- **`data/`** : `spells.json`, `maps/*.json`, `scenarios/*.json` (chaque combattant y a un `look`, le modèle 3D qui le montre). Ajouter un sort, une carte ou un monstre se fait ici, sans toucher au code ; les fichiers sont intégrés à `Rpg.Core`, donc le jeu exporté, le serveur et les tests lisent les mêmes.
- **`samples/`** : deux combats de l'IA, rejoués par la CI sur les trois systèmes.

## Lancer
SDK .NET 10 :
```sh
dotnet test --project tests/Rpg.Core.Tests
dotnet run --project src/Rpg.Sim -- --simulate 1000 --scenario duel
dotnet run --project src/Rpg.Sim -- --record combat.json --scenario training --seed 7 --lang fr
dotnet run --project src/Rpg.Sim -- --replay combat.json --show --lang fr
```

## Tests
Sprint 46 : 10 tests de `Rpg.Client` (aperçu, clics, choix de sort, tour de l'IA, 200 combats joués par les commandes identiques à ceux de l'IA seule, textes des deux langues) ; dans la CI, sur les trois systèmes, le client Godot joue un combat entier par ses commandes (`--selftest`) en vérifiant après chaque action que les personnages, leurs PV, les cases montrées et les points affichés suivent le combat ; l'exécutable Linux exporté rejoue ce test.

Sprint 45 : 57 tests xUnit des règles (`tests/Rpg.Core.Tests`) et 10 du simulateur. Parmi eux : les chemins d'A* comparés à un parcours en largeur sur 200 plateaux tirés au hasard ; la ligne de vue identique dans les deux sens sur 3 000 paires et comparée au segment échantillonné ; 600 combats de l'IA qui se terminent sans une action refusée ; 400 combats enregistrés, passés en JSON et rejoués à l'identique ; l'équilibre mesuré sur 2 000 combats, dans une fourchette (leçon du sprint 23 du roguelike). Vérifiés en cassant le code : 13 mutations, 12 attrapées, la dernière équivalente (voir `REVIEW.md`).

## Limites
- Le client se joue à la souris et au clavier, pas encore à la manette ni au toucher. Les captures se font avec le rendu logiciel de Mesa ; sur une vraie carte graphique, ombres et anticrénelage sont plus nets.
- Équilibre mesuré par simulation : à l'entraînement, le héros joué par l'IA gagne 69 % des combats (il les gagnait tous avant le réglage des points de vie des monstres) ; un joueur fera mieux. Dans le duel symétrique, celui qui joue en second gagne deux fois sur trois : celui qui s'approche le premier se met à portée et encaisse le premier coup. Le réglage fin attendra que le combat soit jouable.
- L'IA ne voit qu'un coup d'avance : elle ne fuit pas, ne protège pas ses alliés et ne garde pas ses PM.
