# lib-c : bibliothèque C de maillages

[![lib-c](https://github.com/Dzop86/Portfolio/actions/workflows/lib-c.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/lib-c.yml)

Bibliothèque C11 qui lit des maillages aux formats OBJ, PLY et STL, avec un outil en ligne de commande `meshinfo`. Premier maillon du fil rouge du portfolio : les autres projets (topologie, API, ML) partiront de ce lecteur.

*C11 mesh library: OBJ, PLY and STL reader, `meshinfo` CLI, Unity tests, CI on Linux, Windows and macOS.*

## État (sprint 6)
- Lecture OBJ : sommets `v`, faces `f` avec références de texture et de normale (`1/2/3`, `1//3`), indices négatifs, polygones triangulés en éventail, fins de ligne Windows.
- Lecture PLY : ASCII, binaire little-endian et big-endian, propriétés et éléments supplémentaires ignorés, format détecté par `mesh_read_file` et `mesh_read_buffer`.
- Lecture STL : binaire (reconnu à sa taille exacte) et ASCII ; les coins identiques sont soudés en sommets partagés, les facettes devenues dégénérées écartées. Testé sur des modèles d'impression 3D de 400 000 triangles (lus en local, non publiés).
- Topologie : arêtes, arêtes de bord, caractéristique d'Euler χ = V − E + F et boîte englobante (`mesh_compute_topology`). Les arêtes sont comptées sur des clés triées par base (sprint 42, D51) : 3 clés compactées par triangle, chiffres de 11 bits, seulement les passes utiles. Mesuré sur un tore de 262 088 triangles : 35 ms avec `qsort`, 18 à 20 ms en natif ; lecture complète de l'OBJ en WebAssembly, 919 puis 562 ms. Sur 3,1 millions de clés, le tri seul passe de 130-140 ms à 60-65 ms.
- WebAssembly : `scripts/build-wasm.sh` (Emscripten 6.0.11 dans Docker) produit la démo de la [fiche du projet](https://dzop86.github.io/Portfolio/fr/project-lib-c.html).
- Erreurs typées (`MESH_ERR_SYNTAX`, `MESH_ERR_INDEX`...) avec numéro de ligne.
- Valgrind sans fuite ni accès invalide sur les tests et la CLI (`scripts/valgrind.sh`).
- Lecteurs fuzzés avec libFuzzer (ASan + UBSan) : 30 millions d'entrées sans erreur en local, 60 s à chaque push en CI.
- À venir : écriture OBJ et PLY.

## Compiler et tester
```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Debug
cmake --build build --config Debug
ctest --test-dir build -C Debug --output-on-failure
./build/meshinfo tests/data/cube.obj         # vertices: 8, polygons: 6, triangles: 12
./build/meshinfo tests/data/tetrahedron.ply  # vertices: 4, polygons: 4, triangles: 4, ...
./build/meshinfo tests/data/torus.obj        # ... edges: 144, boundary edges: 0, euler characteristic: 0
```
Options : `-DMESH_SANITIZE=ON` (ASan et UBSan, GCC ou Clang), `-DMESH_FUZZ=ON` (cible libFuzzer `fuzz_read`, Clang), `-DMESH_BUILD_TESTS=OFF`.
Prérequis : CMake 3.20+, un compilateur C11 (GCC, Clang ou MSVC 2019+), Git et un accès réseau au premier `cmake` (Unity est téléchargé).

## API
```c
mesh m;
mesh_init(&m);
size_t line;
mesh_status st = mesh_read_file("cube.obj", &m, &line); /* OBJ ou PLY */
if (st != MESH_OK) fprintf(stderr, "line %zu: %s\n", line, mesh_status_string(st));
/* m.vertices[i], m.triangles[j][0..2] (indices à partir de 0) */
mesh_free(&m);
```

## Tests
- **Unitaires** (Unity) : `tests/test_obj.c` (15 cas), `tests/test_ply.c` (19 cas), `tests/test_stl.c` (11 cas) et `tests/test_topology.c` (8 cas, dont des tores de 20 tailles, et le tri par base comparé à `qsort` sur des clés aléatoires de 5 à 64 bits, triées, inversées, toutes égales, aux bornes), dont les erreurs de syntaxe, les indices hors bornes, les fins de ligne CRLF, et les 3 entrées trouvées par le fuzzer.
- **Intégration** (CTest) : `meshinfo` sur un cube OBJ, un tétraèdre PLY, un tore OBJ, un fichier absent et sans argument.
- **Fuzzing** (`tests/fuzz/fuzz_read.c`) : toute entrée doit donner un maillage valide (indices dans les bornes) ou une erreur.
- **CI** (`.github/workflows/lib-c.yml`) : Linux (GCC), Windows (MSVC) et macOS (Clang), avertissements traités en erreurs, plus une compilation ASan + UBSan, un passage Valgrind, 60 s de fuzzing, et la vérification que le WebAssembly commité correspond aux sources.
- **Site** : `tests/unit/wasm.test.mjs` charge le WebAssembly dans Node, et Playwright teste la démo dans 5 navigateurs.

## Limites
- Les faces ne peuvent référencer que des sommets déjà lus (cas de tous les exportateurs courants).
- `strtod` dépend de la locale : un programme qui appelle `setlocale` avec une locale à virgule décimale lira mal les coordonnées.
- Index sur 32 bits : au plus 4 294 967 295 sommets.
- PLY : un seul élément `vertex` est lu (le premier) ; en-tête limité à 16 éléments de 32 propriétés, lignes de 255 caractères (commentaires exceptés) ; pas de numéro de ligne pour une erreur dans un corps binaire.

Maillages de test écrits à la main, aucune donnée de laboratoire. Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
