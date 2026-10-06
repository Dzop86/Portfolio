# topologie : invariants topologiques et courbure de maillages, en C++

[![topologie](https://github.com/Dzop86/Portfolio/actions/workflows/topologie.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/topologie.yml)

Bibliothèque C++20 qui construit une structure demi-arête à partir d'un maillage lu par [lib-c](../lib-c/), puis calcule ses invariants topologiques. Sujet de ma thèse, réécrit de zéro sur des maillages synthétiques.

*C++20 half-edge mesh built on lib-c's reader: topological invariants and discrete curvature, tested with GoogleTest on Linux, Windows and macOS.*

## État (sprint 4)
- Structure demi-arête : jumelles, arêtes de bord, arêtes non-variété (partagées par 3 faces ou plus), orientations incohérentes repérées.
- Invariants (`topo::analyze`) : composantes connexes, sommets isolés, boucles de bord, arêtes et sommets non-variété, orientabilité, caractéristique d'Euler, genre total d'une surface orientable.
- Courbure de Gauss discrète (`topo::gaussian_curvature`) : défaut angulaire par sommet (π − Σθ au bord), aire de Voronoï mixte, densité K ≈ défaut / aire (à 2 % près sur une icosphère).
- Chargement OBJ et PLY par lib-c (`add_subdirectory`), erreurs remontées en `topo::LoadError` avec leur ligne.

- WebAssembly : `scripts/build-wasm.sh` (Emscripten 6.0.11 dans Docker), testé dans Node par `tests/unit/topo-wasm.test.mjs` du site.

## À venir (sprint 5)
Viewer Three.js sur la fiche du projet : maillages colorés par courbure, invariants affichés, calcul en WebAssembly.

## Compiler et tester
```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Debug
cmake --build build --config Debug
ctest --test-dir build -C Debug --output-on-failure
```
Options : `-DTOPO_SANITIZE=ON` (ASan et UBSan), `-DTOPO_BUILD_TESTS=OFF`. GoogleTest v1.18.0 est téléchargé au premier `cmake`.

## Tests
- **Unitaires** (GoogleTest) : invariants de la structure (`next` d'ordre 3, `twin` involutive), bords, arête non-variété, orientations incohérentes, entrées invalides.
- **Surfaces de référence** (`tests/shapes.hpp`) : tore, cylindre et ruban de Möbius générés sur une grille, union de deux tores ; leurs invariants sont connus d'avance.
- **Gauss-Bonnet** : sur chaque surface, la somme des défauts vaut 2πχ à 10⁻⁹ près, y compris sur 20 tores déformés au hasard (la somme ne dépend que de la topologie).
- **Exemples du viewer** (`samples/`, générés par `scripts/make_samples.py`) : invariants et signe de la courbure vérifiés (sphère K ≈ 1, tore positif dehors et négatif dedans, selle négative).
- **Intégration** : chargement des fichiers de lib-c, nombre d'arêtes identique à celui calculé par lib-c.
- **CI** (`.github/workflows/topologie.yml`) : Linux, Windows et macOS, plus ASan + UBSan ; relancée aussi quand lib-c change.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
