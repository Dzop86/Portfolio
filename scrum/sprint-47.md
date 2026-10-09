# Sprint 47 : le RPG tactique, comptes et personnages

**Objectif :** le serveur ASP.NET Core des comptes et des personnages, et l'écran de connexion du jeu.

**Goal:** the ASP.NET Core server for accounts and characters, and the game's login screen.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je crée un compte et je me connecte (rpg) : inscription et connexion (JWT, mots de passe hachés PBKDF2), limite de tentatives, OpenAPI ; EF Core, PostgreSQL et migrations ; service docker compose ; tests d'intégration sur une vraie base. | 3 | Fait |
| En tant que joueur, je retrouve mes personnages (rpg) : liste, création et suppression côté serveur (nombre limité par compte, nom unique validé par le serveur), écran de connexion et de choix du personnage dans le jeu ; tests de bout en bout du client contre le serveur. | 2 | Fait |

**Résultat :** le serveur des comptes et des personnages tourne dans Docker (`docker compose up --build rpg-api`, port 8002, Swagger sur `/swagger`). Le jeu s'ouvre sur un écran de connexion : créer un compte, créer jusqu'à cinq personnages (nom et apparence, le modèle 3D tourne à côté), en choisir un pour combattre ; « Jouer hors ligne » sans serveur. Captures de l'écran sur la fiche (T10 à T12).

**Tests :** 22 tests d'intégration du serveur sur une vraie base PostgreSQL, écrits avec le client du jeu ; 11 tests de plus dans `Rpg.Client` (l'écran sans serveur, pannes, textes) ; dans la CI, `scripts/smoke.sh` contre l'image Docker, puis le client Godot contre ce serveur (`--lobby-selftest`) : inscription, personnage créé et choisi par les boutons de l'écran, combat entier avec ce héros. 4 mutations du serveur, 4 attrapées. **Trouvé en route :** la CI du sprint 46 était rouge sous Windows (PowerShell n'attendait pas l'import de Godot), 32 appels de test sans jeton d'annulation, un `SIGPIPE` dans le script d'essai, et sur la première capture un modèle trop gros, une tuile décalée et des apparences affichées par leur identifiant brut.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
