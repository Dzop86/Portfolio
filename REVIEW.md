# Relecture humaine (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et ce que j'ai corrigé dans le code généré. Au moins 2 ou 3 constats par sprint.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Sprint 1 : points à relire en priorité
- [x] `tests/unit/privacy.test.mjs` : les motifs couvrent-ils tous les cas ? Ajouter mes termes privés au secret `PRIVATE_TERMS`.
- [x] `data/cv.json` : textes, dates et statut (ATER 2025-2026 ou autre ?).
- [x] `src/templates.mjs` : échappement HTML (`esc`) appliqué partout ?
- [x] Traductions anglaises.

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« valide les relectures »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `tests/e2e/site.spec.js` | Détecté par le test axe : tableaux défilants inaccessibles au clavier sur mobile | `tabindex="0"` et `role="region"` sur les conteneurs |
| 2026-10-06 | `package.json` | (Claude) `engines` annonçait Node 20, mais `node --test` avec un motif glob exige Node 21 : aucun test ne tournait sous Node 20 | Node 22 minimum, CI sur Node 22 et 24 (D8) |
| 2026-10-06 | `src/lib.mjs` | (Claude) Le lien « Code source » pointait vers un dépôt fictif `charles-lepaire/portfolio` | Lien vers `Dzop86/Portfolio`, test sur le pied de page |
| 2026-10-06 | `src/build.mjs` | (Claude) La page 404 perdait son style et ses liens sur une URL imbriquée (`/Portfolio/a/b`), car ses chemins relatifs partaient de l'URL demandée | `<base href>` calculé depuis `BASE_PATH`, nginx sert `404.html` dans Docker, tests unitaire, intégration et e2e (D10) |
| 2026-10-06 | `data/cv.json` | (Claude) Le lien HAL pointait vers `cv.hal.science/charles-lepaire`, qui renvoie une 404 (aucun CV HAL créé) | Recherche HAL par identifiant auteur `authIdHal_s:charles-lepaire`, test sur la page contact |
| 2026-10-06 | `src/assets/style.css` | (Claude) Détecté par axe : les boutons d'exemple de la démo, de vrais `<button>`, gardaient le fond gris du navigateur sous `.btn-ghost` (contraste 3,3:1) | `.btn` remet à zéro fond, bordure et police des boutons natifs |
| 2026-10-06 | `src/assets/wasm/` | (Claude) nginx ne connaît pas l'extension `.mjs` : le module aurait été servi en `application/octet-stream` et refusé par le navigateur dans Docker | Sortie Emscripten en `.js` (le dépôt est en `"type": "module"`) |
| 2026-10-06 | `tests/unit/wasm.test.mjs` | (Claude) Le test de libération mémoire lisait un petit cube 2000 fois : une fuite serait restée sous les 16 Mo initiaux, le test ne pouvait pas échouer | Fichier de 1 Mo lu 64 fois ; vérifié qu'il échoue sans `_free` |
| 2026-10-06 | `tests/unit/privacy.test.mjs` | (Claude) Le scan de confidentialité ne filtrait que `build/` : il parcourait aussi les dossiers locaux `build-shared/` (87 fichiers de plus, sans risque de fuite mais lent et trompeur) | Filtre sur `build*`, `__pycache__` et `*.egg-info`, test renforcé |
| 2026-10-06 | `tests/unit/privacy.test.mjs` | (Claude) Le scan parcourait les binaires de compilation locaux d'Ada (`bin/`, `obj/`) et échouait ; la liste de dossiers à exclure aurait grossi à chaque langage | Scan des seuls fichiers suivis par git sous `projects/` |
| | | | |
