# Charles Lepaire, portfolio

[![CI](../../actions/workflows/ci.yml/badge.svg)](../../actions/workflows/ci.yml)
[![Deploy](../../actions/workflows/deploy.yml/badge.svg)](../../actions/workflows/deploy.yml)

Portfolio bilingue (français, anglais) : 22 projets (dont 7 jeux) autour des maillages 3D, un CV analytique et la gestion de projet. Le code est généré avec Claude Code, puis relu, testé et documenté par moi (voir `REVIEW.md`).

Bilingual portfolio (French, English): 22 projects (7 of them games) around 3D meshes, an academic CV and project management. The code is generated with Claude Code, then reviewed, tested and documented by me (see `REVIEW.md`).

Site en ligne : https://dzop86.github.io/Portfolio/

## Lancer en local
```bash
npm ci
npm run build
npm run serve            # http://localhost:4173
npm test                 # unit and integration tests
npx playwright install   # once
npm run test:e2e         # desktop and mobile, 3 browser engines
docker compose up --build  # site sur http://localhost:8080, API sur http://localhost:8000
```
Fonctionne sous Linux, macOS et Windows (Node 22 ou plus récent).

## Structure
- `data/` : contenu (CV, projets, Scrum, traductions)
- `src/` : générateur statique et assets (charte dans `src/assets/tokens.css`)
- `tests/` : tests unitaires, d'intégration et end-to-end
- `.github/workflows/` : CI (Linux, Windows, macOS, Playwright, Docker) et déploiement GitHub Pages
- `PLAN.md`, `CLAUDE.md`, `DECISIONS.md`, `REVIEW.md`, `scrum/` : pilotage du projet

## Licence
MIT
