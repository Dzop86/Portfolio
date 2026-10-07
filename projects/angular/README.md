# angular : deux vues du dashboard en Angular, comparées au React

[![angular](https://github.com/Dzop86/Portfolio/actions/workflows/angular.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/angular.yml)

Deux vues du [dashboard React](../react/) (Projets et Résultats) refaites en **Angular 22** sur la même API JSON statique du site (`api/v1/`, D43), publiées sous [`/angular/`](https://dzop86.github.io/Portfolio/angular/). Les types de l'API, les calculs des vues, les échelles des graphiques, les textes et les styles sont ceux du dashboard React, importés tels quels : seul le framework change, et la [fiche du projet](https://dzop86.github.io/Portfolio/fr/project-angular.html) compare les deux sur des mesures.

*Two views of the React dashboard rebuilt in Angular 22 (standalone zoneless components, signals, httpResource, router) on the same static JSON API, sharing types, computations, texts and styles with React; Jest and Cypress tests; a measured comparison of both frameworks on the project page.*

## Organisation
- `src/app/shared.ts` : ce qui vient du dashboard React (`projects/react/src` : `api.ts`, `model.ts`, `charts/scale.ts`, `i18n.ts`) ; `src/styles.css` importe `tokens.css` du site et les styles du React.
- `src/app/context.ts` : langue (`?lang=fr|en`, sinon celle du navigateur), adresse de l'API et racine du site en jetons d'injection ; thème partagé avec le site (même clé `theme`).
- `src/app/app.ts`, `app.routes.ts` : cadre, onglets (`routerLinkActive`, `aria-current`), routeur à fragment (`#/results`, pas de réécriture sur GitHub Pages), pages chargées à la demande.
- `src/app/pages/` : Projets et Résultats, un `httpResource` par fichier de l'API (chargement, erreur, rechargement), état en signaux (`signal`, `computed`, `linkedSignal`) ; `load-state.ts` : chargement et erreur.
- `src/app/charts/` : barres et courbes en SVG, mêmes échelles et même dessin que le React.
- `scripts/compare.mjs` : mesures React contre Angular, écrites dans `data/comparison.json` et lues par la fiche.

## Lancer
Node 22.22.3 ou plus récent (Angular 22). TypeScript 6.0 ici : Angular 22 n'accepte pas encore TypeScript 7, que prennent le site et le dashboard React.
```sh
npm ci
npm run typecheck && npm test   # types stricts (gabarits compris au build), Jest
npm run build                   # dist/, copié dans angular/ par le build du site
npm start                       # http://localhost:4200/?lang=fr, l'API relayée vers le site servi sur :4173
npm run e2e                     # Cypress, sur le site servi (npm run build et npm run serve à la racine)
node scripts/compare.mjs        # depuis la racine du dépôt : refait data/comparison.json
```

## Tests
- **Jest** (jest-preset-angular, sans zone.js, 11 tests) : graphiques (largeur des barres, graduations logarithmiques, étiquettes en bout de ligne), cadre (langue, liens, thème), routeur (vues, page par défaut, chemin inconnu) ; **intégration** : les pages rendues par TestBed sur l'API que le site construit vraiment (`tests/global-setup.mjs` appelle `src/api.mjs`), chaque requête servie par le contrôleur HTTP de test d'Angular : filtres, menus, erreur 503 puis Réessayer, formats de nombres.
- **Cypress** (`cypress/e2e/dashboard.cy.ts`, 20 tests) : chaque vue en français et en anglais, thèmes clair et sombre, à 1 280 et 375 px de large, avec axe (WCAG 2 AA, contrastes compris ; commande `cy.checkA11y` maison, cypress-axe n'acceptant pas encore Cypress 16) et sans défilement horizontal ; adresse et langue conservées, filtres, lien vers les fiches, thème retenu.
- Trouvé par un test : un menu lié par `[value]` avant que ses options n'existent affichait la première option (« cylindre ») au lieu du tore tracé ; lié par `[selected]` sur chaque option, et le test échoue si l'on revient en arrière.

**CI** (`.github/workflows/angular.yml`) : types, Jest et build sur Linux, Windows et macOS, Node 22 et 24 ; Cypress sur le site construit avec les deux dashboards.

## Limites
- Deux vues sur quatre : ni Sprints ni visionneuse 3D (la même scène three.js n'apprendrait rien de plus sur Angular).
- Les mesures de la comparaison dépendent de la machine qui les prend (temps de build) ; tailles, lignes et paquets n'en dépendent pas.
- Deux versions de TypeScript dans le dépôt tant qu'Angular n'accepte pas la 7.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md) et D44 dans le [`DECISIONS.md` du site](../../DECISIONS.md).
