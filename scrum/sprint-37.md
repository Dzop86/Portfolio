# Sprint 37 : les diagrammes de persistance

**Objectif :** demande de Charles (D48) : sur la visionneuse de topologie, le diagramme de persistance de la hauteur, calculé par le code C++ : chaque composante, anse ou cavité y naît à un point critique et meurt à un autre, et la distance à la diagonale sépare les vraies formes du bruit.

**Goal:** Charles's request (D48): on the topology viewer, the persistence diagram of the height, computed by the C++ code: every component, handle or cavity is born at one critical point and dies at another, and the distance to the diagonal tells real features from noise.

| Story | Points | État |
|---|---|---|
| En tant que géomètre, j'obtiens la persistance de la hauteur d'un maillage (topologie) : filtration « lower-star », réduction de la matrice de bord sur Z/2 (avec « clearing »), paires naissance-mort en dimensions 0, 1 et 2 rattachées à leurs sommets critiques, classes sans fin ; GoogleTest (sphère : H0 et H2 sans fin ; tore debout : 1, 2, 1 classes sans fin nées aux bons sommets ; sphère bosselée : une paire par bosse, de persistance égale à sa hauteur ; classes sans fin égales aux nombres de Betti sur tous les maillages ; stabilité : une perturbation de ε déplace chaque point d'au plus ε en distance bottleneck) ; vérifié en cassant le code. | 2 | À faire |
| En tant que visiteur, je lis le diagramme de persistance sur la fiche (topologie) : nuage naissance-mort en SVG avec la diagonale, une forme par dimension, classes sans fin sur une ligne « ∞ », seuil de persistance qui grise le bruit et marque sur le maillage les points critiques des paires gardées ; WebAssembly, FR/EN, clavier, lecteur d'écran (tableau des paires) et mobile ; Node et Playwright. | 2 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
