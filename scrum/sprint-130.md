# Sprint 130 : Osmose : le moteur des glyphes

**Objectif :** la suite d'Osmose après S121, conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11) et validée par Charles (D79).

**Goal:** Osmose after S121, designed by the game designer (v7 of `projects/rpg/docs/suite-apres-d69.md`, §11) and approved by Charles (D79).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je vois des glyphes posés sur le plateau qui frappent qui y entre ou y commence son tour, et l'IA les évite et sait en poser (rpg) ; xUnit, rejeu par le serveur. | 5 | Fait |

## Fait
- `GlyphEffect` (sort seulement), `Fight.Glyphs`, déclenchement à l'entrée, à l'arrivée (poussée, attraction, bond, échange) et au début du tour ; une frappe par tour ; poisons, puis glyphes, puis passifs (T98).
- `Fight.PathTo` et `Pathfinding.FindPath` à péage : le chemin montré et marché contourne les glyphes si les PM le permettent.
- IA : `GlyphRisk` (contourner, sortir d'un glyphe), valeur d'un glyphe posé sous un ennemi.
- Client : fiche du sort, survol d'une case piégée, journal ; Godot : anneaux de l'élément sur le plateau, vérifiés par l'auto-test.
- Limite : aucun sort du jeu ne pose encore de glyphe (les Sylvecerfs, S132).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
