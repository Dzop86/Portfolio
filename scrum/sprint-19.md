# Sprint 19 : une bataille navale en Java et JavaFX

**Objectif :** le projet naval réécrit un jeu de mes études : une bataille navale contre l'ordinateur, dont le modèle est en Java pur et testé, et l'interface en JavaFX ; la fiche du projet en montre des captures, générées sans écran par l'application elle-même.

**Goal:** the naval project rewrites a game from my studies: battleship against the computer, with its model in plain, tested Java and its interface in JavaFX; the project page shows screenshots, made headless by the application itself.

| Story | Points | État |
|---|---|---|
| En tant que joueur, j'affronte l'ordinateur à la bataille navale (naval) : grille de 10 × 10, flotte de 5 navires placée au hasard sans contact, tirs, touché, coulé, fin de partie ; IA qui chasse puis cible ; tests JUnit 5, CI Linux, Windows et macOS. | 3 | Fait |
| En tant que joueur, je joue dans une fenêtre JavaFX (naval) : mes navires et les tirs adverses, la grille ennemie où je clique, messages de partie ; captures d'écran générées sans écran pour la fiche du projet ; build Maven sur les trois systèmes. | 2 | Fait |

**Tests :** modèle 8 tests JUnit (dont 2 000 flottes et 300 parties de l'ordinateur, moins de 60 tirs en moyenne), interface 3 tests sans écran ; fiche vérifiée par axe.

## Rétro (Charles)
- Ce qui a marché : Bataille navale testée sur 2 000 flottes et 300 parties, captures générées sans écran.
- Ce que l'IA a mal fait : Rien de notable.
- À changer au prochain sprint : L'aventure en Java, jouable aussi dans le navigateur.
