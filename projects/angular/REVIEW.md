# Relecture humaine, angular (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/app/pages/` : signaux, `httpResource`, menus, erreurs.
- [ ] `tests/` et `cypress/` : ce que les tests vérifient vraiment.
- [ ] La fiche : la comparaison, ses mesures et ses réserves (`scripts/compare.mjs`).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `tsconfig.json` | (Claude) Le projet généré par la CLI n'activait ni `strict` ni les gabarits stricts | `strict`, `noUncheckedIndexedAccess`, `strictTemplates` ; ils ont aussitôt trouvé deux objets mal typés |
| 2026-10-07 | `src/index.html` | (Claude) Vu en vérifiant l'adresse : avec `<base href="./">`, la navigation perdait `?lang=fr`, et la langue changeait au rechargement | Balise retirée (inutile au routeur à fragment) |
| 2026-10-07 | `src/app/pages/results.ts` | (Claude) Vu sur une capture : le menu Famille affichait « cylindre » alors que le tore était tracé ; `[value]` posé sur `<select>` avant que ses options n'existent | `[selected]` sur chaque option, partout ; test Jest qui échoue si l'on revient en arrière (vérifié) |
| 2026-10-07 | `../react/src/i18n.ts` | (Claude) Relu en écrivant la page Résultats : `makeT` plantait sur une clé absente (une famille ou une forme nouvelle dans les données), dans les deux dashboards | Une clé absente s'affiche telle quelle ; test |
| 2026-10-07 | `tests/pages.spec.ts` | (Claude) Les tests attendaient `whenStable()`, qui attend les requêtes en cours, que seul le test sert : délai dépassé | Effets lancés par `TestBed.tick()`, puis réponse ; contrôleur HTTP vérifié depuis le montage, Angular réinitialisant son module avant `afterEach` |
| 2026-10-07 | `scripts/compare.mjs` | (Claude) Première version : le total du React comptait three.js (visionneuse, sans équivalent Angular) et Vitest 5 écrit son rapport JSON dans un fichier | Seul le chargement à l'ouverture est comparé, réserves écrites sur la fiche ; rapport lu dans un fichier temporaire |
| | | | |
