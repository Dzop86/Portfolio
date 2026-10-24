# Sprint 138 : Osmose : les éléments composés, la Foudre

**Objectif :** l'arbre des talents commun aux classes, conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md` et `projects/rpg/docs/elements.md`) et validé par Charles (D80).

**Goal:** the talent tree shared by all classes, designed by the game designer (v3 of `projects/rpg/docs/talents-arbre.md` and `projects/rpg/docs/elements.md`) and approved by Charles (D80).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'ouvre la Foudre (Feu + Air) avec ma Sentinelle : les composés dans le moteur (parents, 2 × moyenne, résistances, couleurs, l'Arc), Ouverture, Transmutation et Conversion (rpg) ; xUnit, intégration API. | 5 | Fait |

## Fait
- `Element.Lightning` et la table des parents (`Elements`) : 2 × la moyenne des caractéristiques, la moyenne des résistances, la couleur jaune, le nom dans les deux langues (T104).
- Les 8 sorts de Foudre de la Sentinelle, jumeaux générés de ses sorts de Feu et d'Air (`tools/spells_gen.py`, 80 % du barème, noms de travail).
- Le grimoire calculé depuis les talents : Ouverture, Plein grimoire, Transmutation (Feu ou Air), Conversion (+15 %) ; un deck choisi avant suit ; le rang d'un jumeau est celui de son sort.
- L'Arc, la Signature appuyée, le Prisme ; l'IA vaut l'Arc.
- Défaut de conception corrigé : l'étage 2 de Tresses demande 1 point (sinon les builds du §4.5 étaient impossibles).
- Tests : `LightningTests` (Core, 11), `TalentTextsTests` (Client), `TalentTests` (API).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
