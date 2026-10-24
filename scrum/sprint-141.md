# Sprint 141 : Osmose : les branches Distance et Rythme

**Objectif :** l'arbre des talents commun aux classes, conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md` et `projects/rpg/docs/elements.md`) et validé par Charles (D80).

**Goal:** the talent tree shared by all classes, designed by the game designer (v3 of `projects/rpg/docs/talents-arbre.md` and `projects/rpg/docs/elements.md`) and approved by Charles (D80).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je prends des talents de Distance et de Rythme, que l'IA estime avec une seule fonction (rpg) ; xUnit. | 3 | Fait |

## Fait
- Les 8 talents de Distance qui restaient et les 9 de Rythme, compilés ou vivants, et les points d'un tour (T107).
- Une seule estimation des coups dans l'IA (`Fight.Expected`), qui lit les talents vivants depuis la case d'où elle lancerait ; son alignement sur la vraie règle des dégâts déplace huit mesures d'équilibre : reporté aux mesures de S144.
- Tests : `DistanceRhythmTests` (9).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
