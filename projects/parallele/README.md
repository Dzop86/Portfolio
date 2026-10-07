# parallele : la courbure de Gauss en parallèle (OpenMP, OpenCL, CUDA)

[![parallele](https://github.com/Dzop86/Portfolio/actions/workflows/parallele.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/parallele.yml)

La courbure de Gauss discrète d'un maillage (défaut angulaire sur aire de Voronoï mixte), celle du projet [topologie](../topologie/), calculée sur tous les cœurs du processeur (OpenMP) et sur un accélérateur (OpenCL, CUDA), avec des résultats vérifiés contre la référence et des benchmarks affichés sur la fiche du projet. Les fichiers sont lus par [lib-c](../lib-c/).

*Discrete Gaussian curvature of a mesh computed in parallel: sequential C++ identical bit for bit to the topologie project, OpenMP identical bit for bit to sequential on any number of threads, OpenCL and CUDA in double precision within 1e-12, CUDA in single precision measured; GoogleTest on Linux, Windows and macOS, OpenCL tested on the processor with PoCL, CUDA on a GeForce GTX 1660; benchmarks of every version on the project page.*

## L'algorithme, en deux passes indépendantes
1. **Par face** : les angles de ses trois coins (`atan2`, précis près de 0 et de π) et la part de son aire que reçoit chaque coin (cellule de Voronoï dans un triangle non obtus, sinon la moitié au coin obtus et un quart aux autres ; Meyer et al. 2003).
2. **Par sommet** : défaut angulaire 2π − Σ angles (π − Σ au bord) et aire, sommés sur ses coins.

Chaque itération d'une passe est indépendante : pas de verrou ni d'opération atomique. Les coins de chaque sommet sont rangés dans l'ordre des faces (voisinage compressé, `Adjacency`), si bien que chaque sommet additionne dans le même ordre que la boucle de topologie, quel que soit le cœur qui le calcule. D'où des résultats **identiques au bit près** : séquentiel contre topologie, OpenMP contre séquentiel, sur 1 à 16 threads. La somme totale est faite dans l'ordre des sommets, après le calcul parallèle.

## Organisation
- `include/par/mesh.hpp`, `src/mesh.cpp` : maillage en tableaux plats (copiables tels quels vers une carte graphique), voisinage compressé, sommets de bord, tore de taille quelconque.
- `src/kernels.hpp` : les deux passes, écrites une fois pour les versions séquentielle et OpenMP.
- `src/curvature.cpp` : séquentiel et OpenMP (`schedule(static)`, deux boucles dans une même région parallèle).
- `src/opencl.cpp` : les mêmes passes en OpenCL C, double précision, `FP_CONTRACT OFF` (pas de fusion multiplication-addition, comme en C++) ; appareils sans double précision écartés, cartes graphiques d'abord.
- `src/cuda.cu` : les mêmes passes en CUDA, en double et en simple précision (π découpé en partie haute et reste), `--fmad=false` ; temps des copies et des noyaux par événements CUDA.
- `src/io.cpp` : lecture par lib-c (OBJ, PLY, STL).
- `tools/parcurv.cpp` : la commande ; `tools/parbench.cpp` : les benchmarks, écrits en JSON (`data/bench.json`, lu par le site).

## Compiler et lancer
CMake 3.21, un compilateur C++20 avec OpenMP ; OpenCL facultatif (Linux : `ocl-icd-opencl-dev`, `pocl-opencl-icd` pour l'exécuter sur le processeur) ; CUDA facultatif (CUDA 12 ou 13, carte Turing ou plus récente).
```sh
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
ctest --test-dir build -C Release --output-on-failure
./build/parcurv torus:1000:800 --runs 3          # 1,6 million de triangles, chaque version chronométrée
./build/parcurv ../lib-c/tests/data/cube.obj      # somme des défauts / 2π = 2 (χ du cube)
./build/parcurv --devices                         # appareils OpenCL en double précision, cartes CUDA
./build/parbench --threads 6 --out data/bench.json # les benchmarks de la fiche (6 = cœurs physiques)
```
Options : `--backend seq|omp|opencl|cuda|cuda-float|all`, `--threads N`, `--runs N` (médiane). CMake : `-DPAR_OPENCL=OFF`, `-DPAR_CUDA=OFF`, `-DPAR_SANITIZE=ON`.

## Tests
GoogleTest, 18 tests :
- **Voisinage** : chaque coin sous son sommet, dans l'ordre des faces ; sommets de bord identiques à ceux de topologie sur les 7 maillages de référence ; une arête partagée par trois faces n'est pas un bord ; le tore est une surface fermée de caractéristique 0.
- **Séquentiel** : défauts, aires, courbures et somme **identiques au bit près** à topologie sur les maillages de référence (tore, sphère, ruban de Möbius, selle, cube OBJ et STL, tétraèdre PLY) ; Gauss-Bonnet (0 pour le tore et le ruban, 4π pour la sphère).
- **OpenMP** : identique au bit près au séquentiel sur un tore de 120 000 triangles, de 1 à 16 threads ; OpenMP bien présent dans la compilation.
- **OpenCL** : à 1e-12 près du séquentiel sur les maillages de référence et sur un tore de 240 000 triangles (dont la taille n'est pas un multiple du groupe de travail). Écart mesuré avec PoCL : 1,9e-15 au plus sur les défauts, 94 % d'entre eux identiques au bit. Sans appareil OpenCL, ces tests sont sautés, sauf si `PAR_REQUIRE_OPENCL` est posé (CI Linux), où ils échouent.
- **CUDA** (sur la GeForce GTX 1660) : en double, à 1e-12 près du séquentiel sur les maillages de référence et sur un tore de 240 000 triangles (écart mesuré : 2,9e-15 au plus) ; en float, 1,2e-6 au plus sur un défaut et Gauss-Bonnet à 0,0017 sur 120 000 sommets (borne du test : 5e-3) ; un appareil absent est refusé. Sans carte, ces tests sont sautés, sauf si `PAR_REQUIRE_CUDA` est posé.
- **Lecture** : une erreur de lib-c remonte avec sa ligne ; un maillage sans face.
- Vérifié en cassant le code : sans le demi-tour des sommets de bord, trois tests échouent (topologie, Gauss-Bonnet, OpenCL).

**CI** (`.github/workflows/parallele.yml`) : Linux (GCC, OpenCL avec PoCL), Windows (MSVC, OpenMP 2.0) et macOS (Apple Clang avec libomp de Homebrew), avertissements traités en erreurs ; une compilation ASan + UBSan ; une compilation CUDA dans l'image `nvidia/cuda:13.0.1-devel` (noyaux compilés, avertissements en erreurs, tests sautés faute de carte) ; relancée quand lib-c ou topologie changent. Compilé aussi avec Clang en local, et testé avec CUDA sur la GTX 1660 (`docker run --gpus all`).

## Limites
- **macOS** : sans OpenCL (déprécié par Apple, sans double précision sur Apple Silicon) ; OpenMP demande libomp.
- **Windows** : sans OpenCL en CI (pas de pilote sur les machines de GitHub) ; la version OpenCL se compile si un SDK OpenCL est installé.
- **Carte graphique** : la CI n'en a pas ; les tests CUDA ne tournent qu'en local, sur la GeForce GTX 1660. Sous WSL, le pilote NVIDIA fournit CUDA mais pas OpenCL : OpenCL est testé et mesuré sur le processeur (PoCL).
- **CUDA** : pas sur macOS (abandonné par NVIDIA), pas en CI Windows (le compilateur CUDA n'y est pas installé) ; la version se compile partout où le CUDA Toolkit est présent.
- **Benchmarks** : une seule machine (Core i5-10400F, GTX 1660, WSL 2, bande passante mémoire plafonnée à 5 ou 6 Go/s), refaits à la main ; voir `DECISIONS.md` (P6).

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
