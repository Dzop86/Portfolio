# Sprint 143 : Osmose : la branche Soutien

**Objectif :** l'arbre des talents commun aux classes, conçu par le game designer (v3 de `projects/rpg/docs/talents-arbre.md` et `projects/rpg/docs/elements.md`) et validé par Charles (D80).

**Goal:** the talent tree shared by all classes, designed by the game designer (v3 of `projects/rpg/docs/talents-arbre.md` and `projects/rpg/docs/elements.md`) and approved by Charles (D80).

| Story | Points | État |
|---|---|---|
| En tant que joueur, mes sorts lancés sur un allié l'aident selon leur élément (Feu soigne, Terre protège, Eau résiste, Air déplace), avec leurs plafonds, l'IA qui soutient et les huit talents (rpg) ; xUnit, intégration à deux clients. | 4 | Fait |

## Fait
- Main tendue et la table par élément, la paire à 60 %, le plafond de 15 % du soin, un soutien par tour, jamais sur soi (T109).
- Bannière, Double main, Élan partagé, Longue main, Dévouement, Harmonie, Relève ; l'IA qui soutient ; la prévision d'un soutien.
- Tests : `SupportTests` (12), `BattleTests` (deux joueurs avec leurs talents).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
