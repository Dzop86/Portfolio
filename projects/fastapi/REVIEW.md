# Relecture humaine, fastapi (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/meshapi/libmesh.py` : structures ctypes conformes à `mesh/mesh.h`, libération du maillage sur tous les chemins.
- [ ] `src/meshapi/app.py` : limite de taille avant lecture complète du corps.

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/meshapi/app.py` | (Claude) Point d'accès `async` appelant lib-c directement : un gros maillage aurait bloqué la boucle d'événements, et le commentaire affirmait le contraire | `run_in_threadpool` |
| 2026-10-06 | `pyproject.toml` | (Claude) Détecté avec `-W error` : Starlette 1.7 déprécie `httpx` pour son client de test | Dépendance de test `httpx2` |
| | | | |
