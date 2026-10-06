# Relecture humaine, latex (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/parse.ts` : reprise sur erreur, positions, commentaires et espaces après les commandes.
- [x] `src/render.ts` : numérotation et renvois, échappement, liens.
- [x] La liste des commandes et environnements pris en charge suffit-elle pour l'article ?
- [ ] `article/portfolio.fr.tex` et `portfolio.en.tex` : le contenu de l'article me convient-il ?
- [ ] `../../src/latexeditor/editor.js` : éditeur, diagnostics et plan.

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« confirme la review »). Le projet reste en cours : l'éditeur et l'article arrivent au sprint 14.

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/parse.ts` | (Claude) Seules les commandes inconnues faites d'un symbole étaient signalées, pas `\foo` | Avertissement aussi pour les commandes de lettres absentes de la table |
| 2026-10-06 | `src/render.ts` | (Claude) Détecté par un test : un `&` hors tableau disparaissait sans un mot | Rendu tel quel avec un avertissement qui propose `\&` |
| 2026-10-06 | `tests/render.test.ts` | (Claude) KaTeX 0.19 nomme la classe du numéro `katex-tag` (et non `tag`) ; mon test comptait zéro numéro | Classe corrigée |
| 2026-10-06 | `tests/render.test.ts` | (Claude) Test trop strict : `javascript:` apparaissait dans l'annotation MathML (source échappée), sans lien | Le test vérifie l'absence d'attribut `href="javascript:"` |
| 2026-10-06 | `src/render.ts` | (Claude) Détecté par axe : l'article ajoutait un second `h1` à la page | Option `headingLevel` ; l'éditeur rend le titre en `h3` et les sections en `h4` ; test |
| 2026-10-06 | `../../src/assets/style.css` | (Claude) Détecté par axe : liens de l'aperçu à 4,31:1 sur fond gris clair (thème clair) | Aperçu sur le fond de page |
| 2026-10-06 | `../../src/latexeditor/editor.js` | (Claude) Vu sur capture : le plan faisait défiler toute la page, sous l'en-tête fixe | Seul l'aperçu défile |
| 2026-10-06 | `../../tests/e2e/site.spec.js` | (Claude) Mon test lisait l'aperçu avant la fin du délai de 150 ms | Attente du redessin avant l'assertion |
| | | | |
