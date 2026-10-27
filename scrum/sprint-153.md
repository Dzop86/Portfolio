# Sprint 153 : Osmose : le tableau des contrats

**Objectif :** la suite d'Osmose après S151, conçue par le game designer (v8 de `projects/rpg/docs/suite-apres-d69.md`, §12) et prise avec ses recommandations (D83).

**Goal:** Osmose after S151, designed by the game designer (v8 of `projects/rpg/docs/suite-apres-d69.md`, §12) and taken with its recommendations (D83).

| Story | Points | État |
|---|---|---|
| En tant que joueur, je trouve chaque jour 3 contrats (cumulables jusqu'à 9) au tableau d'avis de Clairval et de Quatre-Chemins : chasse, récolte, fabrication, donjon, horde, chef, tirés dans les zones que j'atteins, un de groupe au plus sur trois (rpg) ; xUnit, 1 000 tirages par niveau. | 3 | Fait |
| En tant que joueur, je rends un contrat contre un quart de niveau d'XP et 5 × niveau Éclats, je le suis dans l'onglet Contrats du journal et j'en change un gratuitement par jour (rpg) ; xUnit, intégration PostgreSQL, auto-test du village. | 2 | Fait, sauf l'onglet du journal (reporté) |

## Fait
- Le tableau d'avis sur les places de Clairval et de Quatre-Chemins, ses textes par le scénariste (`projects/rpg/docs/textes-contrats.md`).
- Trois contrats par jour et par personnage, gardés jusqu'à neuf, tirés dans les zones atteintes et les métiers du personnage, un de groupe sur trois au plus ; 1 000 tirages par niveau (1 à 45) tous faisables (xUnit).
- Rendus au tableau contre un quart de niveau d'XP et 5 × niveau Éclats (le double pour un groupe), un changement gratuit par jour (T120) ; intégration PostgreSQL : une chasse comptée par les combats, une récolte qui prend ses objets, les changements payés.
- Reporté : l'onglet « Contrats » du journal. À faire par Charles : le playtest des trois contrats au niveau 30.

## Rétro (Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
