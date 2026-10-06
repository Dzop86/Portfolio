# fastapi : API HTTP au-dessus de lib-c

[![fastapi](https://github.com/Dzop86/Portfolio/actions/workflows/fastapi.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/fastapi.yml)

API FastAPI qui renvoie les statistiques et la topologie d'un maillage OBJ, PLY ou STL. Les calculs sont faits par [lib-c](../lib-c/), compilée en bibliothèque partagée et appelée par `ctypes` : un seul lecteur, celui qui est fuzzé.

*FastAPI service returning mesh statistics and topology, computed by lib-c through ctypes; tested with pytest on Linux, Windows and macOS.*

## État (sprint 6)
- `GET /health` : état du service et de la bibliothèque.
- `POST /v1/mesh/stats` : le fichier en corps brut ; réponse JSON (format, sommets, faces, triangles, arêtes, arêtes de bord, χ, boîte englobante). 422 avec le statut et la ligne de lib-c si le maillage est illisible, 413 au-delà de 32 Mio.
- Documentation OpenAPI sur `/docs`.

## Lancer
```sh
cmake -S ../lib-c -B ../lib-c/build-shared -DBUILD_SHARED_LIBS=ON -DMESH_BUILD_TESTS=OFF -DCMAKE_BUILD_TYPE=Release
cmake --build ../lib-c/build-shared --config Release
python -m pip install ".[test]"
MESHLIB_PATH=../lib-c/build-shared/libmesh.so uvicorn meshapi.app:app   # mesh.dll sous Windows, libmesh.dylib sous macOS
curl --data-binary @../lib-c/tests/data/cube.stl http://127.0.0.1:8000/v1/mesh/stats
```

## Tests
- **Unitaires** (`tests/test_libmesh.py`) : liaison ctypes, formats, erreurs avec leur ligne, 500 lectures sans fuite ni plantage.
- **Intégration** (`tests/test_api.py`) : l'API par le client de test de FastAPI, codes 200, 413 et 422, schéma OpenAPI.
- **CI** (`.github/workflows/fastapi.yml`) : Linux, Windows et macOS, Python 3.12 et 3.13, avertissements traités en erreurs ; relancée quand lib-c change.
- Vérifié à la main avec uvicorn et curl : un STL de 350 000 triangles répond en 0,3 s.

## Limites
- Le service n'est pas encore conteneurisé ni déployé (sprint suivant : Docker et `compose.yaml`).
- Pas d'authentification ni de limitation de débit : à ajouter avant toute exposition publique.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
