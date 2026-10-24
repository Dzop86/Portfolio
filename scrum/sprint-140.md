# Sprint 140 : Osmose : la fenêtre « Ma route » et les réactions

**Objectif :** l'arbre des talents commun aux classes, conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md` et `projects/rpg/docs/elements.md`) et validé par Charles (D80).

**Goal:** the talent tree shared by all classes, designed by the game designer (v3 of `projects/rpg/docs/talents-arbre.md` and `projects/rpg/docs/elements.md`) and approved by Charles (D80).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je choisis mes talents dans une fenêtre à deux familles et sept branches ; les cinq autres réactions dont Électrisé, la portée en caractéristique, le prix du changement, une seule clé de Matière et deux éléments ajoutés au plus (rpg) ; xUnit, intégration API. | 5 | Fait |

## Fait
- Les cinq autres réactions et leurs talents (Transi, Fêlé, Électrisé, Embué, Aveuglé), `Stat.Range` et `Stat.Electrified` (T106).
- Deux éléments ouverts au plus ; une seule clé de Matière (depuis S137).
- Le prix du changement : rendre un talent coûte 10 × le niveau après 40, dans un village, hors combat ; annoncé dans l'écran.
- L'onglet « Ma route » : sept colonnes, deux familles, les tuiles par étage.
- Tests : `ReactionTests` (Core), `TalentTextsTests` (Client), `TalentTests` (API).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
