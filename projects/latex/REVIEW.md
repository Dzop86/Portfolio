# Relecture humaine, latex (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/parse.ts` : reprise sur erreur, positions, commentaires et espaces après les commandes.
- [ ] `src/render.ts` : numérotation et renvois, échappement, liens.
- [ ] La liste des commandes et environnements pris en charge suffit-elle pour l'article ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/parse.ts` | (Claude) Seules les commandes inconnues faites d'un symbole étaient signalées, pas `\foo` | Avertissement aussi pour les commandes de lettres absentes de la table |
| 2026-10-06 | `src/render.ts` | (Claude) Détecté par un test : un `&` hors tableau disparaissait sans un mot | Rendu tel quel avec un avertissement qui propose `\&` |
| 2026-10-06 | `tests/render.test.ts` | (Claude) KaTeX 0.19 nomme la classe du numéro `katex-tag` (et non `tag`) ; mon test comptait zéro numéro | Classe corrigée |
| 2026-10-06 | `tests/render.test.ts` | (Claude) Test trop strict : `javascript:` apparaissait dans l'annotation MathML (source échappée), sans lien | Le test vérifie l'absence d'attribut `href="javascript:"` |
| | | | |
