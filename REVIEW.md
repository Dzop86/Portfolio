# Relecture humaine (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et ce que j'ai corrigé dans le code généré. Au moins 2 ou 3 constats par sprint.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Sprint 1 : points à relire en priorité
- [ ] `tests/unit/privacy.test.mjs` : les motifs couvrent-ils tous les cas ? Ajouter mes termes privés au secret `PRIVATE_TERMS`.
- [ ] `data/cv.json` : textes, dates et statut (ATER 2025-2026 ou autre ?).
- [ ] `src/templates.mjs` : échappement HTML (`esc`) appliqué partout ?
- [ ] Traductions anglaises.

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `tests/e2e/site.spec.js` | Détecté par le test axe : tableaux défilants inaccessibles au clavier sur mobile | `tabindex="0"` et `role="region"` sur les conteneurs |
| 2026-10-06 | `package.json` | (Claude) `engines` annonçait Node 20, mais `node --test` avec un motif glob exige Node 21 : aucun test ne tournait sous Node 20 | Node 22 minimum, CI sur Node 22 et 24 (D8) |
| 2026-10-06 | `src/lib.mjs` | (Claude) Le lien « Code source » pointait vers un dépôt fictif `charles-lepaire/portfolio` | Lien vers `Dzop86/Portfolio`, test sur le pied de page |
| 2026-10-06 | `src/build.mjs` | (Claude) La page 404 perdait son style et ses liens sur une URL imbriquée (`/Portfolio/a/b`), car ses chemins relatifs partaient de l'URL demandée | `<base href>` calculé depuis `BASE_PATH`, nginx sert `404.html` dans Docker, tests unitaire, intégration et e2e (D10) |
| 2026-10-06 | `data/cv.json` | (Claude) Le lien HAL pointait vers `cv.hal.science/charles-lepaire`, qui renvoie une 404 (aucun CV HAL créé) | Recherche HAL par identifiant auteur `authIdHal_s:charles-lepaire`, test sur la page contact |
| | | | |
