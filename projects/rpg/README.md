# rpg : Osmose, un RPG tactique à la manière de Dofus

[![rpg](https://github.com/Dzop86/Portfolio/actions/workflows/rpg.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/rpg.yml)

Un RPG tactique au tour par tour, dans l'esprit de Dofus et de Wakfu, écrit de zéro : aucun code, aucun nom, aucune image d'Ankama (D54). Six sprints : les règles du combat (sprint 45, ce dossier), le combat isométrique dans Godot 4 (46), le serveur de comptes et de personnages (47), la création de personnage (48), la ville d'accueil et ses PNJ (49), le launcher en Rust et les exécutables (50). Solo d'abord ; le multijoueur viendra plus tard, sur ces mêmes règles.

*A turn-based tactical RPG in the style of Dofus and Wakfu, written from scratch. Sprint 50: a Rust launcher (Tauri) that signs in, updates the game file by file from a versioned SHA-256 manifest, resumes cut downloads with HTTP ranges and starts the game with the token; unsigned packages for Windows, macOS and Linux built by the CI; a video and an architecture diagram on the project page. Sprint 49: a welcome town built from Kenney's Fantasy Town Kit, walked with a click along the shortest path, three inhabitants with bilingual dialogues and choices described in data, the character's place saved on the server, a gate to a training fight. Sprint 48: character creation with three classes described in data (characteristics and spells, balanced by simulation), twelve looks and seven outfit colours (a palette shader), checked by the server. Sprint 47: an ASP.NET Core server for accounts and characters (JWT, PBKDF2 password hashes, rate limit, EF Core and PostgreSQL, OpenAPI, Docker), and the game's login and character screen, tested end to end against it, with an offline mode. Sprint 46: a Godot 4 C# desktop client (isometric 3D board, Kenney's free animated models, mouse and keyboard play against the AI, a self-test that plays a whole fight through the controls, builds for Windows, macOS and Linux). Sprint 45: the combat rules in a deterministic C# library (isometric grid, action and movement points, A* paths, exact line of sight, spells, initiative, an AI), described by data files, recorded fights that replay roll for roll on Linux, Windows and macOS, and a terminal simulator.*

## Les règles
- **Le plateau** est une grille de cases dessinées en losanges (vue isométrique). On se déplace vers l'une des quatre cases qui partagent un côté ; toutes les distances se comptent en pas, donc une portée dessine un losange autour du lanceur. Trois terrains : le sol, les obstacles (ni passage ni vue) et les trous (pas de passage, mais les sorts passent au-dessus).
- **Un tour** : chaque combattant a des points d'action (PA) et de mouvement (PM), rendus au début de son tour. Un pas coûte 1 PM ; un sort coûte ses PA. On joue dans l'ordre d'initiative.
- **Les classes** (`data/classes.json`) : le héros du joueur prend les points de vie, PA, PM, l'initiative et les sorts de la sienne. Sentinelle (60 PV, arc et lance), Garde (80 PV, 7 PA, hache), Mage (55 PV, 8 PA, étincelles qui passent par-dessus les obstacles, boule de feu).
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
docker compose up --build rpg-api          # depuis la racine du dépôt : le serveur, sur le port 8002
godot --path godot -- --lang fr            # options : --server URL, --offline, --scenario duel, --seed 7
```
Le jeu s'ouvre sur l'écran de connexion : créer un compte ou se connecter, puis créer un personnage (un nom, l'une des 12 apparences, une classe et l'une des 7 couleurs de tenue ; le modèle, repeint, tourne à côté du formulaire) et le choisir. On arrive au village de Clairval : un clic fait marcher par le plus court chemin (montré en jaune), un clic sur un habitant y mène et ouvre la conversation (réponses au clic ou aux touches 1 à 4), la porte est et Garance mènent au combat d'entraînement ; à la fin du combat, « Retour en ville », et « Personnages » ramène à la liste. Sans serveur, « Jouer hors ligne » mène au village avec l'héroïne du scénario. Ou sans rien installer : les exécutables Windows, macOS et Linux produits par la CI (artefacts du workflow `rpg`, non signés : Windows et macOS avertissent au premier lancement).

**Le launcher** (`launcher/`, Rust et Tauri) : paquets `rpg-launcher-*` des artefacts de la CI (`.deb` et AppImage, installeur Windows, `.dmg`), non signés eux aussi. Il se connecte au serveur, compare le jeu installé au manifeste publié (taille et SHA-256 de chaque fichier), ne télécharge que les fichiers modifiés, reprend un téléchargement coupé, vérifie chaque fichier avant de remplacer l'ancien, puis lance le jeu avec le jeton (variables `RPG_SERVER` et `RPG_TOKEN` : le jeu s'ouvre directement sur les personnages). Pour publier une version en local :
```sh
godot --headless --path godot --export-release Linux               # dans godot/build/linux
cargo run --manifest-path launcher/Cargo.toml -p rpg-launcher-core --bin rpg-manifest -- godot/build/linux 1.0.0
docker compose up --build rpg-api                                  # sert godot/build sous /updates
cargo run --manifest-path launcher/Cargo.toml -p rpg-launcher-core --bin rpg-update -- http://localhost:8002 ./jeu linux
cd launcher/app && npx @tauri-apps/cli@2.5.0 dev                   # la fenêtre du launcher
```

![Launcher](../../src/assets/images/rpg-launcher-fr.png)

- **Souris** : survoler une case montre le chemin (jaune) parmi les cases atteignables (bleu clair) ; cliquer s'y rend. Avec un sort choisi, sa portée est marquée, les cases qu'il peut toucher en bleu, la cible en rouge ; clic droit pour annuler.
- **Clavier** : 1, 2, 3 choisissent un sort, Échap annule, Espace finit le tour. Le bouton FR/EN change de langue.

![Connexion](../../src/assets/images/rpg-lobby-fr.png)
![Village](../../src/assets/images/rpg-town-fr.png)
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
- **`src/Rpg.Api`** (ASP.NET Core, API minimale) : le serveur des comptes et des personnages.
  - `POST /api/accounts` (créer un compte), `POST /api/tokens` (se connecter : un jeton JWT valable 12 heures), `GET`, `POST /api/characters`, `DELETE /api/characters/{id}` et `PUT /api/characters/{id}/place` (où il se tient en ville : refusé si l'on ne peut pas y marcher depuis l'arrivée) (avec le jeton) ; description OpenAPI sur `/openapi/v1.json`, Swagger UI sur `/swagger`.
  - Mots de passe hachés par PBKDF2 (le `PasswordHasher` d'ASP.NET Core Identity) ; un nom inconnu coûte autant qu'un mauvais mot de passe, et la réponse est la même. Inscription et connexion limitées à 10 essais par minute et par adresse.
  - Noms de compte uniques, noms de personnage uniques sur tout le serveur, sans tenir compte de la casse (index unique sur le nom en majuscules : deux créations simultanées ne passent pas toutes les deux) ; cinq personnages au plus par compte ; le personnage d'un autre se comporte comme un personnage absent (404).
  - EF Core 10 et PostgreSQL (Npgsql), migrations par `dotnet ef` (outil épinglé dans `.config/dotnet-tools.json`), appliquées au démarrage. Image Docker « chiseled » (sans shell, utilisateur non root), service `rpg-api` de `compose.yaml` avec sa base `rpg-db`, qui n'est pas exposée.
- **`src/Rpg.Client`** : en plus du combat, `GameServer` (les appels HTTP du jeu, partagés avec les tests) et `Lobby` (ce que l'écran de connexion peut faire : chaque refus devient un message des deux langues, les vérifications du serveur sont faites d'abord sur place, un serveur injoignable propose le jeu hors ligne).
- **`src/Rpg.Client`** (sans Godot) : le côté joueur d'un combat, testé sans moteur : sort choisi, aperçu au survol (`Hover`), clic (`Click`), fin de tour, tours de l'IA, textes français et anglais, et `SelfPlay`, qui joue un combat entier en passant par ces commandes.
- **`godot/`** : le client Godot 4.7 en C#. Il ne fait que dessiner : `BoardView` (plateau 3D, surbrillances, case sous la souris par un rayon de la caméra), `FighterView` (modèle animé, anneau d'équipe, PV, dégâts), `Hud` (tour, ordre de jeu, sorts, journal, fin), `LobbyView` (l'écran de connexion et des personnages), `Main` (l'écran de connexion puis le combat, enchaîne les animations, fait jouer l'IA, options `--selftest`, `--lobby-selftest` et `--screenshot`). Modèles 3D de Kenney (CC0, licences dans `godot/assets/kenney/`).
- **`godot/shaders/outfit.gdshader`** : la couleur de tenue. La texture des personnages de Kenney est une palette de cases ; le shader décale les sept colonnes colorées (vert à violet) de 0 à 6 crans, si bien que vêtements et accessoires changent ensemble sans toucher à la peau, aux cheveux ni aux gris. `Looks.MainColumn` lit dans le maillage la couleur dominante de chaque apparence, pour que chaque pastille montre la teinte réellement obtenue.
- **La ville** : `data/towns/*.json` décrit la carte en lignes de caractères (`.` herbe, `=` route, `H` maison de 2 × 2 cases, `F` fontaine, `T` arbre, `R` rocher, `S` étal, `L` lanterne, `C` charrette, `~` eau), l'arrivée, les habitants (apparence, couleur, case, dialogue) et les portes (vers quel combat) ; `data/dialogues/*.json`, les répliques et les réponses (vers une autre réplique, la fin, ou un combat). Au chargement, chaque habitant et chaque porte doivent être accessibles à pied depuis l'arrivée, chaque réplique atteignable, et chaque conversation doit pouvoir finir. `Rpg.Client` : `TownController` (ce qu'un clic fait en ville) et `TownTour` (tout le village par ces clics, pour l'auto-test) ; `godot/` : `TownView` (le village 3D, maisons montées à partir des murs et des toits du Fantasy Town Kit de Kenney, CC0), `TalkPanel` (la conversation), `Main.Town.cs`.
- **`launcher/`** (Rust) : `core`, une bibliothèque testée sans fenêtre (`manifest` : manifeste, empreintes, plan de mise à jour, refus d'un chemin qui sortirait du dossier du jeu ; `update` : téléchargement avec reprise par en-tête `Range`, vérification, mise à jour complète, manifeste local écrit en dernier ; `server` : connexion ; `launch` : commande du jeu) et ses deux outils, `rpg-manifest` et `rpg-update` ; `app`, la fenêtre Tauri (le jeton reste du côté Rust, la page ne le voit jamais) ; `ui`, la page (HTML, CSS, JavaScript, français et anglais).
- **`data/`** : `spells.json`, `classes.json`, `towns/*.json`, `dialogues/*.json`, `maps/*.json`, `scenarios/*.json` (chaque combattant y a un `look`, le modèle 3D qui le montre). Ajouter un sort, une carte ou un monstre se fait ici, sans toucher au code ; les fichiers sont intégrés à `Rpg.Core`, donc le jeu exporté, le serveur et les tests lisent les mêmes.
- **`samples/`** : deux combats de l'IA, rejoués par la CI sur les trois systèmes.

## Lancer
SDK .NET 10 :
```sh
dotnet test --project tests/Rpg.Core.Tests
dotnet run --project src/Rpg.Sim -- --simulate 1000 --scenario duel
dotnet run --project src/Rpg.Sim -- --record combat.json --scenario training --seed 7 --lang fr
dotnet run --project src/Rpg.Sim -- --replay combat.json --show --lang fr
```
Le serveur et ses tests demandent PostgreSQL :
```sh
docker run -d --name rpg-pg -p 5432:5432 -e POSTGRES_HOST_AUTH_METHOD=trust postgres:18-alpine
RPG_TEST_DB="Host=localhost;Username=postgres" dotnet test --project tests/Rpg.Api.Tests
ConnectionStrings__Game="Host=localhost;Username=postgres;Database=rpg" dotnet run --project src/Rpg.Api
scripts/smoke.sh http://localhost:8002     # contre docker compose : de l'inscription au refus d'une requête trop grosse
```
La clé qui signe les jetons vient de `Jwt__Key` (base64, 32 octets, variable d'environnement ; `RPG_JWT_KEY` pour docker compose). Sans clé, le serveur en tire une au démarrage, et les jetons ne survivent pas à un redémarrage.

## Tests
Sprint 50 : 13 tests Rust (`cargo test`) : premier téléchargement complet puis plus rien, seuls les fichiers modifiés ensuite et les anciens retirés, fichier abîmé sur le disque retéléchargé, téléchargement coupé repris là où il s'était arrêté (avec un serveur qui ignore la reprise aussi), fichier qui ne correspond pas à son empreinte jamais installé, manifeste qui tenterait d'écrire hors du dossier refusé, exécutable rendu exécutable, progression, et contre un vrai serveur HTTP : reprise par `Range` (206), connexion et ses refus, jeton passé par l'environnement ; clippy au niveau `all`, avertissements bloquants. 3 tests du serveur (fichiers servis avec reprise, rien hors du dossier, jeton du launcher). Dans la CI : le launcher compilé, testé et empaqueté sur les trois systèmes ; puis, de bout en bout, l'export Linux du jeu publié par docker compose, installé par `rpg-update`, mis à jour sans rien télécharger, réparé après un fichier abîmé, et qui joue un combat entier depuis le dossier installé. Playwright : la fiche (vidéo, schéma, capture du launcher) sur les cinq navigateurs, avec axe.

Sprint 49 : 4 tests de plus dans `Rpg.Core` (le village cohérent, la carte lue depuis ses lignes, villes et dialogues incohérents refusés : maison incomplète, habitant emmuré, porte vers un combat inconnu, réplique inatteignable, boucle sans fin), 7 dans `Rpg.Client` (chemin le plus court autour des maisons et de l'eau, clic sur un habitant jusqu'à son côté le plus proche, conversation, porte et Garance vers le combat, retour à la position gardée, tour complet du village), 2 du serveur (position gardée si l'on peut y marcher, réponse sans champ calculé). Dans la CI, sur les trois systèmes, `--town-selftest` parle à chaque habitant, vérifie que chaque réplique et ses réponses sont celles à l'écran, passe la porte et joue le combat ; contre le serveur, `--lobby-selftest` fait tout le chemin (inscription, personnage, village, position enregistrée et relue sur le serveur, combat). 3 mutations, 3 attrapées (la troisième après un test ajouté).

Sprint 48 : 7 tests de plus dans `Rpg.Core` (une classe donne ses caractéristiques et ses sorts, classes incohérentes refusées, classe ou couleur inconnue refusée en combat et au rejeu, un combat enregistré garde classe et couleur, l'équilibre de chaque classe sur 1 000 combats), 1 du simulateur (`--class`), 6 du serveur (classe et couleur refusées avec le champ fautif, gardées, un personnage du sprint 47 devenu Sentinelle par la migration), 4 du client. L'auto-test de l'écran crée une Mage de couleur 3 et vérifie en combat sa barre de sorts et la couleur de son modèle. 3 mutations, 3 attrapées.

Sprint 47 : 22 tests d'intégration (`tests/Rpg.Api.Tests`, `WebApplicationFactory` sur une vraie base PostgreSQL créée pour chaque test puis supprimée), écrits avec le client du jeu (`GameServer`, `Lobby`) : inscription, connexion, jeton expiré (horloge avancée de 13 heures), limite d'essais, noms pris quelle que soit la casse, cinq personnages au plus, personnages d'un autre joueur invisibles et intouchables, personnage du serveur qui combat et dont le combat se rejoue. 11 tests de plus dans `Rpg.Client` (l'écran sans serveur, serveur injoignable, session expirée, textes des deux langues pour chaque refus). Dans la CI : migrations à jour du modèle, image Docker construite et servie par `compose.yaml`, `scripts/smoke.sh` contre elle, puis le client Godot contre ce même serveur (`--lobby-selftest`) : inscription, création et choix d'un personnage par les champs et les boutons de l'écran, puis un combat entier avec ce héros. Vérifiés en cassant le code : 4 mutations du serveur, 4 attrapées.

Sprint 46 : 10 tests de `Rpg.Client` (aperçu, clics, choix de sort, tour de l'IA, 200 combats joués par les commandes identiques à ceux de l'IA seule, textes des deux langues) ; dans la CI, sur les trois systèmes, le client Godot joue un combat entier par ses commandes (`--selftest`) en vérifiant après chaque action que les personnages, leurs PV, les cases montrées et les points affichés suivent le combat ; l'exécutable Linux exporté rejoue ce test.

Sprint 45 : 57 tests xUnit des règles (`tests/Rpg.Core.Tests`) et 10 du simulateur. Parmi eux : les chemins d'A* comparés à un parcours en largeur sur 200 plateaux tirés au hasard ; la ligne de vue identique dans les deux sens sur 3 000 paires et comparée au segment échantillonné ; 600 combats de l'IA qui se terminent sans une action refusée ; 400 combats enregistrés, passés en JSON et rejoués à l'identique ; l'équilibre mesuré sur 2 000 combats, dans une fourchette (leçon du sprint 23 du roguelike). Vérifiés en cassant le code : 13 mutations, 12 attrapées, la dernière équivalente (voir `REVIEW.md`).

## Limites
- Launcher et jeu non signés : Windows (SmartScreen) et macOS (Gatekeeper) avertissent au premier lancement ; le launcher ne se met pas à jour lui-même. Les mises à jour sont publiées sans signature : le SHA-256 protège d'un fichier abîmé, pas d'un serveur compromis (il faudrait signer le manifeste). La fenêtre Tauri n'est essayée que sous Linux en local ; sous Windows et macOS, la CI la compile et l'empaquette sans l'ouvrir.
- Un seul village, sans quêtes ni objets : les réponses ne changent rien d'autre que la suite de la conversation ou le combat. La position n'est enregistrée qu'à la fin d'une marche ; hors ligne, elle ne dure que le temps de la partie.
- Le serveur se teste sous Linux seulement : les machines Windows et macOS de GitHub ne lancent pas de conteneurs Linux (il y est compilé). Il parle HTTP : en ligne, il faudra un proxy HTTPS devant. Pas encore de suppression de compte, de changement de mot de passe ni de jeton de rafraîchissement : après 12 heures, on se reconnecte. La limite de cinq personnages se vérifie avant l'insertion : deux créations simultanées sur un même compte pourraient en donner six (l'unicité des noms, elle, est garantie par la base).
- Le client se joue à la souris et au clavier, pas encore à la manette ni au toucher. Les captures se font avec le rendu logiciel de Mesa ; sur une vraie carte graphique, ombres et anticrénelage sont plus nets.
- Les classes ne changent que des nombres et des sorts de dégâts : ni soin, ni bouclier, ni effet qui dure (il faudra étendre les règles). Réglées par simulation, l'IA jouant le héros, sur 5 000 combats d'entraînement : Sentinelle 69 %, Garde 73 %, Mage 74 % ; sans la Hache à 9-12 et les 7 PA, la Garde perdait tout, et la Mage à 50 PV et 7 PA gagnait 0,4 % des combats.
- La couleur de tenue tourne la palette : sur un modèle à plusieurs couleurs, chacune change, et deux modèles ne prennent pas la même teinte au même cran (les pastilles montrent celle du modèle choisi).
- Équilibre mesuré par simulation : à l'entraînement, le héros joué par l'IA gagne 69 % des combats (il les gagnait tous avant le réglage des points de vie des monstres) ; un joueur fera mieux. Dans le duel symétrique, celui qui joue en second gagne deux fois sur trois : celui qui s'approche le premier se met à portée et encaisse le premier coup. Le réglage fin attendra que le combat soit jouable.
- L'IA ne voit qu'un coup d'avance : elle ne fuit pas, ne protège pas ses alliés et ne garde pas ses PM.
