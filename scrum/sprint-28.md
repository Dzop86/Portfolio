# Sprint 28 : le calcul parallèle, OpenMP et OpenCL

**Objectif :** premier des deux sprints du projet parallele : la courbure de Gauss d'un maillage (défaut angulaire et aire de Voronoï mixte) calculée en C++ séquentiel, en OpenMP sur tous les cœurs et en OpenCL sur la carte graphique ou le processeur, avec des résultats identiques à ceux du projet topologie.

**Goal:** first of the two sprints of the parallele project: the Gaussian curvature of a mesh (angle defect and mixed Voronoi area) computed in sequential C++, with OpenMP on every core and with OpenCL on the graphics card or the processor, with results identical to those of the topologie project.

| Story | Points | État |
|---|---|---|
| En tant qu'ingénieur, je calcule la courbure de Gauss d'un grand maillage (parallele) en C++ séquentiel puis en OpenMP : maillage à plat (tableaux et voisinages compressés), résultats OpenMP identiques bit à bit au séquentiel quel que soit le nombre de cœurs, égaux à ceux de topologie, Gauss-Bonnet vérifié ; GoogleTest ; CI Linux, Windows et macOS. | 3 | Fait |
| En tant qu'ingénieur, je lance le même calcul en OpenCL (parallele) sur la carte graphique ou le processeur : noyaux en double précision, résultats égaux au séquentiel à 1e-12 près ; testé en CI et en local avec PoCL (OpenCL sur processeur ; sous WSL, le pilote NVIDIA ne fournit pas OpenCL, la carte graphique servira à CUDA au sprint 29) ; limites des autres systèmes documentées. | 3 | Fait |

**Tests :** 14 tests GoogleTest : séquentiel identique au bit à topologie sur 7 maillages, OpenMP identique au bit au séquentiel de 1 à 16 threads, OpenCL (PoCL) à 1e-12 ; ASan + UBSan ; Clang en local. **Trouvé en route :** une tolérance sur K mal posée (l'écart est celui du défaut divisé par l'aire), et OpenCL indisponible pour la carte NVIDIA sous WSL.

## Rétro (Charles)
- Ce qui a marché : Séquentiel identique au bit à topologie, OpenMP au séquentiel, OpenCL à 1e-12.
- Ce que l'IA a mal fait : Une tolérance sur K mal posée (l'écart est celui du défaut divisé par l'aire).
- À changer au prochain sprint : Dériver chaque tolérance de la formule, pas d'un essai.
