# Sprint 134 : Osmose : les deux donjons de la Forêt

**Objectif :** la suite d'Osmose après S121, conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11) et validée par Charles (D79).

**Goal:** Osmose after S121, designed by the game designer (v7 of `projects/rpg/docs/suite-apres-d69.md`, §11) and approved by Charles (D79).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je fais les deux donjons de la Forêt des Aïeux, avec leurs clefs chez le serrurier et leurs trois salles (rpg) ; xUnit, salles 2 et 3 mesurées. | 5 | Fait |

## Fait
- Les noms du scénariste : la Combe du brame (Sylvecerfs) et les Racines de l'Arbre-Mère (Gardiens moussus, sous l'arbre) ; T102.
- Portes sur les cartes des chefs, trois salles chacune, clefs chez le serrurier au niveau 40 et dans le butin (2 %, 10 % le chef).
- Salle 2 seul et nu : la Garde à 59 % aux Racines au niveau 40 ; la quête passe au niveau 41 (85 %), la salle n'est pas affaiblie. Salle 3 à trois et à quatre dans `PartyBalanceTests`.
- `dungeon_gen.py` garde les objets posés dans les salles (le sceau de la crypte n'est plus effacé).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
