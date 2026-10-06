# fastapi : API HTTP au-dessus de lib-c

[![fastapi](https://github.com/Dzop86/Portfolio/actions/workflows/fastapi.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/fastapi.yml)

API FastAPI qui renvoie les statistiques et les invariants topologiques d'un maillage OBJ, PLY ou STL. Les calculs sont faits par [lib-c](../lib-c/) et par la [bibliothèque C++ de topologie](../topologie/), compilées en bibliothèques partagées et appelées par `ctypes` : le même code que les démos du site.

*FastAPI service returning mesh statistics and topology, computed by lib-c through ctypes; tested with pytest on Linux, Windows and macOS.*

## État (sprint 16)
- `GET /health` : état du service, des deux bibliothèques et du classifieur.
- `POST /v1/mesh/stats` : le fichier en corps brut ; réponse JSON (format, sommets, faces, triangles, arêtes, arêtes de bord, χ, boîte englobante). 422 avec le statut et la ligne de lib-c si le maillage est illisible, 413 au-delà de 32 Mio.
- `POST /v1/mesh/topology` : composantes, boucles de bord, arêtes et sommets non-variété, orientabilité, χ, genre (`null` si non défini), courbure totale (2πχ par Gauss-Bonnet).
- `POST /v1/mesh/classify` : forme du maillage parmi six classes (sphère, tore, boîte, cylindre, cône, gélule) et probabilités, par le PointNet du [projet ML](../ml/) exporté en ONNX ; 512 points tirés sur la surface avec une graine fixe (même maillage, même réponse), prétraitement partagé avec l'entraînement (`shapeml.infer`), ONNX Runtime seul (pas de PyTorch). La réponse cite le modèle (précision de test 95,17 %, empreinte SHA-256). 503 si le classifieur n'est pas installé, 422 pour un maillage sans surface.
- Documentation OpenAPI sur `/docs`.

## Lancer
```sh
cmake -S ../lib-c -B ../lib-c/build-shared -DBUILD_SHARED_LIBS=ON -DMESH_BUILD_TESTS=OFF -DCMAKE_BUILD_TYPE=Release
cmake --build ../lib-c/build-shared --config Release
cmake -S ../topologie -B ../topologie/build-shared -DTOPO_C_API=ON -DTOPO_BUILD_TESTS=OFF -DCMAKE_BUILD_TYPE=Release
cmake --build ../topologie/build-shared --config Release --target topoc
python -m pip install "../ml[serve]" ".[test]"   # shapeml (inférence du projet ML) avec onnxruntime
export MESHLIB_PATH=../lib-c/build-shared/libmesh.so TOPOLIB_PATH=../topologie/build-shared/libtopoc.so  # .dll / .dylib ailleurs
export MODEL_PATH=../ml/export/pointnet.onnx
uvicorn meshapi.app:app
curl --data-binary @../lib-c/tests/data/cube.stl http://127.0.0.1:8000/v1/mesh/stats
```

## Docker
```sh
docker compose up --build api          # depuis la racine du dépôt, API sur http://localhost:8000
docker build -f projects/fastapi/Dockerfile -t meshapi projects && projects/fastapi/scripts/smoke.sh meshapi
```
Image multi-étapes : lib-c est compilée dans la première, la seconde ne contient ni compilateur ni code source ; utilisateur non root (uid 10001), système de fichiers en lecture seule dans `compose.yaml`, contrôle de santé intégré.

## Tests
- **Unitaires** (`tests/test_libmesh.py`, `tests/test_libtopo.py`) : liaisons ctypes, formats, erreurs avec leur ligne, 500 lectures sans fuite, Gauss-Bonnet, et 90 appels concurrents à la topologie (le test échoue ou plante sans le verrou).
- **Intégration** (`tests/test_api.py`) : l'API par le client de test de FastAPI, codes 200, 413 et 422, schéma OpenAPI.
- **Classification** (`tests/test_classify.py`) : des maillages produits par le générateur du projet ML, envoyés en OBJ, sont reconnus (sphère, tore, cône, gélule, les quatre classes jamais confondues sur le jeu de test) ; le cube de lib-c est une boîte, en OBJ comme en STL ; probabilités complètes, réponse déterministe, 413, 422 (illisible ou sans surface), 503 sans modèle. Vérifié en cassant le code : sans la normalisation du nuage, quatre tests échouent.
- **CI** (`.github/workflows/fastapi.yml`) : Linux, Windows et macOS, Python 3.12 et 3.13, avertissements traités en erreurs ; relancée quand lib-c change.
- **Docker** : `scripts/smoke.sh` vérifie l'utilisateur, `/health` (bibliothèques et classifieur), une requête valide et une invalide, la classification du cube, puis l'état « healthy » ; lancé en CI avec `docker compose up`.
- Vérifié à la main avec uvicorn et curl : un STL de 350 000 triangles répond en 0,3 s.

## Limites
- Le service est conteneurisé mais pas déployé en ligne : GitHub Pages ne sert que du statique.
- Pas d'authentification ni de limitation de débit : à ajouter avant toute exposition publique.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
