# Relecture humaine, topologie (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/mesh.cpp`, constructeur : appariement des jumelles, cas des arêtes partagées par 3 faces.
- [x] `src/invariants.cpp` : formule du genre et traitement des sommets isolés.
- [x] `src/curvature.cpp` : défaut au bord (π − Σθ) et sommets non-variété.
- [x] `tests/test_halfedge.cpp`, `expect_consistent` : les invariants vérifiés sont-ils les bons ?

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« valide les relectures »).
- [x] Sprint 36 : `src/morse.cpp` (lien inférieur, indice, départage des égalités) et ses tests ; le mode hauteur de `src/viewer/topoviewer.js` (filtration, marqueurs, courbe de χ).

> Case cochée par Claude le 8 octobre 2026, à la demande explicite de Charles (« j'ai relu, c'est top »), pour le sprint 36.
- [ ] Sprint 37 : `src/persistence.cpp` (ordre de la filtration, réduction et « clearing », paires d'un même sommet écartées) et ses tests (oracle bottleneck) ; `topoc_persistence` ; le diagramme de `src/viewer/topoviewer.js` (seuil, marqueurs, tableau).
- [ ] Sprint 38 : `src/reeb.cpp` (nœuds par le lien, tranches et recollement, barycentres des lignes de niveau) et ses tests ; `topoc_reeb` ; le graphe de Reeb de `src/viewer/topoviewer.js` (dans le maillage et à plat).

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
| 2026-10-07 | `src/morse.cpp`, `tests/test_morse.cpp` | (Claude) Vu en compilant avec `-Wsign-conversion` (que Clang active avec `-Wconversion`, pas GCC) : un `int` indexait un `std::array` ; la CI macOS aurait échoué. Dans le test, un `int64_t` rangé dans un `int` et un `EXPECT` sans accolades | Indice `std::size_t`, `auto`, accolades |
| 2026-10-07 | `src/viewer/topoviewer.js` | (Claude) Relecture : une sphère et un matériau créés par point critique, à chaque maillage et à chaque changement d'axe, jamais libérés (mémoire de la carte graphique) | Une sphère et un matériau par sorte, partagés |
| 2026-10-07 | `src/viewer/topoviewer.js` | (Claude) Relecture : `Math.max(...euler)` sur un tableau d'une valeur par sommet dépasse le nombre d'arguments d'un appel sur un gros fichier déposé ; le chemin de la courbe avait un segment par sommet | Bornes par une boucle ; le chemin ne garde que les sauts de χ |
| 2026-10-08 | `tests/test_persistence.cpp` | (Claude) Le test de stabilité passait, mais sa fonction « rugueuse » n'avait que 2 paires : il ne testait presque rien | Bruit plus fort, grille plus grande, et le test exige au moins 10 paires avant de mesurer |
| 2026-10-08 | `tests/test_persistence.cpp` | (Claude) Le test de l'ordre des paires passait même en cassant le tri : sur un tore lisse, il n'y a aucune paire finie à ordonner | Hauteur bruitée, plus de 10 paires exigées ; la mutation est maintenant attrapée |
| 2026-10-08 | `src/persistence.cpp` | (Claude) Mutation survivante, et c'est normal : mettre les faces avant leurs arêtes au sein d'un même sommet ne change pas les paires entre sommets (elles ne dépendent que des rangs des blocs de la matrice de bord, un bloc par sommet) | Rien à corriger ; noté pour la relecture |
| 2026-10-08 | `src/persistence.cpp` | (Claude) Vu en compilant avec Clang 19 : `std::stable_sort` appelle une fonction marquée obsolète de la bibliothèque de GCC 12, et `-Werror` refuse | `std::sort` sur un ordre total (les sommets départagent), sortie déterministe |
| 2026-10-08 | `src/assets/topo-api.js` | (Claude) Relecture : le tri du diagramme soustrayait les persistances ; pour deux classes sans fin, Infinity − Infinity = NaN, et l'ordre ne tenait que par chance | Comparaison explicite |
| 2026-10-08 | `CMakeLists.txt` | (Claude) CI rouge sur Windows seulement après le sprint 38 : `gtest_discover_tests` n'a pas obtenu la liste des tests dans le délai par défaut de 5 s (sortie vide, aucun test lancé) ; un exécutable Debug fraîchement compilé démarre lentement sur le runner | `DISCOVERY_TIMEOUT 60` |
| | | | |
