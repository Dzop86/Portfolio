# Sprint 1 : la vitrine en ligne

**Objectif :** le portfolio est en ligne, bilingue, testé et déployé automatiquement.

**Goal:** the portfolio is online, bilingual, tested and deployed automatically.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, je navigue entre 5 pages en français et en anglais. | 2 | Fait |
| En tant que visiteur mobile, je lis toutes les pages sans défilement horizontal. | 1 | Fait |
| En tant que recruteur, je consulte le CV analytique (thèse, publications, heures filtrables). | 1 | Fait |
| En tant que recruteur, je vois la roadmap, les risques et la Definition of Done. | 1 | Fait |
| En tant que Charles, je suis protégé contre la publication de mes données privées. | 1 | Fait |
| En tant que Charles, chaque push est testé (Linux, Windows, macOS) puis déployé. | 1 | Fait |
| En tant que Charles, je sers le site avec Docker. | 1 | Fait |

**Tests :** 41 tests unitaires et d'intégration, 30 tests end-to-end sur Chromium desktop et mobile (Firefox, WebKit et iPhone tournent en CI).

## Rétro (Charles)
- Ce qui a marché : Le site est en ligne dès le premier sprint, bilingue, testé sur trois OS et déployé à chaque push ; le scan des données privées tourne dans les tests.
- Ce que l'IA a mal fait : Rien de bloquant ; Firefox, WebKit et iPhone n'étaient testés qu'en CI, pas en local.
- À changer au prochain sprint : Pages de détail par projet, et le premier projet technique (lib-c) avec sa CI multi-OS.

## Sprint 2 (proposé)
- Pages détail par projet, générées depuis `projects.json`.
- Squelette `projects/lib-c/` : CMake, lecteur OBJ, tests Unity, matrice CI multi-OS, miroir GitLab.
