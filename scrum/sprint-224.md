# Sprint 224 : Osmose : la zone de 46 à 60, ses cartes

**Objectif :** Une cinquième région, de 46 à 60, après la Forêt des Aïeux (D102).

**Goal:** A fifth region, from 46 to 60, after the Forest of the Ancestors (D102).

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'explore une nouvelle zone de 46 à 60 : 25 cartes reliées au monde et ses ressources nouvelles (rpg) ; xUnit, cartes et ressources validées. | 5 | Fait |

## Fait
- Les Mines de Sous-Racine (`underroot`, « Underroot Mines ») : 25 cartes de [4, 6] à [8, 10], niveau 46, sous la Forêt des Aïeux ; on y descend par le milieu du fond de la forêt (`ancestors-2-4` → `underroot-2-0`), creusé dans la carte telle qu'elle est.
- Leur décor : à-pics de blocs de falaise (kit Nature, sans modèle nouveau), quelques arbres, charrettes et lanternes de mineurs, une mare une carte sur deux, bordures naturelles ; teinte de poussière de pierre, relief le plus fort du jeu ; l'auto-test Godot compte les à-pics.
- Dix ressources (mélèze, bois-racine, cobalt, or, ombre, poisson des grottes, orge, millet, arnica, gentiane), 50 près de l'entrée et 55 au fond, leurs tuiles ; une pierre de passage (102 Éclats) ; derrière « Quatre pistes ».
- `world_gen.py` n'écrit plus que les zones nommées (il réécrivait toujours le bois) ; photos des cartes refaites.
- Limite : ni monstres (S225, S226), ni donjons (S227), ni recettes (S228) ; noms des ressources non relus par le scénariste (T189).

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
