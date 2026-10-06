# gcartes : un mini-cours sur les cartes généralisées

Les cartes généralisées (G-cartes) sont la structure de ma thèse : un objet est découpé en **brins**, reliés par des **involutions** α0, α1, α2 ; sommets, arêtes et faces ne sont pas stockés, ce sont des **orbites**. Ce projet en donne une petite bibliothèque JavaScript, et la fiche du projet en fait un cours court, avec un cube déplié interactif et un quiz.

*Generalized maps of dimension 2 in JavaScript (darts, involutions, orbits, Euler characteristic, boundary, orientability), and a short interactive course with a quiz. Written from the textbook definitions; no laboratory code.*

## La bibliothèque (`src/gmap.js`)
- `GMap` : brins, `link(i, a, b)` (refuse un brin déjà lié), `check()` (chaque αi est une involution, et α0α2α0α2 se referme), `orbit(d, dims)`, `cells(dims)`.
- En dimension 2 : sommet = orbite ⟨α1, α2⟩, arête = ⟨α0, α2⟩, face = ⟨α0, α1⟩, composante connexe = ⟨α0, α1, α2⟩.
- `invariants()` : nombre de brins et de cellules, caractéristique d'Euler χ = S − A + F, boucles de bord (brins 2-libres, reliés par α0 et en tournant autour du sommet), orientabilité (deux couleurs de brins, chaque liaison change de couleur), genre (χ = 2c − 2g − b, ou χ = 2c − g − b en non orientable).
- `fromFaces(faces)` : G-carte d'une surface donnée par ses faces ; chaque côté donne deux brins (α0), les côtés consécutifs se rejoignent au sommet (α1), deux faces qui partagent un côté sont cousues (α2). Formes prêtes : `CUBE_FACES`, `gridFaces` (tore ou cylindre), `moebiusFaces`.

## Le cours
La [fiche du projet](https://dzop86.github.io/Portfolio/fr/project-gcartes.html) présente la décomposition de deux carrés en G-carte, étape par étape (l'objet, puis les coupes selon α2, α1 et α0, `src/decompose.js`), six leçons (brins, liaisons α0, α1, α2, contrainte de cohérence, orbites, comptage et orientabilité, intérêt pour le calcul parallèle), le cube déplié avec ses 48 brins (cliquer un brin, se déplacer par α0, α1, α2, afficher son sommet, son arête, sa face ou toute la composante) et un quiz de six questions corrigé avec explications. Textes : `course.json` ; figure : `src/net.js`. Choix : D24 dans le [`DECISIONS.md` du site](../../DECISIONS.md).

## Tests
`tests/gmap.test.mjs`, lancés par `npm test` (CI sur Linux, Windows et macOS, Node 22 et 24) : un carré (8 brins, un bord), le cube (48 brins, 8 sommets, 12 arêtes, 6 faces, χ = 2, chaque orbite de sommet de 6 brins tous au même sommet), des tores (genre 1) et des cylindres (deux bords) de plusieurs tailles, le ruban de Möbius (non orientable, un seul bord), deux cubes (deux composantes), les contraintes violées par une carte faite à la main. Le patron du cube du cours est vérifié aussi (G-carte valide, χ = 2, chaque liaison relie les bons sommets, chaque brin dessiné dans son carré). Côté site, un test vérifie leçons, brins et questions dans les deux langues, et Playwright utilise la figure et le quiz dans 5 navigateurs. Vérifié en cassant le code : si l'orientabilité répond toujours oui, ou si les bords ne sont pas reliés autour des sommets, les tests échouent.

## Limites
- Dimension 2 seulement (surfaces) ; les G-cartes de la thèse vont jusqu'à la dimension 3 et au-delà.
- Les faces sont des polygones simples ; un côté partagé par plus de deux faces est refusé (ce n'est plus une surface).

Écrit de zéro à partir des définitions publiées (Lienhardt, 1994) ; aucun code ni aucune donnée du laboratoire. Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
