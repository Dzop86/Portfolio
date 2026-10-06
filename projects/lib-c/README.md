# lib-c : bibliothèque C de maillages

[![lib-c](https://github.com/Dzop86/Portfolio/actions/workflows/lib-c.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/lib-c.yml)

Bibliothèque C11 qui lit des maillages au format OBJ, avec un outil en ligne de commande `meshinfo`. Premier maillon du fil rouge du portfolio : les autres projets (topologie, API, ML) partiront de ce lecteur.

*C11 mesh library: OBJ reader, `meshinfo` CLI, Unity tests, CI on Linux, Windows and macOS.*

## État (sprint 2)
- Lecture OBJ : sommets `v`, faces `f` avec références de texture et de normale (`1/2/3`, `1//3`), indices négatifs, polygones triangulés en éventail, fins de ligne Windows.
- Erreurs typées (`MESH_ERR_SYNTAX`, `MESH_ERR_INDEX`...) avec numéro de ligne.
- Valgrind sans fuite ni accès invalide sur les tests et la CLI (`scripts/valgrind.sh`).
- À venir : PLY, WebAssembly (Emscripten) pour une démo dans le navigateur, miroir GitLab CI.

## Compiler et tester
```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Debug
cmake --build build --config Debug
ctest --test-dir build -C Debug --output-on-failure
./build/meshinfo tests/data/cube.obj      # vertices: 8, polygons: 6, triangles: 12
```
Options : `-DMESH_SANITIZE=ON` (ASan et UBSan, GCC ou Clang), `-DMESH_BUILD_TESTS=OFF`.
Prérequis : CMake 3.20+, un compilateur C11 (GCC, Clang ou MSVC 2019+), Git et un accès réseau au premier `cmake` (Unity est téléchargé).

## API
```c
mesh m;
mesh_init(&m);
size_t line;
mesh_status st = mesh_read_obj_file("cube.obj", &m, &line);
if (st != MESH_OK) fprintf(stderr, "line %zu: %s\n", line, mesh_status_string(st));
/* m.vertices[i], m.triangles[j][0..2] (indices à partir de 0) */
mesh_free(&m);
```

## Tests
- **Unitaires** (`tests/test_obj.c`, Unity) : 15 cas, dont les erreurs de syntaxe, les indices hors bornes, les fins de ligne CRLF.
- **Intégration** (CTest) : `meshinfo` sur un cube, sur un fichier absent et sans argument.
- **CI** (`.github/workflows/lib-c.yml`) : Linux (GCC), Windows (MSVC) et macOS (Clang), avertissements traités en erreurs, plus une compilation ASan + UBSan et un passage Valgrind sous Linux.

## Limites
- Les faces ne peuvent référencer que des sommets déjà lus (cas de tous les exportateurs courants).
- `strtod` dépend de la locale : un programme qui appelle `setlocale` avec une locale à virgule décimale lira mal les coordonnées.
- Index sur 32 bits : au plus 4 294 967 295 sommets.

Maillages de test écrits à la main, aucune donnée de laboratoire. Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
