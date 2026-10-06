# Sprint 1 : la vitrine en ligne

**Objectif :** le portfolio est en ligne, bilingue, testé et déployé automatiquement.

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

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :

## Sprint 2 (proposé)
- Pages détail par projet, générées depuis `projects.json`.
- Squelette `projects/lib-c/` : CMake, lecteur OBJ, tests Unity, matrice CI multi-OS, miroir GitLab.
