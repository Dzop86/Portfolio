# Sprint 18 : l'Othello dans le navigateur

**Objectif :** sur la fiche du projet othello, le visiteur joue contre l'IA : le moteur et l'IA en C du sprint 17, compilés en WebAssembly, derrière un plateau utilisable à la souris, au toucher et au clavier.

**Goal:** on the othello project page, the visitor plays against the AI: the C engine and AI of sprint 17, compiled to WebAssembly, behind a board usable with the mouse, touch and keyboard.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je compile le moteur et l'IA (othello) en WebAssembly avec une API pour le navigateur (coups, IA, annulation) ; build commité vérifié par la CI, test Node (perft et partie complète identiques au natif). | 1 | Fait |
| En tant que visiteur, je joue à l'Othello (othello) sur la fiche : couleur et niveau au choix, coups possibles montrés, passes annoncées, annulation, score ; clavier (flèches, Entrée) et tactile ; tests Playwright, mobile et axe compris. | 2 | Fait |

**Tests :** 5 tests Node sur le WebAssembly, scénario Playwright dans 5 navigateurs et à 375 px, axe dans les deux thèmes. **Trouvé :** cases de 43,1 px sur un écran de 390 px.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
