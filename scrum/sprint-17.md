# Sprint 17 : un Othello en C, de ses règles à son IA

**Objectif :** le projet othello réécrit un jeu de mes études : un moteur en C, exact et rapide (plateau en deux entiers de 64 bits), validé en dénombrant les parties possibles, et une IA minimax alpha-bêta, jouables dans le terminal ; le sprint 18 le mettra dans le navigateur.

**Goal:** the othello project rewrites a game from my studies: an exact and fast engine in C (the board in two 64-bit integers), validated by counting the possible games, and an alpha-beta minimax AI, playable in the terminal; sprint 18 will bring it to the browser.

| Story | Points | État |
|---|---|---|
| En tant que joueur, je joue à l'Othello (othello) dans le terminal : coups légaux, retournements, passes, fin de partie et score ; moteur en bitboards validé par perft (nombres de positions connus), tests Unity, CI Linux, Windows et macOS, ASan et UBSan. | 3 | Fait |
| En tant que joueur, j'affronte une IA (othello) : minimax alpha-bêta à profondeur réglable, évaluation par coins, mobilité et positions ; tests : alpha-bêta donne la valeur du minimax, l'IA bat un joueur aléatoire. | 2 | Fait |

**Tests :** 8 tests du plateau (perft jusqu'à 8 coups), 6 tests de l'IA, 3 tests de la partie en terminal ; ASan et UBSan.

**Prévu au sprint 18 (3 points) :** moteur et IA en WebAssembly, plateau jouable sur la fiche du projet (clavier et tactile), tests Node et Playwright.

## Rétro (Charles)
- Ce qui a marché : Moteur d'Othello en bitboards validé par perft jusqu'à 8 coups, IA alpha-bêta testée.
- Ce que l'IA a mal fait : Rien de notable.
- À changer au prochain sprint : Le mettre dans le navigateur.
