# Relecture humaine, topologie (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/mesh.cpp`, constructeur : appariement des jumelles, cas des arêtes partagées par 3 faces.
- [x] `src/invariants.cpp` : formule du genre et traitement des sommets isolés.
- [x] `src/curvature.cpp` : défaut au bord (π − Σθ) et sommets non-variété.
- [x] `tests/test_halfedge.cpp`, `expect_consistent` : les invariants vérifiés sont-ils les bons ?

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« valide les relectures »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/mesh.cpp` | (Claude) Détecté par les tests : `check(mesh_read_buffer(..., &line), line)` lisait `line` avant l'appel (ordre d'évaluation des arguments non spécifié en C++), les erreurs remontaient toujours en ligne 0 | Appel et lecture de `line` en deux instructions |
| 2026-10-06 | `src/curvature.cpp` | (Claude) Indice de boucle en `int` sur un `std::array` : Clang (macOS) inclut `-Wsign-conversion` dans `-Wconversion`, contrairement à GCC en C++, et la CI macOS aurait échoué avec `-Werror` | Indice en `std::size_t`, vérifié avec Clang 19 dans Docker |
| 2026-10-06 | `src/curvature.cpp` | (Claude) Détecté par un test sur l'icosphère : avec l'aire barycentrique, la densité K valait 1,15 au lieu de 1 aux 12 sommets de valence 5, des taches dans le viewer | Aire de Voronoï mixte (Meyer et al. 2003), écart sous 2 % |
| 2026-10-06 | `src/viewer/topoviewer.js` | (Claude) Vu sur capture d'écran : les bords de la selle apparaissaient en rouge (dômes) et l'échelle montait à ±25, car le défaut angulaire au bord mesure le virage du bord | Drapeau `boundary` exporté par le C++ ; sommets du bord colorés d'après leurs voisins, exclus de l'échelle |
| 2026-10-06 | `src/viewer/topoviewer.js` | (Claude) Vu sur capture d'écran : la somme des défauts du tore s'affichait « −0 × 2π » | `turns()` arrondit et normalise −0 |
| 2026-10-06 | `src/viewer/topoviewer.js` | (Claude) Détecté par Playwright sous WebKit : « ResizeObserver loop », le redimensionnement modifiait la taille de l'élément observé | Taille fixée en CSS (`aspect-ratio`), le script ne fait que la lire |
| 2026-10-06 | `src/viewer/topoviewer.js` | (Claude) Vu en chargeant les modèles de Charles en local : sur un maillage sculpté, l'échelle linéaire laissait presque tout gris (queue de |K| 1 000 fois la médiane) | Échelle par quantiles, légende graduée (D18 du site) |
| | | | |
