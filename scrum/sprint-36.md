# Sprint 36 : la hauteur et les points critiques des maillages

**Objectif :** demande de Charles : sur la visionneuse de topologie, une filtration selon la hauteur, comme le filtre Elevation de ParaView, et les points critiques de la hauteur (minimums, selles, maximums), calculés par le code C++ selon la théorie de Morse discrète.

**Goal:** Charles's request: on the topology viewer, a filtration by height, like ParaView's Elevation filter, and the critical points of the height (minima, saddles, maxima), computed by the C++ code with discrete Morse theory.

| Story | Points | État |
|---|---|---|
| En tant que géomètre, j'obtiens la hauteur et les points critiques d'un maillage (topologie) : hauteur de chaque sommet selon une direction, ordre de la filtration (égalités départagées par l'indice), points critiques par le lien inférieur de chaque sommet (minimum, maximum, selle et sa multiplicité), caractéristique d'Euler de chaque ensemble de sous-niveau ; GoogleTest (sphère convexe : un minimum et un maximum ; tore debout : un minimum, deux selles, un maximum ; selle de singe de multiplicité 2 ; somme des indices égale à χ sur tous les maillages ; χ du sous-niveau recalculé à la main). | 2 | Fait |
| En tant que visiteur, je filtre le maillage par la hauteur sur la fiche (topologie) : coloration par la hauteur, curseur qui ne garde que la partie sous un seuil, points critiques marqués et comptés, courbe de χ du sous-niveau qui saute à chaque point critique ; WebAssembly, FR/EN, clavier et mobile ; Node et Playwright. | 2 | Fait |

**Tests :** 7 tests GoogleTest (`tests/test_morse.cpp` : sphère, tore debout, selle de singe, somme des indices égale à χ dans quatre directions, χ du sous-niveau recompté à la main, sommet de bord), vérifiés en cassant le code ; 4 tests Node (3 sur la filtration de `topo-api.js`, 1 sur le module WebAssembly : le tore debout, la sphère, χ partout) ; 5 tests Playwright (5 navigateurs : mode hauteur, seuil au clavier, changement d'axe, valeur du seuil qui tient sur mobile, accessibilité). **Trouvé en route :** un `int` qui indexait un `std::array` (Clang l'aurait refusé sur macOS) ; des sphères et des matériaux jamais libérés ; `Math.max(...)` sur un tableau par sommet ; le panneau de hauteur visible malgré `hidden` (`display: grid`) ; la valeur du seuil qui dépassait sa colonne à 375 px.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
