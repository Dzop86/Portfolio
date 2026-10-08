# Sprint 45 : le RPG tactique, les règles du combat

**Objectif :** premier des six sprints du RPG tactique (D54) : les règles du combat dans une bibliothèque C# déterministe, partagée plus tard par le client Godot et le serveur, testée sans moteur.

**Goal:** first of the six tactical RPG sprints (D54): the combat rules in a deterministic C# library, later shared by the Godot client and the server, tested without an engine.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je déplace mon personnage sur un plateau en losanges (rpg) : grille isométrique (coordonnées de case, voisins, cases bloquantes), points de mouvement, chemin le plus court par A* qui ne dépasse pas les points restants, cases atteignables ; carte décrite en données (JSON) ; tests xUnit, CI Linux, Windows et macOS. | 2 | À faire |
| En tant que joueur, je lance des sorts au tour par tour (rpg) : points d'action, sorts décrits en données (coût, portée minimale et maximale, ligne de vue, dégâts), ligne de vue sur la grille (obstacles, cas des coins testés), ordre des tours par initiative, victoire et défaite. | 2 | À faire |
| En tant que joueur, j'affronte une IA et je peux revoir le combat (rpg) : adversaire qui s'approche et frappe au mieux de ses points ; combat enregistré (graine et actions) et rejoué à l'identique, refus des actions impossibles ; tests de rejeu sur des centaines de combats. | 1 | À faire |

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
