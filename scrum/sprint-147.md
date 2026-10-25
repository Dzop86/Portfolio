# Sprint 147 : Osmose : l'arbre devient un graphe

**Objectif :** l'arbre des talents en vrai arbre (tronc, paires d'éléments, styles de jeu), d'après les réponses de Charles au questionnaire et les arbitrages du game designer (`projects/rpg/docs/talents-arbre-questions.md`, D82).

**Goal:** the talent tree as a real tree (trunk, element pairs, play styles), from Charles's answers to the questionnaire and the game designer's rulings (`projects/rpg/docs/talents-arbre-questions.md`, D82).

| Story | Points | État |
|---|---|---|
| En tant que joueur, mon arbre a un tronc, une fourche de 6 paires et d'Élément pur, une Fusion par paire et 6 styles partagés, et chaque refus me donne sa raison (rpg) ; xUnit en table, test de propriété, intégration. | 3 | Fait |
| En tant que joueur, 9 builds de référence garantissent qu'aucun style, aucune paire et aucun couple n'est une fausse décision (rpg) ; mesures d'équilibre. | 2 | Fait |

## Fait
- L'arbre en graphe (T114) : la fourche de sept portes (six paires, l'Élément pur et ses quatre Cœurs), la tresse de Foudre, six styles partagés (Alchimie, Mêlée, Distance, Terrain, Tempo, Soutien), le parent OU, les clés au niveau 45, une clé par style, une seule grande bascule.
- Chaque refus dit sa raison avant le clic, en FR et en EN ; rendre un talent annonce puis rend ce qui ne tenait que par lui.
- L'arbre en version 3 : les talents de la version 2 rendus une fois, gratuitement.
- Les mesures : styles, paires et couples à ±1,5 victoire à 45 ; 9 builds de référence (Matière, Manière, pur) sous le plafond des hordes ; 10 000 arbres tirés au hasard.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
