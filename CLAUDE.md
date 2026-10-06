# CLAUDE.md, instructions pour Claude Code

Lis aussi `PLAN.md` (vision, 19 projets, roadmap) avant toute tâche.

## Contexte
Portfolio de Charles Lepaire, docteur en informatique graphique. Message porté : « je sais faire générer du code par l'IA dans beaucoup de langages, puis le relire, le tester et le livrer proprement ». Fil rouge : les maillages 3D et leur topologie.

## Commandes
- `npm run build` : génère `dist/` (site statique FR/EN).
- `npm test` : tests unitaires et d'intégration (`node:test`), dont le scan de données privées.
- `npm run test:e2e` : Playwright, desktop (Chromium, Firefox, WebKit) et mobile (Pixel 7, iPhone 14), avec axe pour l'accessibilité.
- `npm run serve` : sert `dist/` sur http://localhost:4173.
- `docker compose up --build` : sert le site sur http://localhost:8080 et l'API (fastapi) sur http://localhost:8000.

## Règles non négociables
1. **Tests d'abord.** Toute fonctionnalité arrive avec ses tests unitaires et au moins un test d'intégration. Pas de merge si la CI est rouge.
2. **Bilingue.** Tout texte visible existe en `fr` et `en` (`data/i18n/*.json` ou objets `{ fr, en }`). Une clé manquante fait échouer le build.
3. **Confidentialité.** Publics : nom, prénom, LinkedIn, ORCID, HAL. Interdits partout (code, données, commits) : adresse, téléphone, e-mail, date de naissance. Aucun code ni aucune donnée du laboratoire XLIM ou du projet ARTERIA, aucune donnée médicale : tout s'écrit de zéro, avec des maillages synthétiques ou sous licence libre.
4. **Multi-OS.** Les projets compilés passent une matrice CI `ubuntu-latest`, `windows-latest`, `macos-latest`. Documente honnêtement les limites (CUDA indisponible sur macOS, applis non signées).
5. **Mobile.** Pas de défilement horizontal de page à 375 px, cibles tactiles de 44 px minimum.
6. **Charte.** Couleurs uniquement via `src/assets/tokens.css` : gris sombre façon VS Code par défaut (`#1f1f1f`), pistache `#bef374` en accent, chocolat `#5a3a22` en touches. Jamais de pistache en texte sur fond clair.
7. **Pas de secrets.** Variables d'environnement et secrets GitHub uniquement. Jamais de clé dans le dépôt.
8. **Petits pas.** Une story à la fois, un commit par étape logique, messages en anglais au format Conventional Commits (`feat:`, `fix:`, `test:`, `docs:`, `ci:`).

## Traçabilité
- `DECISIONS.md` : tu y notes chaque choix d'architecture, ses alternatives et ses limites.
- `REVIEW.md` : la relecture de Charles. Tu y ajoutes au fur et à mesure chaque problème trouvé et sa correction, préfixé « (Claude) ». Tu ne coches pas ses cases et ne modifies pas ses propres constats.
- `scrum/sprint-NN.md` : objectif, stories, points, ce qui est fait, rétro.

## Structure
- `data/` : contenu (CV, projets, Scrum, traductions). Ajouter un projet = une entrée dans `projects.json`.
- `src/` : générateur (`build.mjs`, `templates.mjs`, `lib.mjs`) et assets.
- `tests/unit/`, `tests/e2e/` : tests.
- Les projets techniques (C, Ada, ML...) vivent dans `projects/<id>/`, chacun avec son README, son `REVIEW.md`, ses tests et son job CI.
