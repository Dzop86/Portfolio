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
- [x] Sprint 37 : `src/persistence.cpp` (ordre de la filtration, réduction et « clearing », paires d'un même sommet écartées) et ses tests (oracle bottleneck) ; `topoc_persistence` ; le diagramme de `src/viewer/topoviewer.js` (seuil, marqueurs, tableau).
- [x] Sprint 38 : `src/reeb.cpp` (nœuds par le lien, tranches et recollement, barycentres des lignes de niveau) et ses tests ; `topoc_reeb` ; le graphe de Reeb de `src/viewer/topoviewer.js` (dans le maillage et à plat).

> Cases cochées par Claude le 8 octobre 2026, à la demande explicite de Charles (« valide »), pour les sprints 37 et 38.

- [x] Sprint 39 : `barcode`, `aliveAt`, `levelBetween` et `tour` de `src/assets/topo-api.js` et leurs tests ; dans `src/viewer/topoviewer.js`, le code-barres, la paire choisie (halos, seuil, listes à tabulation mobile) et l'explication guidée. Le diagramme se lit-il maintenant sans connaître la persistance ?
- [x] Sprint 40 : `tools/bench.hpp`, `tools/topo_bench.cpp`, `scripts/bench.mjs` (même tore en C++ et en JavaScript, meilleur de 3) et `data/bench.json` ; les temps affichés par la visionneuse ; les notes GPU et 32 Mo de la fiche.

> Cases cochées par Claude le 8 octobre 2026, à la demande explicite de Charles (« valide la review »), pour les sprints 39 et 40.

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
| 2026-10-08 | `src/viewer/topoviewer.js` | (Claude) Vu sur capture : le halo d'une paire choisie, en pistache, disparaissait sur le haut du maillage, pistache lui aussi en mode hauteur | Halo dans la couleur du texte |
| 2026-10-08 | `src/viewer/topoviewer.js` | (Claude) Vu sur capture : dans le code-barres, le titre de chaque groupe touchait sa première barre | Interligne du titre agrandi |
| 2026-10-08 | `src/viewer/topoviewer.js` | (Claude) Limite assumée : les barres (10 unités de haut) et les points du nuage ne font pas 44 px au doigt ; au toucher, le chemin conforme passe par les boutons du tableau des paires (44 px), noté dans D49 | Rien à corriger ; signalé pour la relecture |
| 2026-10-08 | `src/viewer/topoviewer.js` | (Claude) Détecté par Playwright sous WebKit : `focusin` n'arrive pas des éléments SVG, une barre atteinte au clavier n'était pas choisie | Écouteur `focus` en phase de capture, et les flèches choisissent la barre elles-mêmes |
| 2026-10-08 | `src/viewer/topoviewer.js` | (Claude) Détecté par Playwright sous WebKit, instrumenté : choisir une paire affiche une phrase qui décale la page, et WebKit envoie alors un `pointerover` puis un `pointermove` immobile pour la barre glissée sous le pointeur, qui reprenait la place de la barre choisie au clavier ; `movementX` ne distingue rien (0 aussi pour un vrai mouvement) | Survol par `pointermove` seulement, retenu si la position diffère de la précédente (suivie sur toute la page) |
| 2026-10-08 | `Dockerfile` | (Claude) Détecté par `docker-context.test.mjs` : le tableau des temps (`projects/topologie/data/bench.json`) n'était pas copié dans l'image, la construction du site dans Docker aurait échoué | Ligne `COPY` ajoutée |
| 2026-10-08 | `scripts/bench.mjs` | (Claude) Constat de mesure : en WebAssembly, lire l'OBJ coûte plus que tout le reste (2,5 s pour 37 Mo, contre 0,24 s pour la hauteur) ; c'est elle qui justifie la limite de 32 Mo, pas la persistance | Chiffre repris dans la note de la fiche |
| 2026-10-08 | `src/viewer/topoviewer.js` | Signalé par Charles : le nuage de points ne se lit pas, les paires devraient apparaître où elles naissent et meurent. Les positions étaient justes (x = naissance, y = mort), mais sur le terrain à cuvettes les trois paires meurent au sommet et leurs points s'alignaient contre la ligne ∞, sans rien pour montrer leur durée | (Claude) Chaque paire est aussi une barre verticale, de la diagonale à sa hauteur de naissance jusqu'au point de sa mort (en tireté jusqu'à ∞ pour une classe sans fin), graduation du haut de l'axe des morts ; texte du diagramme réécrit ; Playwright vérifie les barres |
| 2026-10-08 | `scripts/bench.mjs` | (Claude) CI rouge sous Windows seulement : le test qui importe le banc échouait, `import()` d'un chemin absolu (`D:\...`) est refusé par Node, qui veut une URL `file://` ; le script du raytracer avait le même défaut, jamais lancé sous Windows | `pathToFileURL` dans les deux scripts |
| | | | |
