# Sprint 41 : la persistance étendue

**Objectif :** relecture de Charles (D50) : sur le tore debout, la boucle du graphe de Reeb naît à une selle et se ferme à l'autre, alors que le diagramme de persistance garde ses deux boucles jusqu'à l'infini. Les deux sont justes, mais regardent des choses différentes (les sous-niveaux, les lignes de niveau). Calculer la persistance étendue, qui apparie aussi les classes sans fin, et montrer sur la fiche la paire selle-selle de la boucle du graphe de Reeb.

**Goal:** Charles's review (D50): on the standing torus, the Reeb graph's loop is born at one saddle and closes at the other, while the persistence diagram keeps its two loops up to infinity. Both are right but look at different things (sublevel sets, level sets). Compute extended persistence, which pairs the classes that never die too, and show on the project page the saddle-to-saddle pair of the Reeb graph's loop.

| Story | Points | État |
|---|---|---|
| En tant que géomètre, j'obtiens la persistance étendue de la hauteur (topologie) : filtration montante des sous-niveaux, puis descendante des sur-niveaux (cône), paires ordinaires, étendues et relatives ; GoogleTest (sphère et tore debout : paires étendues min-max, selle-selle dans les deux sens, max-min ; dualités de Poincaré et de Lefschetz sur des surfaces bruitées : ordinaires et relatives, étendues de dimensions p et 2 − p, symétriques ; autant de paires étendues de dimension 1 au-dessus de la diagonale que de boucles du graphe de Reeb) ; vérifié en cassant le code ; mesuré ; API C et WebAssembly. | 3 | Fait |
| En tant que visiteur, je comprends pourquoi le tore garde deux boucles sans fin et où se ferme celle du graphe de Reeb (topologie) : diagramme étendu sur la fiche (ordinaires, étendues, relatives), la paire selle-selle reliée à la boucle du graphe de Reeb et à ses deux sommets sur le maillage, une note qui distingue sous-niveaux et lignes de niveau ; FR/EN, clavier, mobile ; Node et Playwright. | 2 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
