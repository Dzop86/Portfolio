# Sprint 142 : Osmose : la branche Terrain et la Rémanence

**Objectif :** l'arbre des talents commun aux classes, conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md` et `projects/rpg/docs/elements.md`) et validé par Charles (D80).

**Goal:** the talent tree shared by all classes, designed by the game designer (v3 of `projects/rpg/docs/talents-arbre.md` and `projects/rpg/docs/elements.md`) and approved by Charles (D80).

| Story | Points | État |
|---|---|---|
| En tant que joueur, un sort lancé dans le vide pose une glyphe en losange dont les dégâts baissent loin du centre (Rémanence), avec les autres talents de Terrain (rpg) ; xUnit, intégration API. | 4 | Fait |

## Fait
- La Rémanence : le glyphe d'un sort qui ne touche personne, son centre et sa décroissance, un seul par lanceur (T108).
- Les autres talents de Terrain : Pas de côté, Adossé, Relais, Sceau (glyphe qui marque), Leurre (invocation qui attire les monstres), Terre brûlée, Nomade, Écho ; les sorts de l'arbre générés.
- Le client : la couleur d'un glyphe vient de l'événement, le nom d'une Rémanence, l'écho dans le journal.
- Tests : `TerrainTests` (10), `TalentTextsTests`, `TalentTests` (API : un Sceau dans le deck avec son talent seulement, combat rejoué).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
