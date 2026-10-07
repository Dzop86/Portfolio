# Sprint 24 : le roguelike, l'API de scores en ASP.NET Core

**Objectif :** deuxième des trois sprints du roguelike (D32) : une API ASP.NET Core de scores, avec comptes et jetons JWT, EF Core et PostgreSQL, qui impose la graine de chaque partie classée (R6) et calcule elle-même le score en rejouant la partie avec la bibliothèque de règles.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je crée un compte, je me connecte (JWT), je demande une partie classée (graine tirée par le serveur, valable 24 heures, une seule fois) et j'envoie ma partie : l'API la rejoue avec Rogue.Core et enregistre le score qu'elle a calculé, ou refuse la partie en disant pourquoi ; EF Core et PostgreSQL avec migrations, mots de passe hachés (PBKDF2) ; tests d'intégration sur une vraie base PostgreSQL. | 3 | Fait |
| En tant que visiteur, je consulte le classement (meilleure partie de chaque joueur) et la documentation OpenAPI de l'API (Swagger UI) ; limitation du débit sur les comptes et la connexion ; image Docker multi-étapes et service dans `compose.yaml` avec PostgreSQL ; CI : compilation sur Linux, Windows et macOS, intégration avec PostgreSQL, image construite et testée. | 2 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
