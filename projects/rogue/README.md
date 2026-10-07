# rogue : un roguelike 2D, en C#

[![rogue](https://github.com/Dzop86/Portfolio/actions/workflows/rogue.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/rogue.yml)

Réécriture d'un jeu de mes études : un roguelike au tour par tour, en C#. Les règles vivent dans une bibliothèque déterministe (sprint 23), jouable dans le terminal ; une partie s'enregistre (graine et actions) et se rejoue à l'identique. L'API de scores (sprint 24) s'en sert : elle tire la graine de chaque partie classée, rejoue la partie envoyée et calcule elle-même le score. Le client Godot 4 viendra au sprint 25.

*A turn-based roguelike in C#: deterministic rules library (seeded dungeons, field of view, fights, five floors), a terminal client in French and English, an autopilot, and recorded runs that replay to the same score on Linux, Windows and macOS. An ASP.NET Core score API (JWT accounts, EF Core, PostgreSQL, OpenAPI, Docker) draws each ranked run's seed and computes the score by replaying the run. Next: a Godot 4 client.*

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
- **`src/Rogue.Api`** (net10.0) : l'API de scores, décrite plus bas.
- **`samples/`** : deux parties du pilote automatique (une sortie du donjon, une mort), rejouées par la CI.

## Lancer
SDK .NET 10 :
```sh
dotnet run --project src/Rogue.Cli                               # une partie, graine au hasard, en français
dotnet run --project src/Rogue.Cli -- --seed 42 --lang en --save run.json
dotnet run --project src/Rogue.Cli -- --replay run.json          # rejoue et vérifie le score
dotnet run --project src/Rogue.Cli -- --bot --seed 0             # regarder le pilote automatique
dotnet run --project src/Rogue.Cli -c Release -- --stats 1000    # statistiques du pilote automatique
dotnet test --project tests/Rogue.Core.Tests                     # tests des règles (de même pour Rogue.Cli.Tests)
ROGUE_TEST_DB="Host=localhost;Username=postgres" dotnet test --project tests/Rogue.Api.Tests   # API, sur un PostgreSQL
```
Touches : flèches, zqsd (AZERTY), wasd (QWERTY) ou hjkl pour bouger ou attaquer, `.` pour attendre, `>` pour descendre, `p` pour boire une potion, `x` ou Échap pour quitter.

## L'API de scores
API minimale ASP.NET Core, documentée par OpenAPI (`/openapi/v1.json`, interface Swagger sur `/swagger`) :

| Requête | Accès | Rôle |
|---|---|---|
| `POST /api/accounts` | libre, limité | crée un compte (nom de 3 à 20 caractères, mot de passe de 10 à 128) |
| `POST /api/tokens` | libre, limité | échange nom et mot de passe contre un jeton JWT (12 heures) |
| `POST /api/runs` | jeton | commence une partie classée : le serveur tire la graine (générateur cryptographique), valable 24 heures |
| `POST /api/runs/{id}/submission` | jeton | envoie la partie (format du jeu) : le serveur la rejoue et enregistre le score qu'il calcule |
| `GET /api/runs` | jeton | mes parties |
| `GET /api/scores?limit=10` | libre | classement : la meilleure partie de chaque joueur |

Une partie classée reçoit un seul verdict : marquée (200) ou refusée par les règles (422, avec l'action fautive et sa position). Un corps mal formé (400), une autre graine ou une partie inachevée (422) laissent la partie ouverte : c'est une erreur du client, pas une triche. Partie d'un autre joueur : 404 ; expirée : 410 ; déjà jugée : 409, y compris pour deux envois simultanés (version de ligne `xmin` de PostgreSQL). Les mots de passe sont hachés par le `PasswordHasher` d'ASP.NET Core Identity (PBKDF2) ; un nom inconnu coûte le même calcul qu'un mauvais mot de passe. Inscription et connexion sont limitées à 10 requêtes par minute et par adresse ; Kestrel refuse les corps de plus de 256 Ko.

Données : EF Core et PostgreSQL, migrations commitées (`src/Rogue.Api/Data/Migrations`), appliquées au démarrage. Les graines (entiers non signés de 64 bits) sont rangées en `numeric(20,0)`.

```sh
docker compose up --build rogue-api           # depuis la racine du dépôt : API sur http://localhost:8001/swagger
scripts/smoke.sh http://localhost:8001        # de bout en bout : compte, graine, partie du pilote automatique, score
```
La clé de signature des jetons se donne par la variable `ROGUE_JWT_KEY` (base64, 32 octets) ; sans elle, l'API en tire une au démarrage et les jetons ne survivent pas à un redémarrage.

## Équilibrage
Sur 1 000 parties (graines 0 à 999), le pilote automatique sort du donjon 276 fois (27,6 %), score moyen 1 316, 1 155 tours en moyenne ; il meurt surtout aux étages 4 (388) et 5 (279). La première version était trop facile : il gagnait les 200 parties d'essai, avec un soin complet à chaque niveau.

## Tests
- **xUnit v3** sur Microsoft.Testing.Platform, 725 tests :
  - générateur aléatoire (valeurs de référence de SplitMix64, bornes, répartition) ;
  - 500 étages générés (100 graines × 5 profondeurs) : salles disjointes, murs tout autour, toutes les cases atteignables depuis le départ, escalier unique, monstres et objets sur des cases libres ; plus de monstres forts en profondeur ;
  - règles sur de petits étages dessinés à la main : murs, escalier, potions, or, combat, expérience, poursuite par le plus court chemin, monstres endormis, mort, régénération, vision ;
  - rejeu de 100 parties complètes au même score, parties de référence (graine → issue, tours et score exacts, vérifiées sur les trois systèmes), 13 formats invalides, action inconnue, impossible ou après la fin, partie trop longue, partie copiée sur une autre graine ;
  - client : options, partie jouée au clavier puis enregistrée et rejouée, messages des deux langues pour chaque événement (trouvés par réflexion), touches, écran ;
  - API (32 tests d'intégration, `WebApplicationFactory`, une base PostgreSQL neuve par classe de tests, horloge simulée) : comptes, noms uniques sans tenir compte de la casse, mots de passe hachés, jetons expirés ou forgés, limitation du débit, partie jouée sur la graine du serveur et marquée au même score que dans le client, envoi unique même simultané, partie d'un autre joueur, expiration, autre graine, partie inachevée, refusée par les règles, mal formée ou trop longue, classement, document OpenAPI.
- **Vérifié en cassant le code** : sans le premier couloir, 453 tests échouent ; sans le contrôle des actions après la fin, ou sans le filtre sur le propriétaire d'une partie dans l'API, le test correspondant échoue.
- **Analyseurs .NET** au niveau recommandé, avertissements traités en erreurs, `dotnet format` vérifié.
- **CI** (`.github/workflows/rogue.yml`) : Linux, Windows et macOS (compilation de tout, tests des règles et du terminal, rejeu des parties de `samples/`, partie jouée au clavier puis rejouée) ; tests de l'API avec un service PostgreSQL et contrôle que les migrations suivent le modèle ; image Docker construite par `compose.yaml` et testée de bout en bout par `scripts/smoke.sh` (dont le refus d'un corps de 300 Ko).

## Limites
- Pas encore d'interface graphique : le client Godot 4 arrive au sprint 25 (Godot 4 n'exporte pas le C# vers le web : client de bureau seulement). L'API n'est pas hébergée en ligne (le portfolio est un site statique) : elle se lance avec Docker.
- Les tests de l'API tournent sous Linux seulement : les machines Windows et macOS de GitHub n'exécutent pas de conteneurs Linux ; l'API y est compilée.
- Dans `compose.yaml`, PostgreSQL accepte les connexions sans mot de passe : la base n'est joignable que par les services du fichier (aucun port publié). Un déploiement réel passerait un mot de passe par un secret.
- Les monstres ne se déplacent pas en diagonale et ne s'enfuient pas.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
