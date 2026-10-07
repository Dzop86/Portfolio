# Sprint 30 : le dashboard React, données et résultats

**Objectif :** premier des deux sprints du projet react : un dashboard React et TypeScript qui lit une API JSON statique produite par le build du site (projets, sprints, résultats chiffrés), en français et en anglais, aux couleurs de la charte, publié avec le site.

**Goal:** first of the two sprints of the react project: a React and TypeScript dashboard reading a static JSON API produced by the site build (projects, sprints, measured results), in French and English, in the site's colours, published with the site.

| Story | Points | État |
|---|---|---|
| En tant que développeur front, je bâtis le dashboard React (react) : Vite, React 19, TypeScript strict ; API JSON statique produite par le build du site (`api/v1/`, que lira aussi le dashboard Angular) ; vue Projets (filtres par statut et technologie, avancement en points) et vue Sprints (vélocité, burndown) ; chargement et erreurs gérés ; FR/EN, thèmes clair et sombre de la charte ; Vitest et Testing Library ; publié sous `/dashboard/` ; CI. | 4 | Fait |
| En tant que visiteur, je lis les résultats chiffrés des projets dans le dashboard (react) : benchmarks de parallele (temps et accélérations), mesures de lib-c tirées de la base SQL (temps de lecture par taille et par format), précision du modèle ML ; graphiques SVG accessibles doublés d'un tableau ; sans défilement horizontal à 375 px. | 3 | Fait |

**Tests :** 28 tests Vitest (échelles, traductions, calculs, et le dashboard entier rendu sur l'API que le site construit vraiment) ; 6 tests de l'API côté site ; 75 tests Playwright du dashboard (3 vues, 2 langues, 2 thèmes, 5 navigateurs, axe, 375 px, cibles de 44 px) ; image Docker vérifiée. **Trouvé en route :** une série plate faisait boucler les graduations sans fin ; la vélocité plongeait à 0 au sprint ouvert ; les classes du modèle ML restaient en anglais en français ; le contexte Docker aurait embarqué les `node_modules` du dashboard.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
