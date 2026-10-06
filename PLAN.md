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
| 1 | Vitrine (ce site) | HTML, CSS, JS, Node, Playwright | S1-S4 | 5 |
| 2 | Topologie 3D | C++, Three.js | S5-S8 | 13 |
| 3 | Bibliothèque C de maillages | C, CMake, WebAssembly, GitLab CI | S5-S8 | 8 |
| 4 | Visionneuse Qt/OpenGL | C++, Qt, OpenGL | S5-S8 | 13 |
| 5 | API Python | FastAPI, pytest, Docker | S9-S12 | 8 |
| 6 | ML et MLOps | PyTorch, scikit-learn, DVC, MLflow | S9-S12 | 13 |
| 7 | Base SQL des benchmarks | PostgreSQL, sql.js | S9-S12 | 5 |
| 8 | Microservices Spring | Java, Spring Boot, Testcontainers, Jenkins | S13-S16 | 13 |
| 9 | API ASP.NET | C#, EF Core, xUnit | S13-S16 | 8 |
| 10 | Mini-langage | Flex, Bison, OCaml | S17-S20 | 8 |
| 11 | Carrefour en Ada | Ada, AUnit | S17-S20 | 5 |
| 12 | Calcul parallèle | OpenMP, CUDA, OpenCL | S17-S20 | 13 |
| 13 | Dashboard React | React, TypeScript, Vite | S21-S22 | 13 |
| 14 | Dashboard Angular | Angular, Jest, Cypress | S21-S22 | 8 |
| 15 | Migration Bootstrap | jQuery, Bootstrap | S23-S26 | 5 |
| 16 | Éditeur LaTeX | TypeScript, KaTeX | S23-S26 | 8 |
| 17 | Mini-cours G-cartes | JS, SVG | S23-S26 | 5 |

Total : 151 points sur 26 sprints de deux semaines.

## CI/CD
- GitHub Actions : CI principale, déploiement Pages, matrice multi-OS.
- Jenkins : `projects/spring/Jenkinsfile`.
- GitLab CI : miroir GitLab, `.gitlab-ci.yml` pour la bibliothèque C et le ML.
- Docker : un Dockerfile multi-stage par service, `compose.yaml` global, images sur GHCR.
- MLOps : DVC (données et modèle), MLflow (expériences), entraînement et évaluation en CI avec seuil de précision bloquant, déploiement du modèle dans le conteneur FastAPI.

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
