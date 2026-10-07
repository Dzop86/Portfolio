# Sprint 15 : un mini-cours sur les cartes généralisées

**Objectif :** le projet gcartes explique les cartes généralisées, sujet de ma thèse : une petite bibliothèque construit et vérifie des G-cartes de dimension 2, et la fiche du projet devient un cours court, avec un cube déplié dont on parcourt les brins, les liaisons et les orbites, puis un quiz.

**Goal:** the gcartes project explains generalised maps, the subject of my thesis: a small library builds and checks 2D G-maps, and the project page becomes a short course, with an unfolded cube whose darts, links and orbits can be walked, then a quiz.

| Story | Points | État |
|---|---|---|
| En tant que chercheur, je construis des G-cartes de dimension 2 (gcartes) à partir de faces, je vérifie leurs contraintes, et j'en calcule orbites, cellules, caractéristique d'Euler, bords et orientabilité ; tests node:test sur le cube, le tore et le ruban de Möbius, CI sur trois systèmes. | 3 | Fait |
| En tant qu'étudiant, je suis le mini-cours (gcartes) : leçons en français et en anglais, cube déplié interactif (brins, α0, α1, α2, orbites, comptage des cellules), quiz corrigé ; tests Playwright, clavier et mobile compris. | 2 | Fait |

**Tests :** bibliothèque, 10 tests node:test (cube, tores, cylindres, Möbius, patron du cours) dans `npm test` sur 3 OS ; cours, test de site et Playwright dans 5 navigateurs (axe compris).

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
