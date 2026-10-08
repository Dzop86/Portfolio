# Sprint 31 : le dashboard React, visionneuse 3D et fiche

**Objectif :** second sprint du projet react : une visionneuse 3D dans le dashboard, qui colore les maillages du fil rouge selon leur courbure de Gauss calculée par le code C++ de topologie en WebAssembly, puis la fiche du projet avec ses captures.

**Goal:** second sprint of the react project: a 3D viewer in the dashboard, colouring the common-thread meshes by their Gaussian curvature computed by the topologie C++ code in WebAssembly, then the project page with its screenshots.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, je fais tourner un maillage dans le dashboard (react) : composant React autour de three.js (ressources libérées au démontage), courbure de Gauss et invariants calculés par topologie en WebAssembly (le module que publie le site), maillages listés par l'API (`meshes.json`) ou fichier ouvert depuis l'ordinateur, légende par quantiles, courbure lue au survol ; repli sans WebGL ; Vitest et Playwright. | 4 | Fait |
| En tant que recruteur, je vois le dashboard sur la fiche du projet (react) : captures en français et en anglais, thèmes clair et sombre, refaites par un script Playwright ; README qui compare les choix aux futurs dashboards Angular. | 2 | Fait |

**Tests :** 39 tests Vitest, dont la visionneuse sur le vrai module WebAssembly de topologie (invariants du tore, de la sphère et du ruban de Möbius, erreur de lib-c avec sa ligne) et en composant sans WebGL ; Playwright sur 5 navigateurs, dont la courbure lue au survol là où WebGL existe ; captures refaites par script. **Trouvé en route :** les erreurs de lib-c réduites à « format non reconnu » ; three.js chargé pour toutes les vues (désormais à la demande) ; un test de survol qui visait hors du tore sur un canevas large.

## Rétro (Charles)
- Ce qui a marché : Visionneuse 3D dans React sur le vrai WebAssembly, three.js chargé à la demande.
- Ce que l'IA a mal fait : Les erreurs de lib-c réduites à « format non reconnu » ; un test de survol qui visait hors du tore.
- À changer au prochain sprint : Garder le message d'erreur d'origine à travers les couches.
