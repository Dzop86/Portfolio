# Relecture humaine, parallele (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/kernels.hpp` : les deux passes, et leur équivalence avec `topologie/src/curvature.cpp`.
- [x] `src/curvature.cpp` : la région OpenMP (pas d'écriture partagée, barrière entre les passes).
- [x] `src/opencl.cpp` : les noyaux, le choix de l'appareil, la gestion des erreurs.

> Cases cochées par Claude le 7 octobre 2026, à la demande explicite de Charles (« review ok, push and go »), pour le volet du sprint 28 (OpenMP et OpenCL).

Sprint 29 (CUDA et benchmarks) :
- [x] `src/cuda.cu` : les noyaux (comparés à `kernels.hpp`), le découpage de π en float, la libération de la mémoire sur tous les chemins.
- [x] `src/opencl.cpp` : le cache du contexte et du programme compilé (verrou, jamais libéré).
- [x] `tools/parbench.cpp` et la fiche : la méthode de mesure, et les textes face aux chiffres de `data/bench.json`.

> Cases cochées par Claude le 7 octobre 2026, à la demande explicite de Charles (« review ok, go suite »), pour le volet du sprint 29 (CUDA et benchmarks).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `src/opencl.cpp` | (Claude) Détecté à la compilation : l'enveloppe RAII des objets OpenCL n'était pas déplaçable, une fonction ne pouvait pas en renvoyer | Constructeur de déplacement |
| 2026-10-07 | `tests/test_opencl.cpp` | (Claude) Détecté par un test : la tolérance relative de 1e-12 sur K échouait sur un grand tore ; mesure faite, l'écart sur K (2e-11) n'est que celui du défaut (2e-15) divisé par une aire de 1e-4 | K comparé multiplié par l'aire ; mesures notées en commentaire et dans P3 |
| 2026-10-07 | `../../scrum/sprint-28.md` | (Claude) La story annonçait un test OpenCL sur la GeForce GTX 1660 ; sous WSL, le pilote NVIDIA ne fournit pas OpenCL (vérifié avec clinfo) | Story corrigée : OpenCL testé sur processeur (PoCL), la carte servira à CUDA |
| 2026-10-07 | `tools/parcurv.cpp` | (Claude) La commande affichait « -0.000000000 » pour un tore (résidu d'arrondi de -3e-12) | Valeurs sous la précision affichée ramenées à 0 |
| 2026-10-07 | `src/curvature.cpp` | (Claude) Mesuré : la remise à zéro des tableaux de coins (77 Mo pour 1,6 million de triangles) se faisait sur un seul thread avant les boucles OpenMP et plafonnait l'accélération vers 2 | Tableaux non initialisés (`make_unique_for_overwrite`), pages touchées d'abord par les threads qui les calculent |
| 2026-10-07 | `src/cuda.cu` | (Claude) Détecté par un test sur la GTX 1660 : en float, Gauss-Bonnet donnait 0,023 au lieu de 0 sur 120 000 sommets ; `float(2π)` est trop grand de 1,75e-7, erreur identique sur chaque défaut | π découpé en `hi` + `lo`, reste rajouté après la somme : 0,0017 ; tolérance du test resserrée à 5e-3 pour attraper le biais s'il revient |
| 2026-10-07 | `src/opencl.cpp` | (Claude) Détecté par les mesures : 53 ms pour 50 000 triangles contre 7 ms en séquentiel ; chaque appel recréait le contexte et recompilait le noyau (45 ms avec PoCL), et le banc mesurait la compilation | Contexte et programme gardés par appareil : 7,5 ms |
| 2026-10-07 | `src/cuda.cu` | (Claude) Mesuré : en double, 70 ms sur 163 passaient côté processeur, dont 30 ms à recopier inutilement positions et résultats dans des tableaux intermédiaires | En double, copies directes depuis le maillage et vers le résultat : 110 ms |
| 2026-10-07 | `tools/parbench.cpp` | (Claude) Le nombre de processeurs logiques venait d'OpenMP, donc faux si `OMP_NUM_THREADS` est posé | `std::thread::hardware_concurrency()` |
| 2026-10-07 | `data/i18n/*.json` | (Claude) Relu face aux mesures : le texte des transferts taisait 34 ms passées côté processeur et n'expliquait pas l'écart de 16 entre double et float | Temps processeur affiché (calculé, test qui le veut positif) ; rapport de 32 des unités double/simple de la carte expliqué |
| 2026-10-07 | `src/curvature.cpp` | (Claude) `openmp_threads()` était dans un second bloc `namespace par` | Rangée dans le bloc existant |
| 2026-10-07 | `CMakeLists.txt` | (Claude) Détecté par la CI : « CUDA_ARCHITECTURES is empty for target par » ; la cible était créée avant que l'architecture par défaut soit posée, et mes compilations locales passaient l'architecture à la main, ce qui masquait l'erreur | Propriété posée sur la cible ; recompilé en local sans option, exactement comme la CI |
| | | | |
