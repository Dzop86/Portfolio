# Sprint 133 : Osmose : les Gardiens moussus

**Objectif :** la suite d'Osmose après S121, conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11) et validée par Charles (D79).

**Goal:** Osmose after S121, designed by the game designer (v7 of `projects/rpg/docs/suite-apres-d69.md`, §11) and approved by Charles (D79).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'affronte les Gardiens moussus, leur chef, leur butin et leur panoplie (rpg) ; xUnit, T41 et `--party`. | 3 | Fait |

## Fait
- Conception du game designer (`docs/familles-foret.md` §4), 5 textes du scénariste ; T101.
- 12 sorts (3 glyphes alliés), 2 passifs (Cairn, Cœur gravé), l'invocation Caillou éveillé ; la famille `golem` (34 à 37, chef 45) ; le Cercle de pierres, partagé avec les cerfs ; le dessin du gardien ; la panoplie de 4 pièces.
- T41 à 31 : gardiens aux PV des gabarits ; chef ≤ 10 % seul, une horde de 10 et le chef dans `PartyBalanceTests`.
- CI Godot : un combat de chaque chef de la forêt, glyphes contrôlés.
- Limite : l'IA ne cherche pas un cercle allié pour s'y arrêter (elle le pose autour d'elle).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
