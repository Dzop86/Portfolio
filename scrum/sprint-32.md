# Sprint 32 : le dashboard Angular, comparé au React

**Objectif :** deux vues du dashboard (Projets et Résultats) en Angular, sur la même API JSON que le dashboard React, testées avec Jest et Cypress, et une comparaison mesurée des deux frameworks sur la fiche du projet.

**Goal:** two views of the dashboard (Projects and Results) in Angular, on the same JSON API as the React dashboard, tested with Jest and Cypress, and a measured comparison of both frameworks on the project page.

| Story | Points | État |
|---|---|---|
| En tant que développeur front, je refais deux vues du dashboard en Angular (angular) : Angular 22, composants autonomes à signaux, sans zone.js ; API chargée par un service injecté ; routeur ; Projets (filtres par état et technologie) et Résultats (calcul parallèle, lecture de maillages, modèle ML) ; FR/EN, thèmes de la charte ; Jest (unitaires, et intégration sur la vraie API) ; publié sous `/angular/` ; CI Linux, Windows et macOS. | 5 | Fait |
| En tant que recruteur, je compare les deux dashboards sur la fiche du projet (angular) : tests Cypress de bout en bout (vues, langues, thèmes, accessibilité avec axe, 375 px) ; tableau comparatif mesuré par un script (taille des paquets, temps de build, lignes de code, nombre de tests), en français et en anglais ; captures. | 3 | Fait |

**Tests :** 11 tests Jest (dont l'intégration sur l'API que construit le site), 20 tests Cypress (vues, langues, thèmes, axe, 375 px), tests unitaires du site sur les mesures et la fiche (tableau, liens, captures) ; captures refaites par script. **Trouvé en route :** le projet généré sans `strict` ; `<base href>` qui perdait la langue ; un menu lié avant ses options (« cylindre » affiché, tore tracé) ; `makeT` qui plantait sur une clé absente, dans les deux dashboards ; une première comparaison qui comptait three.js côté React.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
