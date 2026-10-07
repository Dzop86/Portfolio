# Plan du portfolio

## Objectif
Montrer qu'un ingénieur docteur en informatique graphique sait piloter la génération de code par l'IA dans une quinzaine de technologies, et surtout la relire, la tester et la livrer. Public visé : recruteurs (FR et EN) et milieu académique.

## Principes
- Un site vitrine multi-pages, bilingue, responsive, installable (PWA), hébergé gratuitement sur GitHub Pages.
- Un fil rouge : les maillages 3D. Les projets forment une chaîne cohérente.
- Chaque projet : tests unitaires + intégration, CI (multi-OS si compilé), Docker si serveur, démo en ligne (statique ou pré-calculée), README, `REVIEW.md`, `DECISIONS.md`.
- Roadmap par sprints de deux semaines (S1 à S33), affichée sur la page Gestion de projet : l'état de chaque phase (fait, en cours, prévu) est calculé depuis `scrum/sprint-NN.md`. Les dates réelles sont celles des commits.

## Les 21 projets
Source de vérité : `data/projects.json`. Résumé :

| # | Projet | Stack principale | Sprint | Points |
|---|---|---|---|---|
| 1 | Vitrine (ce site) | HTML, CSS, JS, Node, Playwright | S1-S9 | 5 |
| 2 | Topologie 3D | C++, Three.js | S4-S5 | 13 |
| 3 | Bibliothèque C de maillages | C, CMake, WebAssembly, libFuzzer | S2-S6 | 8 |
| 4 | Visionneuse Qt/OpenGL | C++, Qt, OpenGL | S26-S27 | 13 |
| 5 | API Python | FastAPI, pytest, Docker | S6-S7 | 8 |
| 6 | ML et MLOps | PyTorch, scikit-learn, DVC, MLflow, ONNX | S8+S16 | 13 |
| 7 | Base SQL des benchmarks | PostgreSQL, sql.js, pgTAP | S10 | 5 |
| 8 | Mini-langage | Flex, Bison, C, OCaml, js_of_ocaml | S11-S12 | 8 |
| 9 | Carrefour en Ada | Ada, SPARK, AUnit | S9 | 5 |
| 10 | Calcul parallèle | OpenMP, CUDA, OpenCL | S28-S29 | 13 |
| 11 | Dashboard React | React, TypeScript, Vite | S30-S31 | 13 |
| 12 | Dashboard Angular | Angular, Jest, Cypress | S32 | 8 |
| 13 | Migration Bootstrap | jQuery, Bootstrap | S33 | 5 |
| 14 | Éditeur LaTeX | TypeScript, KaTeX | S13-S14 | 8 |
| 15 | Mini-cours G-cartes | JS, SVG | S15 | 5 |
| 16 | Jeu : Othello | C, WebAssembly | S17-S18 | 8 |
| 17 | Jeu : bataille navale | Java, JavaFX | S19 | 5 |
| 18 | Jeu : aventure textuelle | Java | S20 | 5 |
| 19 | Jeu : bataille (cartes) | Ada | S21 | 3 |
| 20 | Jeu : morpion | Python | S22 | 3 |
| 21 | Jeu : roguelike 2D | Godot 4, C#, ASP.NET Core, EF Core, PostgreSQL, JWT | S23-S25 | 13 |

Total : 167 points sur 33 sprints de deux semaines (numéros réels jusqu'au sprint 15, prévisionnels ensuite).

## CI/CD
- GitHub Actions : CI principale, déploiement Pages, matrice multi-OS.
- GitLab CI : abandonné le 6 octobre (pas de compte GitLab) ; tous les projets, ML compris, passent par GitHub Actions.
- Docker : un Dockerfile multi-stage par service, `compose.yaml` global, images sur GHCR.
- MLOps : DVC (données et modèle), MLflow (expériences), entraînement et évaluation en CI avec seuil de précision bloquant, déploiement du modèle dans le conteneur FastAPI.

## Ordre de réalisation (décision de Charles, 6 octobre 2026)
Faits : vitrine (en continu), bibliothèque C, topologie 3D, API Python, ML (jeu de données et modèles).
Ensuite, dans cet ordre : Ada, base SQL, mini-langage, éditeur LaTeX, mini-cours G-cartes. La suite du ML (export ONNX, classification dans l'API, page de résultats) est reportée après eux. Le tableau ci-dessus et `data/scrum.json` suivent cet ordre.

## Périmètre revu (décision de Charles, 6 octobre 2026, D25)
Les microservices Spring et l'API ASP.NET sont retirés : ils ne seront pas faits. La visionneuse Qt/OpenGL est gardée et vient juste après la suite du ML : S16 ML (ONNX, classification dans l'API), S17-S18 Qt/OpenGL, S19-S20 calcul parallèle, S21-S22 React, S23 Angular, S24 Bootstrap.

## Jeux (décision de Charles, 6 octobre 2026, D26)
Une section « Jeux de mes études » s'ajoute à la page Projets : quatre jeux programmés pendant ses études, réécrits de zéro avec l'IA (tests, CI, démo jouable quand c'est possible). Othello en C jouable dans le navigateur (S17-S18), bataille navale en Java/JavaFX (S19), aventure textuelle en Java (S20, réécrite sans reprendre le code d'origine, écrit à deux), bataille en Ada (S21). Ils viennent après la suite du ML (S16) ; Qt/OpenGL passe en S22-S23 et la suite est décalée de cinq sprints.

## Deux jeux de plus (décision de Charles, 7 octobre 2026, D31)
Un morpion en Python (S22) et un roguelike 2D rejoignent les jeux, juste après la bataille. Le roguelike (D32, 13 points, S23-S25) : client Godot 4 en C# pour le bureau (Godot 4 n'exporte pas le C# vers le web), règles partagées en C#, API ASP.NET Core de scores avec comptes JWT, EF Core, PostgreSQL, Swagger, Docker et GitHub Actions ; l'API vérifie chaque score en rejouant la partie. Qt/OpenGL passe en S26-S27 et la suite est décalée d'autant.

## Gestion de projet
- Scrum, sprints de deux semaines, Definition of Done dans `data/scrum.json`.
- Registre des risques : `data/scrum.json`, affiché sur la page Gestion de projet, revu à chaque rétro.
- Priorisation MoSCoW : Must = vitrine, topologie, C, ML, SQL, Qt. Should = les six jeux. Could = Angular, Bootstrap. Won't (retirés le 6 octobre) = microservices Spring, API ASP.NET.

## Reporté
- Agent conversationnel.
- Mascotte.

## Questions ouvertes
- Situation professionnelle actuelle (fin de l'ATER ?) pour mettre `data/cv.json` à jour.
- Libellé unique entre le CV industriel et le CV analytique.
- Nom définitif du compte ou de l'organisation GitHub.
