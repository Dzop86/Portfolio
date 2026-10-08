# Sprint 29 : le calcul parallèle, CUDA et benchmarks

**Objectif :** second sprint du projet parallele : la même courbure en CUDA sur la GeForce GTX 1660, en double précision (à 1e-12 du séquentiel) et en simple précision (écart mesuré), puis des benchmarks rigoureux de toutes les versions, affichés sur la fiche du projet.

**Goal:** second sprint of the parallele project: the same curvature in CUDA on the GeForce GTX 1660, in double precision (within 1e-12 of sequential) and in single precision (error measured), then rigorous benchmarks of every version, shown on the project page.

| Story | Points | État |
|---|---|---|
| En tant qu'ingénieur, je calcule la courbure en CUDA (parallele) : noyaux en double et en simple précision, `--fmad=false`, mémoire libérée sur tous les chemins, temps de copie et de calcul mesurés par événements CUDA ; testé sur la GeForce GTX 1660 (à 1e-12 en double, Gauss-Bonnet tenu en float) ; CI qui compile les noyaux dans l'image CUDA de NVIDIA (sans carte, tests sautés) ; limites documentées. | 4 | Fait |
| En tant que visiteur, je compare les versions sur la fiche du projet : `parbench` mesure séquentiel, OpenMP, OpenCL et CUDA sur des tores de 50 000 à 3,2 millions de triangles (médiane de 7 essais après échauffement), et la fiche affiche graphique, tableau, décomposition copies/calcul et explication des écarts, en français et en anglais. | 3 | Fait |

**Tests :** 18 tests GoogleTest, tous passés sur la GTX 1660 (dont 4 CUDA) ; compilation sans CUDA avec Clang, et avec GCC sous ASan + UBSan ; 651 tests unitaires du site (dont la cohérence des mesures), 520 tests Playwright. **Trouvé en route :** en simple précision, `float(π)` biaisait chaque défaut (Gauss-Bonnet à 0,023 au lieu de 0), corrigé en découpant π en partie haute et reste ; OpenCL recompilait son noyau à chaque appel (45 ms), et le banc mesurait donc la compilation ; CUDA en double recopiait inutilement positions et résultats (30 ms).

## Rétro (Charles)
- Ce qui a marché : CUDA testé sur la vraie GTX 1660, bancs complets affichés sur la fiche.
- Ce que l'IA a mal fait : `float(π)` biaisait la simple précision, OpenCL recompilait son noyau à chaque appel (le banc mesurait la compilation), CUDA recopiait inutilement.
- À changer au prochain sprint : Vérifier ce que mesure un banc avant d'en publier les chiffres.
