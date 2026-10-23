# Sprint 136 : Osmose : les quêtes de la Forêt, playtest

**Objectif :** la suite d'Osmose après S121, conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11) et validée par Charles (D79).

**Goal:** Osmose after S121, designed by the game designer (v7 of `projects/rpg/docs/suite-apres-d69.md`, §11) and approved by Charles (D79).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je suis les 9 quêtes de la Forêt des Aïeux jusqu'à la fin de la tranche ; textes relus par Charles ; playtest (rpg) ; xUnit, chaîne jouée par l'API. | 5 | Fait, playtest à faire |

## Fait
- Les textes du scénariste : `projects/rpg/docs/textes-v8.md` (à relire par Charles, questions au §7.2).
- Neuf quêtes au barème des zones de Clairval :
  - quatre principales après « Là où les routes se séparent » : Ceux de l'arbre (31, donnée par Anatole), Le passage qui n'existait pas (33), Les gardiens moussus (36), Les Racines de l'Arbre-Mère (41, rendue à Anatole) ;
  - trois secondaires : Le sol qui brûle (32), Des coins d'andouiller (34), Le brame (40) ;
  - deux fins de donjon : la Combe (44) et les Racines (45).
- Berthe, la contremaîtresse, et Bérold, le juge des serments, au camp des mineurs (`ancestors-1-1`), avec leurs dialogues.
- Cinq indices : trois sondages dans la forêt, la pierre d'écho et la dalle dans les grandes salles des deux donjons.
- Le Cœur gravé, butin de quête des gardiens (35 %, 100 % sur le menhir).
- Aucune étape principale ne demande plus que la salle 2 d'un donjon. Les clefs sont offertes une fois. La salle gagnée à plusieurs compte pour chaque membre dont la course est à cette salle (`Battles.cs`).
- Tests : `ForestQuestsTests` (Core : barème, chaîne, lieux, butin, toutes les quêtes atteintes d'ici le niveau 45) ; `ForestQuestsTests` (API : de « Là où les routes se séparent » aux gardiens, la clef des Racines).
- **Reste à faire :** la relecture des textes et le playtest de Charles.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
