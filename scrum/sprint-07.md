# Sprint 7 : l'API en conteneur, avec la topologie

**Objectif :** le projet fastapi tourne dans Docker à côté du site (`docker compose up`) et renvoie aussi les invariants topologiques calculés par le C++.

**Goal:** the fastapi project runs in Docker next to the site (`docker compose up`) and also returns the topological invariants computed in C++.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je lance l'API (fastapi) avec `docker compose up` : image multi-étapes, utilisateur non root, test de fumée en CI. | 2 | Fait |
| En tant que chercheur, j'obtiens par HTTP (fastapi) les invariants topologiques et la courbure totale d'un maillage, calculés par la bibliothèque C++ de topologie. | 3 | Fait |

**Tests :** fastapi 24 tests pytest (3 OS × 2 versions de Python), image Docker vérifiée par `scripts/smoke.sh` ; topologie et site inchangés et verts.

**Décision de Charles (6 octobre) :** le projet ML utilisera GitHub Actions, sans GitLab CI.

## Rétro (Charles)
- Ce qui a marché : L'API tourne en conteneur (multi-étapes, non root, test de fumée) et appelle le C++ de topologie.
- Ce que l'IA a mal fait : Rien de notable.
- À changer au prochain sprint : Démarrer le ML sur GitHub Actions, sans GitLab CI (décision de Charles).
