# Sprint 4 : la topologie des maillages en C++

**Objectif :** `projects/topologie/` calcule en C++ les invariants d'un maillage (composantes, bords, orientabilité, genre) et sa courbure discrète, en réutilisant le lecteur de lib-c.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je construis une structure demi-arête (topologie) à partir d'un maillage lu par lib-c, testée avec GoogleTest sur Linux, Windows et macOS. | 3 | Fait |
| En tant que chercheur, j'obtiens les invariants topologiques d'un maillage (topologie) : composantes connexes, boucles de bord, orientabilité, variété ou non, genre. | 3 | Fait |
| En tant que chercheur, j'obtiens la courbure de Gauss discrète (topologie) par défaut angulaire, vérifiée par le théorème de Gauss-Bonnet. | 2 | Fait |

**Tests :** topologie 25 tests GoogleTest (Linux, Windows, macOS, ASan + UBSan), dont Gauss-Bonnet sur 20 tores déformés au hasard.

**Reporté au sprint 5 :** viewer Three.js de la topologie.

**Reporté du sprint 3 :** miroir GitLab de lib-c, en attente du compte et du jeton de Charles.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
