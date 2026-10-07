# react : le dashboard du portfolio en React et TypeScript

[![react](https://github.com/Dzop86/Portfolio/actions/workflows/react.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/react.yml)

Dashboard React 19 et TypeScript strict qui lit l'**API JSON statique** produite par le build du site (`api/v1/`) et en montre quatre vues : l'avancement des projets, les sprints (vélocité, burndown, objectifs), les résultats mesurés des autres projets (calcul parallèle, lecture de maillages, modèle ML) et une **visionneuse 3D** où le code C++ de [topologie](../topologie/), compilé en WebAssembly, calcule invariants et courbure de Gauss dans le navigateur. Publié avec le site sous [`/dashboard/`](https://dzop86.github.io/Portfolio/dashboard/).

*React 19 and strict TypeScript dashboard reading the static JSON API built by the site: project progress, sprints, the measured results of the other projects, and a three.js viewer colouring meshes by the Gaussian curvature that the topologie C++ code, compiled to WebAssembly, computes in the browser. French and English, the site's colours and themes, tested with Vitest and Playwright.*

## L'API qu'il lit
Le build du site (`src/api.mjs` à la racine) écrit, à partir des mêmes fichiers que les pages (D2, D43) :

| Fichier | Contenu | Source |
|---|---|---|
| `projects.json` | projets : nom et accroche FR/EN, état, points, sprint, technologies, liens | `data/projects.json` |
| `sprints.json` | objectif FR/EN de chaque sprint, points des stories, vélocité, burndown | `scrum/sprint-NN.md` |
| `parallel-bench.json` | temps de chaque version de la courbure | `projects/parallele/data/bench.json` |
| `mesh-io.json` | médiane des temps de lecture par bibliothèque, famille, taille et format | `projects/sql/data/measurements.csv` |
| `ml.json` | classes, précision sur le jeu de test, écart PyTorch/ONNX | `projects/ml/export/pointnet.json` |
| `meshes.json` | maillages de la visionneuse (nom FR/EN, fichier) et module WebAssembly de topologie | `projects/topologie/samples`, `src/assets/wasm/topo.js` |

Le dashboard Angular (sprint 32) lira la même API.

## Organisation
- `src/api.ts` : types de l'API, chargement en parallèle, erreur qui nomme le fichier et le statut.
- `src/i18n.ts` : tous les textes en français et en anglais (une clé manquante ne compile pas), langue par `?lang=fr|en` sinon celle du navigateur, nombres au format de chaque langue.
- `src/model.ts` : ce que les vues calculent (filtres, sommes, vitesses, séries), en fonctions pures.
- `src/charts/` : échelles linéaire et logarithmique, barres et courbes en SVG (trois séries au plus, distinguées par le trait autant que par la couleur, nommées au bout de la ligne), chaque graphique doublé d'un tableau.
- `src/views/` : Projets (filtres par état et technologie), Sprints, Résultats, Visionneuse 3D.
- `src/views/ViewerView.tsx` et `src/viewer/topology.ts` : three.js dans un composant React (scène créée au montage et entièrement libérée au démontage), chargé à la demande (`React.lazy`, three.js ne vient qu'avec cette vue) ; lecture par le module WebAssembly que publie le site et par son `topo-api.js`, partagés avec la visionneuse de la vitrine, pas copiés ; légende par quantiles, courbure au survol, fichier OBJ ou PLY ouvert depuis l'ordinateur, repli sans WebGL.
- `src/App.tsx` : vues par fragment d'URL (`#sprints`), chargement annulé au démontage, bouton Réessayer, thème clair ou sombre partagé avec le site (même clé `theme`).
- Couleurs : `src/assets/tokens.css` du site, importé tel quel (règle 6).

## Lancer
Node 22.12 ou plus récent.
```sh
npm run build --prefix ../..   # le site et son API (dist/api/v1/), lus par le serveur de développement
npm ci
npm run dev                     # http://localhost:5173/?lang=fr
npm run typecheck && npm test   # types stricts, puis Vitest
npm run build                   # dist/, copié dans dashboard/ par le build du site
```

## Tests
- **Vitest** (39 tests) : échelles (dont une série plate, qui bouclait sans fin avant correction), traductions (mêmes clés et mêmes paramètres dans les deux langues, formats de nombres), calculs des vues ; la visionneuse sur le **vrai module WebAssembly** (invariants du tore, de la sphère et du ruban de Möbius, erreur de lib-c avec sa ligne, module qui ne se charge pas puis se recharge, couleurs) et en composant (sans WebGL : repli, invariants, choix d'un maillage, fichier en erreur).
- **Intégration** : le dashboard entier rendu (Testing Library) sur l'API que le site construit vraiment, servie par un faux `fetch` : chargement, filtres, navigation entre vues, graphiques et tableaux, fichier en erreur puis Réessayer, langue et thème.
- **Playwright** (`tests/e2e/dashboard.spec.js` à la racine) : chaque vue dans les deux langues et les deux thèmes, sur Chromium, Firefox, WebKit, Pixel 7 et iPhone 14 ; axe (WCAG 2 AA, contrastes compris), pas de défilement horizontal, cibles tactiles de 44 px, lien vers les fiches du site ; la visionneuse avec le vrai WebAssembly (invariants, fichier cassé) et, là où WebGL existe (Chromium, WebKit), la courbure lue au survol de l'anneau.

**CI** (`.github/workflows/react.yml`) : types, Vitest et build sur Linux, Windows et macOS, Node 22 et 24 ; Playwright dans la CI du site ; le déploiement lance les tests avant de publier.

## Captures
`scripts/screenshots.mjs` refait les captures de la fiche (vue Résultats en sombre, visionneuse en clair, en français et en anglais), le site servi : `node projects/react/scripts/screenshots.mjs` depuis la racine, après `npm run build` et `npm run serve`.

## Pour le dashboard Angular (sprint 32)
Ce qu'il reprendra tel quel : l'API (`api/v1/`), les textes et la charte (`tokens.css`), les mêmes vues pour comparer. Ce qui changera, et sera comparé : composants à signaux d'Angular au lieu des hooks, injection de dépendances pour le chargement de l'API au lieu de `useApi`, routeur d'Angular au lieu du fragment d'URL, Jest et Cypress au lieu de Vitest et Playwright (comme annoncé dans le plan).

## Limites
- Les données ne changent qu'au déploiement du site : l'API est statique.
- Le dashboard demande JavaScript ; les mêmes informations restent sur les pages du site, qui s'en passent.
- La visionneuse demande WebGL ; sans lui (certains navigateurs sans écran, comme Firefox en CI), elle le dit et calcule quand même les invariants. three.js pèse 560 ko (140 ko compressés), chargés seulement avec cette vue.
- Les titres des sprints et le texte des stories ne sont qu'en français : l'API ne publie que l'objectif, bilingue, et les points.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md) et D43 dans le [`DECISIONS.md` du site](../../DECISIONS.md).
