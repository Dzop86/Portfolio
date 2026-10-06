# Relecture humaine (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et ce que j'ai corrigé dans le code généré. Au moins 2 ou 3 constats par sprint.

## Sprint 1 : points à relire en priorité
- [ ] `tests/unit/privacy.test.mjs` : les motifs couvrent-ils tous les cas ? Ajouter mes termes privés au secret `PRIVATE_TERMS`.
- [ ] `data/cv.json` : textes, dates et statut (ATER 2025-2026 ou autre ?).
- [ ] `src/templates.mjs` : échappement HTML (`esc`) appliqué partout ?
- [ ] Traductions anglaises.

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `tests/e2e/site.spec.js` | Détecté par le test axe : tableaux défilants inaccessibles au clavier sur mobile | `tabindex="0"` et `role="region"` sur les conteneurs |
| | | | |
