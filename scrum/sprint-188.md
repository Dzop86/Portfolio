# Sprint 188 : Osmose : douze sorts par trio

**Objectif :** les composés consomment leurs parents, sur la décision de Charles du 2 novembre 2026 (D94).

**Goal:** compounds consume their parents, on Charles's decision of 2 November 2026 (D94).

| Story | Points | État |
|---|---|---|
| En tant que joueur de niveau 50, un trio me donne 12 sorts, jumeaux de tous les sorts de ses trois parents, pour un livre qui reste à 24 sorts : 96 sorts de plus, leurs noms et leurs icônes (rpg) ; xUnit : budget × 0,75, signature, rang partagé, grimoire de 24 sorts avec un trio. | 3 | Fait |
| En tant que Charles, chaque trio reste dans les bornes à 50 et 60 (rpg) ; mesure hors CI dans `docs/mesures-arbre.md`. | 1 | Mesuré (v4 bis) |

**Ce qui est fait.** Un trio donne les 12 sorts de ses trois parents, jumeaux au budget × 0,75 : 144 sorts de trios au lieu de 48, et un livre qui reste à 24 sorts.

**Ce qui a bougé.** Les fichiers `.import` des icônes sont écrits par `tools/spell_art.py --imports`, sans Godot en local : l'import réel se fait en CI. Noms et silhouettes restent de travail, à relire par le scénariste.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
