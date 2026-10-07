# Relecture humaine, parallele (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/kernels.hpp` : les deux passes, et leur équivalence avec `topologie/src/curvature.cpp`.
- [x] `src/curvature.cpp` : la région OpenMP (pas d'écriture partagée, barrière entre les passes).
- [x] `src/opencl.cpp` : les noyaux, le choix de l'appareil, la gestion des erreurs.

> Cases cochées par Claude le 7 octobre 2026, à la demande explicite de Charles (« review ok, push and go »), pour le volet du sprint 28 (OpenMP et OpenCL).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `src/opencl.cpp` | (Claude) Détecté à la compilation : l'enveloppe RAII des objets OpenCL n'était pas déplaçable, une fonction ne pouvait pas en renvoyer | Constructeur de déplacement |
| 2026-10-07 | `tests/test_opencl.cpp` | (Claude) Détecté par un test : la tolérance relative de 1e-12 sur K échouait sur un grand tore ; mesure faite, l'écart sur K (2e-11) n'est que celui du défaut (2e-15) divisé par une aire de 1e-4 | K comparé multiplié par l'aire ; mesures notées en commentaire et dans P3 |
| 2026-10-07 | `../../scrum/sprint-28.md` | (Claude) La story annonçait un test OpenCL sur la GeForce GTX 1660 ; sous WSL, le pilote NVIDIA ne fournit pas OpenCL (vérifié avec clinfo) | Story corrigée : OpenCL testé sur processeur (PoCL), la carte servira à CUDA |
| 2026-10-07 | `tools/parcurv.cpp` | (Claude) La commande affichait « -0.000000000 » pour un tore (résidu d'arrondi de -3e-12) | Valeurs sous la précision affichée ramenées à 0 |
| | | | |
