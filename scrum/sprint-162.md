# Sprint 162 : Osmose : le rendement des éléments

**Objectif :** un héros d'un seul élément ne vide plus les hordes seul, avant que les spécialisations ne se règlent dessus (D85, décision du game designer dans `projects/rpg/docs/suite-apres-d69.md`, §12).

**Goal:** a hero of a single element no longer clears hordes alone, before the specialisations are tuned on top of it (D85, the game designer's decision in `projects/rpg/docs/suite-apres-d69.md`, §12).

| Story | Points | État |
|---|---|---|
| En tant que joueur, mes points d'élément rapportent 2 de bonus par 10 points jusqu'à 3 × mon niveau + 5, puis 1 par 20 points au-delà : un héros d'un seul élément gagne au plus 3 hordes solides sur 10 à 50 et à 60, avec chaque kit, et le héros de T41 retrouve le plafond de 2 avec le kit « craft » (rpg) ; xUnit, relevé de combat en version 4, mesures. | 2 | Fait |
| En tant que joueur, la fiche et l'éditeur de points montrent la valeur efficace de chaque élément ; les builds de l'arbre, les grandes salles des donjons et le jeu à plusieurs sont remesurés (rpg) ; xUnit, auto-tests du client. | 2 | Fait |
| En tant que joueur de Foudre, l'Arc saute avec le jet du coup, sans le bonus d'élément : un build de Matière de l'arbre ne vide pas plus de 3 hordes solides sur 10 (rpg) ; xUnit, mesures de l'arbre à 35, 40 et 45. | 1 | Fait |

## Ce qui est fait
- Au-delà de 3 × niveau + 5, les points d'un élément d'un héros comptent un quart (1 de bonus pour 20 points) ; le héros mixte équipé n'est touché à aucun niveau ; relevé de combat en version 4 (une version 3 ou moins se rejoue avec le bonus linéaire) ; la fiche et l'éditeur montrent « 245 (177) » et la règle au survol ; T129.
- Mesuré pas à pas avec le game designer (`projects/rpg/docs/suite-apres-d69.md` §12) : Éboulis passe à 6 PA (13-17), l'Arc d'un héros saute sans le bonus d'élément ; les retouches de Tir de recul et de Taillade sont annulées (sans effet, ou planchers cassés au niveau 1).
- Le héros d'un seul élément bat au plus 2,5 hordes solides sur 10 à 50 et 60 ; les builds de l'arbre de 35 à 45, 0,8 au plus ; la grande salle de la Combe, mesurée sur 300 combats, à 75 % pour la Sentinelle. Les hordes de 6 petits monstres sans chef tombent encore à un héros pur bien équipé : les Mines auront un monstre solide dans chaque horde.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
