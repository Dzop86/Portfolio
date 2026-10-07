# Sprint 22 : le morpion, en Python

**Objectif :** le projet morpion réécrit un jeu de mes études : le morpion contre une IA qui ne perd jamais (minimax), en Python, jouable dans le terminal puis sur la fiche du projet ; les tests prouvent que l'IA est imbattable en essayant toutes les parties possibles.

**Goal:** the morpion project rewrites a game from my studies: tic-tac-toe against an AI that never loses (minimax), in Python, playable in the terminal then on the project page; the tests prove the AI unbeatable by trying every possible game.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je joue au morpion (morpion) dans le terminal contre l'IA, avec les croix ou les ronds, au niveau débutant ou imbattable ; règles et minimax en Python typé ; tests pytest (nombre de positions et de parties connus, IA jamais battue sur toutes les parties possibles), CI Linux, Windows et macOS. | 2 | Fait |
| En tant que visiteur, je joue au morpion (morpion) sur la fiche du projet : les coups de l'IA viennent d'un livre de coups calculé par le programme Python, commité et vérifié par la CI ; tests Node et Playwright. | 1 | Fait |

**Tests :** 36 tests pytest (dont toutes les parties possibles contre l'IA, en X et en O), mypy strict ; 5 tests Node sur les règles JavaScript et le livre de coups ; un scénario Playwright (partie complète au clavier, axe). **Trouvé par Playwright :** les flèches partaient de la dernière case cliquée, pas de celle qui avait le focus.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
