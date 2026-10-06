# Plan du portfolio

## Objectif
Montrer qu'un ingénieur docteur en informatique graphique sait piloter la génération de code par l'IA dans une quinzaine de technologies, et surtout la relire, la tester et la livrer. Public visé : recruteurs (FR et EN) et milieu académique.

## Principes
- Un site vitrine multi-pages, bilingue, responsive, installable (PWA), hébergé gratuitement sur GitHub Pages.
- Un fil rouge : les maillages 3D. Les projets forment une chaîne cohérente.
- Chaque projet : tests unitaires + intégration, CI (multi-OS si compilé), Docker si serveur, démo en ligne (statique ou pré-calculée), README, `REVIEW.md`, `DECISIONS.md`.
- Planning de référence illustratif de septembre 2025 à octobre 2026, affiché comme tel. Les dates réelles sont celles des commits.

## Les 17 projets
Source de vérité : `data/projects.json`. Résumé :

| # | Projet | Stack principale | Sprint | Points |
|---|---|---|---|---|
| 1 | Vitrine (ce site) | HTML, CSS, JS, Node, Playwright | S1-S9 | 5 |
| 2 | Topologie 3D | C++, Three.js | S4-S5 | 13 |
| 3 | Bibliothèque C de maillages | C, CMake, WebAssembly, libFuzzer | S2-S6 | 8 |
| 4 | Visionneuse Qt/OpenGL | C++, Qt, OpenGL | S20-S21 | 13 |
| 5 | API Python | FastAPI, pytest, Docker | S6-S7 | 8 |
| 6 | ML et MLOps | PyTorch, scikit-learn, DVC, MLflow, ONNX | S8+S16 | 13 |
| 7 | Base SQL des benchmarks | PostgreSQL, sql.js | S10 | 5 |
| 8 | Microservices Spring | Java, Spring Boot, Testcontainers, Jenkins | S17-S18 | 13 |
| 9 | API ASP.NET | C#, EF Core, xUnit | S19 | 8 |
| 10 | Mini-langage | Flex, Bison, OCaml | S11-S12 | 8 |
| 11 | Carrefour en Ada | Ada, AUnit | S9 | 5 |
| 12 | Calcul parallèle | OpenMP, CUDA, OpenCL | S22-S23 | 13 |
| 13 | Dashboard React | React, TypeScript, Vite | S24-S25 | 13 |
| 14 | Dashboard Angular | Angular, Jest, Cypress | S26 | 8 |
| 15 | Migration Bootstrap | jQuery, Bootstrap | S27 | 5 |
| 16 | Éditeur LaTeX | TypeScript, KaTeX | S13-S14 | 8 |
| 17 | Mini-cours G-cartes | JS, SVG | S15 | 5 |

Total : 151 points sur 27 sprints de deux semaines (numéros réels jusqu'au sprint 9, prévisionnels ensuite).

## CI/CD
- GitHub Actions : CI principale, déploiement Pages, matrice multi-OS.
- Jenkins : `projects/spring/Jenkinsfile`.
- GitLab CI : abandonné le 6 octobre (pas de compte GitLab) ; tous les projets, ML compris, passent par GitHub Actions.
- Docker : un Dockerfile multi-stage par service, `compose.yaml` global, images sur GHCR.
- MLOps : DVC (données et modèle), MLflow (expériences), entraînement et évaluation en CI avec seuil de précision bloquant, déploiement du modèle dans le conteneur FastAPI.

## Ordre de réalisation (décision de Charles, 6 octobre 2026)
Faits : vitrine (en continu), bibliothèque C, topologie 3D, API Python, ML (jeu de données et modèles).
Ensuite, dans cet ordre : Ada, base SQL, mini-langage, éditeur LaTeX, mini-cours G-cartes. La suite du ML (export ONNX, classification dans l'API, page de résultats) est reportée après eux. Les sprints S1-S26 du tableau restent le planning de référence illustratif.

## Gestion de projet
- Scrum, sprints de deux semaines, Definition of Done dans `data/scrum.json`.
- Registre des risques : `data/scrum.json`, affiché sur la page Méthode, revu à chaque rétro.
- Priorisation MoSCoW : Must = vitrine, topologie, C, ML, Spring, SQL. Could = Angular, Bootstrap.

## Reporté
- Agent conversationnel.
- Mascotte.

## Questions ouvertes
- Situation professionnelle actuelle (fin de l'ATER ?) pour mettre `data/cv.json` à jour.
- Libellé unique entre le CV industriel et le CV analytique.
- Nom définitif du compte ou de l'organisation GitHub.
