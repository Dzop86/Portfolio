# Sprint 45 : le RPG tactique, les règles du combat

**Objectif :** premier des six sprints du RPG tactique (D54) : les règles du combat dans une bibliothèque C# déterministe, partagée plus tard par le client Godot et le serveur, testée sans moteur.

**Goal:** first of the six tactical RPG sprints (D54): the combat rules in a deterministic C# library, later shared by the Godot client and the server, tested without an engine.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je déplace mon personnage sur un plateau en losanges (rpg) : grille isométrique (coordonnées de case, voisins, cases bloquantes), points de mouvement, chemin le plus court par A* qui ne dépasse pas les points restants, cases atteignables ; carte décrite en données (JSON) ; tests xUnit, CI Linux, Windows et macOS. | 2 | Fait |
| En tant que joueur, je lance des sorts au tour par tour (rpg) : points d'action, sorts décrits en données (coût, portée minimale et maximale, ligne de vue, dégâts), ligne de vue sur la grille (obstacles, cas des coins testés), ordre des tours par initiative, victoire et défaite. | 2 | Fait |
| En tant que joueur, j'affronte une IA et je peux revoir le combat (rpg) : adversaire qui s'approche et frappe au mieux de ses points ; combat enregistré (graine et actions) et rejoué à l'identique, refus des actions impossibles ; tests de rejeu sur des centaines de combats. | 1 | Fait |

**Résultat :** une partie de 41 actions s'enregistre en 1,2 Ko ; 1 000 combats d'IA se jouent en une seconde. À l'entraînement, le héros joué par l'IA gagne 69 % des combats ; dans le duel symétrique, celui qui joue en second gagne deux fois sur trois.

**Tests :** 58 tests xUnit des règles et 10 du simulateur (A* comparé à un parcours en largeur sur 200 plateaux au hasard ; ligne de vue identique dans les deux sens sur 3 000 paires et comparée au segment échantillonné ; projection isométrique et son inverse ; 600 combats d'IA sans action refusée ; 400 combats rejoués à l'identique après JSON ; 6 enregistrements invalides ; données incohérentes refusées ; équilibre dans une fourchette) ; deux combats de référence rejoués par la CI sur Linux, Windows et macOS ; analyseurs .NET, `dotnet format`. Vérifié en cassant le code : 13 mutations, 12 attrapées, une équivalente. **Trouvé en route :** un trou des tests (le lanceur qui se cachait la vue avec son propre corps, vu en cassant le code), l'entraînement gagné à 100 % (mesuré par simulation, réglé), un enregistrement deux fois trop gros et une graine que JavaScript aurait arrondie, un `grep -x` qui aurait échoué sous Windows.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
