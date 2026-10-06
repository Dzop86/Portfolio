# Sprint 13 : lire et rendre du LaTeX en TypeScript

**Objectif :** le projet latex lit un sous-ensemble utile de LaTeX (structure, listes, tableaux, mathématiques, renvois, citations, notes) en un arbre dont chaque nœud connaît sa ligne et sa colonne, et le rend en HTML sûr, les formules par KaTeX ; les erreurs deviennent des diagnostics situés plutôt qu'un échec.

| Story | Points | État |
|---|---|---|
| En tant que développeur, j'analyse un document LaTeX (latex) en arbre situé : commandes, groupes, environnements, mathématiques, commentaires ; accolades et environnements mal fermés signalés à leur ligne et colonne ; tests node:test, typage strict, CI Linux, Windows et macOS. | 3 | À faire |
| En tant que lecteur, je vois le document (latex) rendu en HTML : sections numérotées et table des matières, KaTeX, équations numérotées, `\ref` et `\cite`, listes, tableaux, notes ; texte échappé, diagnostics pour l'inconnu ; tests. | 2 | À faire |

**Prévu au sprint 14 (3 points) :** l'éditeur en direct sur la fiche du projet (texte et rendu côte à côte, diagnostics cliquables, plan du document), et l'article qui présente le portfolio, en français et en anglais.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
