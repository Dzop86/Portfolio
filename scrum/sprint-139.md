# Sprint 139 : Osmose : la Foudre pour toutes les classes

**Objectif :** l'arbre des talents commun aux classes, conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md` et `projects/rpg/docs/elements.md`) et validé par Charles (D80).

**Goal:** the talent tree shared by all classes, designed by the game designer (v3 of `projects/rpg/docs/talents-arbre.md` and `projects/rpg/docs/elements.md`) and approved by Charles (D80).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'ouvre la Foudre avec la Garde et la Mage, avec ses textes en français et en anglais et les sorts de base qui portaient son nom renommés (rpg) ; xUnit. | 3 | Fait, textes à relire par Charles |

## Fait
- Les 8 jumeaux de Foudre de la Garde et de la Mage (`tools/spells_gen.py`), noms de travail (T105).
- Renommés : l'« Éclair » de la Mage (Air) devient « Trait de vent », la « Cyclone » de la Garde « Lame cyclone », le « Grain orageux » de la Sentinelle.
- `docs/textes-v9.md` : les 24 noms de Foudre, les talents et la réaction, à relire par Charles (§4, deux questions).
- Tests : `LightningTests` (8 jumeaux par classe, noms uniques, Ouverture et conversion de la Garde et de la Mage).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
