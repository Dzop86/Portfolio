# Sprint 5 : voir la topologie dans le navigateur

**Objectif :** sur la fiche Topologie 3D, un visiteur fait tourner un maillage coloré par sa courbure et lit ses invariants, calculés par le code C++ compilé en WebAssembly.

**Goal:** on the 3D topology page, a visitor turns a mesh coloured by its curvature and reads its invariants, computed by the C++ code compiled to WebAssembly.

| Story | Points | État |
|---|---|---|
| En tant que développeur, j'appelle la bibliothèque C++ de topologie depuis JavaScript : build WebAssembly vérifié en CI, testé dans Node. | 2 | Fait |
| En tant que recruteur, je fais tourner un maillage (topologie) coloré par sa courbure de Gauss, avec ses invariants, sur des exemples ou mon propre fichier. | 3 | Fait |

**Tests :** site 157 unitaires et d'intégration, 210 end-to-end (5 navigateurs, deux thèmes, viewer compris, sans WebGL sous Firefox) ; topologie 27 tests GoogleTest.


## Rétro (Charles)
- Ce qui a marché : Le C++ tourne dans le navigateur, testé dans Node, et la visionneuse three.js montre la courbure.
- Ce que l'IA a mal fait : Les bords de la selle apparaissaient en rouge, l'échelle de couleurs était inutilisable sur les modèles sculptés : vus sur capture, pas par les tests.
- À changer au prochain sprint : Regarder chaque figure sur capture avant de livrer.
