# Relecture humaine, react (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `../../src/api.mjs` : ce que l'API publie (rien de privé, rien qui ne soit déjà sur les pages), la médiane des mesures.
- [x] `src/App.tsx` et `src/api.ts` : chargement, annulation, erreurs, thème.
- [x] `src/charts/` et `src/views/ResultsView.tsx` : les graphiques face aux chiffres des autres projets.

> Cases cochées par Claude le 7 octobre 2026, à la demande explicite de Charles (« review ok, go »), pour le volet du sprint 30 (API et dashboard).

Sprint 31 (visionneuse 3D et fiche) :
- [ ] `src/views/ViewerView.tsx` : création et libération de la scène three.js, survol, erreurs.
- [ ] `src/viewer/topology.ts` : lecture par le WebAssembly du site, couleurs.
- [ ] La fiche : captures et textes alternatifs, script `scripts/screenshots.mjs`.

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `src/charts/scale.ts` | (Claude) Détecté par un test : une série plate (minimum = maximum) donnait un pas presque nul, et la boucle des graduations ne s'arrêtait pas | Une série plate prend une étendue de son propre ordre de grandeur |
| 2026-10-07 | `src/views/SprintsView.tsx` | (Claude) Vu sur une capture : la vélocité plongeait à 0 au sprint 30, ouvert le jour même | Points livrés tracés pour les sprints terminés seulement ; « en cours » dans le tableau ; test qui le vérifie |
| 2026-10-07 | `src/views/ResultsView.tsx` | (Claude) Vu sur une capture : les classes du modèle ML restaient en anglais dans le texte français | Noms des formes traduits ; test sur le texte français |
| 2026-10-07 | `tests/App.test.tsx` | (Claude) `toHaveTextContent` remplace l'espace insécable du français par une espace simple : l'assertion sur « 95,2 % » échouait à tort | Comparaison sur `textContent` |
| 2026-10-07 | `../../tests/unit/docker-context.test.mjs` | (Claude) Le test prenait la première étape du Dockerfile pour celle du site ; l'étape du dashboard vient maintenant avant | L'étape est cherchée par son nom (`AS build`) |
| 2026-10-07 | `../../.dockerignore` | (Claude) `node_modules` n'écartait que celui de la racine : celui du dashboard serait parti dans le contexte Docker | `**/node_modules` et `projects/react/dist` écartés |
| 2026-10-07 | `tsconfig.json` | (Claude) Détecté par la CI : `vite.config.ts` utilise les modules de Node, mais `@types/node` manquait ; en local, TypeScript trouvait celui de la racine en remontant les dossiers | `@types/node` ajouté ; vérifié dans une copie isolée, sans le `node_modules` de la racine, comme en CI |
| 2026-10-07 | `../../tests/unit/site.test.mjs`, `../../.github/workflows/deploy.yml` | (Claude) Détecté par le déploiement : le test des liens voyait `../dashboard/` cassé, car le dashboard était construit après les tests ; en local, un ancien build le masquait | Le déploiement construit le dashboard avant les tests et l'exige (`REQUIRE_DASHBOARD`) ; ailleurs, son lien n'est excusé que s'il n'est pas construit |
| 2026-10-07 | `vite.config.ts` | (Claude) Détecté par la CI, sur Windows et Node 22 seulement : le premier test de formatage des nombres a mis 5,2 s (premier usage d'`Intl` sur une machine froide), au-delà des 5 s par défaut | Délai des tests porté à 20 s, raison notée dans la configuration |
| 2026-10-07 | `src/views/ViewerView.tsx` | (Claude) Relu face à la vitrine : les quatre erreurs de lib-c (fichier illisible, mémoire, syntaxe, sommet absent) s'affichaient toutes en « format non reconnu » | Les quatre messages de la vitrine, traduits ; test sur l'erreur 4 avec sa ligne |
| 2026-10-07 | `src/App.tsx` | (Claude) Vu au build : three.js faisait passer le paquet unique du dashboard au-delà du seuil de 500 ko de Vite, pour toutes les vues | Visionneuse chargée à la demande (`React.lazy`) : 250 ko sans elle, 560 ko pour elle seule |
| 2026-10-07 | `../../tests/e2e/dashboard.spec.js` | (Claude) Le test du survol échouait sur Chromium et WebKit : le point visé (30 % de la largeur) tombait hors du tore sur un canevas plus large que haut, et sous le bas d'une fenêtre de 720 px ; diagnostiqué par des captures, le rendu était bon | Point visé selon la hauteur du canevas (taille du tore à l'écran), `hover()` qui fait défiler, survol répété le temps que la forme entre dans la scène ; 3 passages sur 3 |
| 2026-10-07 | `src/views/ViewerView.tsx` | (Claude) Les couleurs des jetons doivent passer par `Color` de three.js (conversion sRGB vers linéaire), comme la vitrine ; une lecture directe des valeurs les aurait éclaircies | `new Color(css)` pour chaque jeton |
| | | | |
