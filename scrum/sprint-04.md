# Sprint 4 : la topologie des maillages en C++

**Objectif :** `projects/topologie/` calcule en C++ les invariants d'un maillage (composantes, bords, orientabilité, genre) et sa courbure discrète, en réutilisant le lecteur de lib-c.

**Goal:** `projects/topologie/` computes in C++ the invariants of a mesh (components, boundaries, orientability, genus) and its discrete curvature, reusing lib-c's reader.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je construis une structure demi-arête (topologie) à partir d'un maillage lu par lib-c, testée avec GoogleTest sur Linux, Windows et macOS. | 3 | Fait |
| En tant que chercheur, j'obtiens les invariants topologiques d'un maillage (topologie) : composantes connexes, boucles de bord, orientabilité, variété ou non, genre. | 3 | Fait |
| En tant que chercheur, j'obtiens la courbure de Gauss discrète (topologie) par défaut angulaire, vérifiée par le théorème de Gauss-Bonnet. | 2 | Fait |

**Tests :** topologie 25 tests GoogleTest (Linux, Windows, macOS, ASan + UBSan), dont Gauss-Bonnet sur 20 tores déformés au hasard.

**Reporté au sprint 5 :** viewer Three.js de la topologie.


## Rétro (Charles)
- Ce qui a marché : La structure demi-arête et les invariants sont vérifiés par Gauss-Bonnet sur 20 tores déformés au hasard.
- Ce que l'IA a mal fait : La ligne d'erreur de lib-c était lue avant l'appel (ordre d'évaluation non spécifié en C++), et la courbure par aire barycentrique faisait des taches : attrapés par les tests.
- À changer au prochain sprint : Garder un théorème comme oracle pour chaque calcul géométrique.
