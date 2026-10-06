# Sprint 7 : l'API en conteneur, avec la topologie

**Objectif :** le projet fastapi tourne dans Docker à côté du site (`docker compose up`) et renvoie aussi les invariants topologiques calculés par le C++.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je lance l'API (fastapi) avec `docker compose up` : image multi-étapes, utilisateur non root, test de fumée en CI. | 2 | Fait |
| En tant que chercheur, j'obtiens par HTTP (fastapi) les invariants topologiques et la courbure totale d'un maillage, calculés par la bibliothèque C++ de topologie. | 3 | En cours |

**Décision de Charles (6 octobre) :** le projet ML utilisera GitHub Actions, sans GitLab CI.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
