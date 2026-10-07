# Sprint 24 : le roguelike, l'API de scores en ASP.NET Core

**Objectif :** deuxième des trois sprints du roguelike (D32) : une API ASP.NET Core de scores, avec comptes et jetons JWT, EF Core et PostgreSQL, qui impose la graine de chaque partie classée (R6) et calcule elle-même le score en rejouant la partie avec la bibliothèque de règles.

**Goal:** second of the three roguelike sprints (D32): an ASP.NET Core score API, with accounts and JWT tokens, EF Core and PostgreSQL, which draws the seed of each ranked run (R6) and computes the score itself by replaying the run with the rules library.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je crée un compte, je me connecte (JWT), je demande une partie classée (graine tirée par le serveur, valable 24 heures, une seule fois) et j'envoie ma partie : l'API la rejoue avec Rogue.Core et enregistre le score qu'elle a calculé, ou refuse la partie en disant pourquoi ; EF Core et PostgreSQL avec migrations, mots de passe hachés (PBKDF2) ; tests d'intégration sur une vraie base PostgreSQL. | 3 | Fait |
| En tant que visiteur, je consulte le classement (meilleure partie de chaque joueur) et la documentation OpenAPI de l'API (Swagger UI) ; limitation du débit sur les comptes et la connexion ; image Docker multi-étapes et service dans `compose.yaml` avec PostgreSQL ; CI : compilation sur Linux, Windows et macOS, intégration avec PostgreSQL, image construite et testée. | 2 | Fait |

**Tests :** 32 tests d'intégration de l'API sur un vrai PostgreSQL (une base neuve par classe de tests, horloge simulée), 725 tests en tout ; test de fumée de bout en bout sur l'image Docker (le pilote automatique du terminal joue la graine tirée par le serveur, qui trouve le même score). **Trouvé par les tests et la CI :** le classement que EF Core ne savait pas traduire, un JSON invalide qui donnait 500, une limite de taille absente du serveur de test, un conflit de versions d'EF Core, `dotnet ef` qui ne restaure pas les paquets.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
