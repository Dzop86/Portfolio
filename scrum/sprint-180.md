# Sprint 180 : Osmose : les bordures naturelles

**Objectif :** les retours de Charles sur la 1.0.157 (30 octobre), avant les spécialisations : le passage d'une carte à l'autre n'est plus marqué par un mur d'arbres (D87 ; `projects/rpg/docs/graphisme/s165-retours-30-oct.md`).

**Goal:** Charles's feedback on 1.0.157 (30 October), before the specialisations: moving from one map to the next is no longer marked by a wall of trees (D87; `projects/rpg/docs/graphisme/s165-retours-30-oct.md`).

| Story | Points | État |
|---|---|---|
| En tant que joueur, les cartes ont des bordures irrégulières, et deux cartes d'une même zone s'ouvrent l'une sur l'autre, les cités gardant leur contour (rpg) ; xUnit sur `world_gen.py` et les données régénérées, PNJ, groupes, récoltes et sorties à leur place. | 4 | Fait (T140) |

## Ce qui est fait
- `tools/border_soften.py` sur les 125 cartes des cinq zones : côtés internes ouverts sur 56 à 92 % du côté (le lac moins, là où l'eau barre), jamais plus de 8 obstacles d'affilée ; bande irrégulière de 1 à 3 cases, mêlée selon le style ; le lac continue jusqu'au bord ; rien de posé n'est touché, tout reste accessible ; cités et donjons intacts ; idempotent.
- xUnit `BorderTests` (5 tests), `WorldTests` et empreintes des groupes mis à jour ; photos de l'atlas refaites ; auto-test du client vert (141 cartes, 383 groupes).
- Limite : les groupes et les points de récolte changent de case (tirés parmi les cases accessibles), avec les mêmes identifiants.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
