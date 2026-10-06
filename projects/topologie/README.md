# topologie : invariants topologiques et courbure de maillages, en C++

[![topologie](https://github.com/Dzop86/Portfolio/actions/workflows/topologie.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/topologie.yml)

Bibliothèque C++20 qui construit une structure demi-arête à partir d'un maillage lu par [lib-c](../lib-c/), puis calcule ses invariants topologiques. Sujet de ma thèse, réécrit de zéro sur des maillages synthétiques.

*C++20 half-edge mesh built on lib-c's reader: topological invariants and discrete curvature, tested with GoogleTest on Linux, Windows and macOS.*

## État (sprint 4)
- Structure demi-arête : jumelles, arêtes de bord, arêtes non-variété (partagées par 3 faces ou plus), orientations incohérentes repérées.
- Chargement OBJ et PLY par lib-c (`add_subdirectory`), erreurs remontées en `topo::LoadError` avec leur ligne.

## Compiler et tester
```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Debug
cmake --build build --config Debug
ctest --test-dir build -C Debug --output-on-failure
```
Options : `-DTOPO_SANITIZE=ON` (ASan et UBSan), `-DTOPO_BUILD_TESTS=OFF`. GoogleTest v1.18.0 est téléchargé au premier `cmake`.

## Tests
- **Unitaires** (GoogleTest) : invariants de la structure (`next` d'ordre 3, `twin` involutive), bords, arête non-variété, orientations incohérentes, entrées invalides.
- **Intégration** : chargement des fichiers de lib-c, nombre d'arêtes identique à celui calculé par lib-c.
- **CI** (`.github/workflows/topologie.yml`) : Linux, Windows et macOS, plus ASan + UBSan ; relancée aussi quand lib-c change.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
