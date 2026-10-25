# Sprint 151 : Osmose : planifier, partager, playtest de l'arbre

**Objectif :** l'arbre des talents en vrai arbre (tronc, paires d'éléments, styles de jeu), d'après les réponses de Charles au questionnaire et les arbitrages du game designer (`projects/rpg/docs/talents-arbre-questions.md`, D82).

**Goal:** the talent tree as a real tree (trunk, element pairs, play styles), from Charles's answers to the questionnaire and the game designer's rulings (`projects/rpg/docs/talents-arbre-questions.md`, D82).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je planifie un build de niveau 60, je charge un build suggéré et je partage mon build par un code (rpg) ; xUnit, intégration. | 2 | Fait |
| En tant que Charles, un outil hors CI mesure les 18 combinaisons de paire et de classe et la règle des 5 %, et un joueur extérieur passe le critère des deux minutes (rpg). | 2 | Mesure faite ; playtest extérieur à faire par Charles |

## Fait
- Le chemin d'un nœud au survol et son coût, la planification au niveau 60 et ses fantômes jamais enregistrés, les builds suggérés en brouillon, le code de build, la pastille des points à dépenser (T118).
- `Rpg.Sim --talents` : les 18 combinaisons de classe et de paire sur 100 combats, avec des builds tirés comme le ferait un joueur, et la règle des 5 % ; rapport versionné dans `projects/rpg/docs/mesures-arbre.md` (Électrisation devant contre les hordes, à voir avec Charles).
- À faire par Charles : le playtest extérieur (un joueur explique son build en une phrase après deux minutes), noté dans `REVIEW.md`.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
