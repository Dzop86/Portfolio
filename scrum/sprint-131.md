# Sprint 131 : Osmose : la Forêt des Aïeux

**Objectif :** la suite d'Osmose après S121, conçue par le game designer (v7 de `projects/rpg/docs/suite-apres-d69.md`, §11) et validée par Charles (D79).

**Goal:** Osmose after S121, designed by the game designer (v7 of `projects/rpg/docs/suite-apres-d69.md`, §11) and approved by Charles (D79).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'explore la Forêt des Aïeux, 25 cartes et 10 ressources nouvelles (rpg) ; xUnit, cartes et ressources validées. | 5 | Fait |

## Fait
- `tools/world_gen.py` : la zone `ancestors`, 25 cartes de [4, 1] à [8, 5], niveau 31, son style (futaies, rochers en amas, fougères, sources) ; `tools/hamlet.py` : la route du sud traverse la palissade de la maison des routes (T99).
- Godot : un arbre sur trois en tronc géant, les rochers coiffés de mousse, la teinte d'herbe de la forêt (`ZoneStyle`), sans modèle nouveau ; l'auto-test compte les géants.
- Dix ressources (charme, orme, granit, malachite, truite, silure, épeautre, sorgho, sauge, mandragore), leurs points (`Harvesting.Tiers` : 40 près de l'entrée, 45 au fond) et leurs dessins (`credits.json`).
- La pierre de passage de la forêt (72 Éclats) ; ses cartes derrière « Quatre pistes », comme Quatre-Chemins.
- Corrigés au passage : `icons_gen.py` (le parchemin de rappel manquait), `trades_gen.py` (l'ordre des métiers).
- Limite : ni monstres (S132, S133) ni recettes (S135) encore ; noms des ressources non relus par le scénariste.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
