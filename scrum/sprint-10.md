# Sprint 10 : une base SQL pour les mesures de performance

**Objectif :** le projet sql range dans PostgreSQL des mesures réelles de performance des bibliothèques du portfolio (lib-c et topologie, en WebAssembly), les analyse avec des vues, des fonctions de fenêtrage et des index justifiés, et la même base s'interroge dans le navigateur.

| Story | Points | État |
|---|---|---|
| En tant qu'ingénieur, je range dans PostgreSQL (sql) des mesures réelles de lib-c et topologie : schéma contraint, vues, fonctions de fenêtrage, index justifiés par `EXPLAIN` ; tests pgTAP en CI. | 3 | Fait |
| En tant que visiteur, j'interroge la base (sql) dans le navigateur avec sql.js : requêtes d'exemple, résultats en tableau, erreurs lisibles ; tests Node et Playwright, mobile compris. | 2 | Fait |

**Tests :** sql 10 tests Node sur 3 OS, 46 vérifications pgTAP sur PostgreSQL 17 ; bac à sable 11 tests Node et un scénario Playwright dans 5 navigateurs (240 tests e2e au total).

**Trouvé par les mesures :** le comptage d'arêtes de lib-c (`qsort`) coûte deux fois la lecture ; topologie, qui fait plus de travail, va plus vite en WebAssembly. Correction à décider par Charles.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
