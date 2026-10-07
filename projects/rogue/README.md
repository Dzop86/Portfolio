# rogue : un roguelike 2D, en C#

[![rogue](https://github.com/Dzop86/Portfolio/actions/workflows/rogue.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/rogue.yml)

Réécriture d'un jeu de mes études : un roguelike au tour par tour, en C#. Ce premier volet (sprint 23) pose les règles, dans une bibliothèque déterministe que partageront le client Godot 4 et l'API de scores ; le jeu se joue déjà dans le terminal. Une partie s'enregistre (graine et actions) et se rejoue à l'identique : c'est ainsi que l'API vérifiera chaque score.

*A turn-based roguelike in C#: deterministic rules library (seeded dungeons, field of view, fights, five floors), a terminal client in French and English, an autopilot, and recorded runs that replay to the same score on Linux, Windows and macOS. Next: an ASP.NET Core score API that replays each run, then a Godot 4 client.*

## Le jeu
Cinq étages à traverser, du premier escalier à la sortie. On se déplace dans les quatre directions ; marcher sur un monstre l'attaque. Rats et gobelins aux premiers étages, orques et trolls plus bas. Potions (12 PV), or, expérience et niveaux. Score : l'or, la valeur des monstres tués, 100 points par étage atteint après le premier, 500 points pour la sortie.

```
Étage 1/5  PV 30/30  Niv 1 (0/30)  Att 4  Déf 1  Potions 0  Or 0  Score 0   Graine 42
    .
   #.
   #.
 ###.###
 #.....#
 #.....#
 #.....#
 #....@#
 #######
```

## Organisation
- **`src/Rogue.Core`** (net8.0, sans dépendance ; la version que cible Godot 4) :
  - `Rng.cs` : SplitMix64, identique sur toutes les plateformes et toutes les versions de .NET (ce que `System.Random` ne garantit pas), tirages sans biais de modulo.
  - `Level.cs` : génération d'un étage à partir de la graine : jusqu'à neuf salles sans chevauchement, triées de gauche à droite et reliées dans cet ordre par des couloirs en L, donc toutes atteignables ; monstres selon la profondeur, potions, or.
  - `FieldOfView.cs` : vision dans un rayon de 6 cases, lignes de Bresenham arrêtées par les murs ; les cases vues restent en mémoire.
  - `Game.cs` : un tour = l'action du joueur puis celle des monstres réveillés, qui le poursuivent par un plus court chemin (égalités départagées nord, sud, est, ouest). Coups à 85 %, dégâts = attaque − défense ± 1, au moins 1. Une action impossible (mur, pas d'escalier, pas de potion, partie finie) est refusée sans coûter de tour.
  - `Replay.cs` : format de partie versionné, `{"format":"rogue-run","version":1,"seed":"42","actions":"ees>"}` (une lettre par action, graine en chaîne car un entier de 64 bits ne tient pas dans un nombre JavaScript), et rejeu qui recalcule l'issue et le score en refusant toute action non permise à ce moment-là, avec sa position.
  - `Autopilot.cs` : un joueur automatique simple (boit quand il est mal en point, combat, se repose, ramasse, explore, descend), qui ne se sert que de ce que le joueur peut savoir.
- **`src/Rogue.Cli`** (net10.0) : le jeu dans le terminal, en français ou en anglais ; les cases hors de vue sont en gris foncé.
- **`samples/`** : deux parties du pilote automatique (une sortie du donjon, une mort), rejouées par la CI.

## Lancer
SDK .NET 10 :
```sh
dotnet run --project src/Rogue.Cli                               # une partie, graine au hasard, en français
dotnet run --project src/Rogue.Cli -- --seed 42 --lang en --save run.json
dotnet run --project src/Rogue.Cli -- --replay run.json          # rejoue et vérifie le score
dotnet run --project src/Rogue.Cli -- --bot --seed 0             # regarder le pilote automatique
dotnet run --project src/Rogue.Cli -c Release -- --stats 1000    # statistiques du pilote automatique
dotnet test                                                      # tests
```
Touches : flèches, zqsd (AZERTY), wasd (QWERTY) ou hjkl pour bouger ou attaquer, `.` pour attendre, `>` pour descendre, `p` pour boire une potion, `x` ou Échap pour quitter.

## Équilibrage
Sur 1 000 parties (graines 0 à 999), le pilote automatique sort du donjon 276 fois (27,6 %), score moyen 1 316, 1 155 tours en moyenne ; il meurt surtout aux étages 4 (388) et 5 (279). La première version était trop facile : il gagnait les 200 parties d'essai, avec un soin complet à chaque niveau.

## Tests
- **xUnit v3** sur Microsoft.Testing.Platform, 693 tests :
  - générateur aléatoire (valeurs de référence de SplitMix64, bornes, répartition) ;
  - 500 étages générés (100 graines × 5 profondeurs) : salles disjointes, murs tout autour, toutes les cases atteignables depuis le départ, escalier unique, monstres et objets sur des cases libres ; plus de monstres forts en profondeur ;
  - règles sur de petits étages dessinés à la main : murs, escalier, potions, or, combat, expérience, poursuite par le plus court chemin, monstres endormis, mort, régénération, vision ;
  - rejeu de 100 parties complètes au même score, parties de référence (graine → issue, tours et score exacts, vérifiées sur les trois systèmes), 13 formats invalides, action inconnue, impossible ou après la fin, partie trop longue, partie copiée sur une autre graine ;
  - client : options, partie jouée au clavier puis enregistrée et rejouée, messages des deux langues pour chaque événement (trouvés par réflexion), touches, écran.
- **Vérifié en cassant le code** : sans le premier couloir, 453 tests échouent ; sans le contrôle des actions après la fin, le test correspondant échoue.
- **Analyseurs .NET** au niveau recommandé, avertissements traités en erreurs, `dotnet format` vérifié.
- **CI** (`.github/workflows/rogue.yml`) : Linux, Windows et macOS ; tests, rejeu des parties de `samples/`, partie jouée au clavier puis rejouée.

## Limites
- Pas encore d'interface graphique ni de scores en ligne : l'API ASP.NET Core arrive au sprint 24, le client Godot 4 au sprint 25 (Godot 4 n'exporte pas le C# vers le web : client de bureau seulement).
- Les monstres ne se déplacent pas en diagonale et ne s'enfuient pas.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
