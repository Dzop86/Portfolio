# Sprint 37 : les diagrammes de persistance

**Objectif :** demande de Charles (D48) : sur la visionneuse de topologie, le diagramme de persistance de la hauteur, calculé par le code C++ : chaque composante, anse ou cavité y naît à un point critique et meurt à un autre, et la distance à la diagonale sépare les vraies formes du bruit.

**Goal:** Charles's request (D48): on the topology viewer, the persistence diagram of the height, computed by the C++ code: every component, handle or cavity is born at one critical point and dies at another, and the distance to the diagonal tells real features from noise.

| Story | Points | État |
|---|---|---|
| En tant que géomètre, j'obtiens la persistance de la hauteur d'un maillage (topologie) : filtration « lower-star », réduction de la matrice de bord sur Z/2 (avec « clearing »), paires naissance-mort en dimensions 0, 1 et 2 rattachées à leurs sommets critiques, classes sans fin ; GoogleTest (sphère : H0 et H2 sans fin ; tore debout : 1, 2, 1 classes sans fin nées aux bons sommets ; sphère bosselée : une paire par bosse, de persistance égale à sa hauteur ; classes sans fin égales aux nombres de Betti sur tous les maillages ; stabilité : une perturbation de ε déplace chaque point d'au plus ε en distance bottleneck) ; vérifié en cassant le code. | 2 | Fait |
| En tant que visiteur, je lis le diagramme de persistance sur la fiche (topologie) : nuage naissance-mort en SVG avec la diagonale, une forme par dimension, classes sans fin sur une ligne « ∞ », seuil de persistance qui grise le bruit et marque sur le maillage les points critiques des paires gardées ; WebAssembly, FR/EN, clavier, lecteur d'écran (tableau des paires) et mobile ; Node et Playwright. | 2 | Fait |

**Tests :** 9 tests GoogleTest (`tests/test_persistence.cpp` : sphère, tore debout, terrain à cuvettes, nombres de Betti sur six surfaces et quatre directions, sommets critiques, ordre, stabilité en distance bottleneck avec son oracle), vérifiés en cassant le code (cinq mutations attrapées, une équivalente expliquée) ; ASan et UBSan ; Clang 19 sans avertissement ; 4 tests Node (2 sur le diagramme de `topo-api.js`, 2 sur le module WebAssembly : nombres de Betti, multiplicités, plafond de 300 000 triangles) ; 5 tests Playwright (5 navigateurs : nombres de Betti des exemples, terrain déposé, seuil à la souris et au clavier, tableau, mobile, accessibilité). Mesuré : 0,5 s en WebAssembly à 300 000 triangles. **Trouvé en route :** deux tests qui passaient sans rien tester (diagramme presque vide, tri sans paire à trier) ; `stable_sort` refusé par Clang ; NaN dans le tri des classes sans fin ; le tableau qui élargissait la page sur iPhone ; le curseur sans piste ; le cadre du tableau inaccessible au clavier.

## Rétro (Charles)
- Ce qui a marché : Persistance testée par la stabilité en distance bottleneck, avec son oracle.
- Ce que l'IA a mal fait : Deux tests qui passaient sans rien tester, NaN dans un tri, un tableau qui élargissait la page sur iPhone.
- À changer au prochain sprint : Exiger qu'un test ait de la matière (assez de paires) avant qu'il conclue.
