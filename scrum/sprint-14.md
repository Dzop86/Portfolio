# Sprint 14 : un éditeur LaTeX en direct, et l'article du portfolio

**Objectif :** sur la fiche du projet latex, le visiteur édite un article qui présente ce portfolio et voit le rendu se mettre à jour pendant qu'il tape, façon Overleaf ; les diagnostics et le plan le mènent à la bonne ligne.

**Goal:** on the latex project page, the visitor edits an article presenting this portfolio and sees the rendering update while typing, as in Overleaf; diagnostics and the outline lead to the right line.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, j'édite un document (latex) et je vois son rendu en direct : texte et aperçu côte à côte (onglets sur mobile), diagnostics et plan cliquables, téléchargement du .tex ; tests Playwright, mobile compris. | 2 | Fait |
| En tant que recruteur, je lis l'article (latex) qui présente le portfolio, en français et en anglais : objectif, méthode, fil rouge des maillages avec ses formules, projets, références ; rendu sans aucun diagnostic, vérifié par un test. | 1 | Fait |

**Tests :** article sans diagnostic dans les deux langues (node:test) ; éditeur dans 5 navigateurs (Playwright, axe dans les deux thèmes). **Trouvé par les tests :** un second `h1` dans la page, un contraste de 4,31:1 en thème clair.

## Rétro (Charles)
- Ce qui a marché : L'éditeur rend en direct et l'article est sans diagnostic dans les deux langues.
- Ce que l'IA a mal fait : Un second `h1` dans la page et un contraste de 4,31:1 en thème clair, attrapés par axe.
- À changer au prochain sprint : Passer axe dans les deux thèmes avant de livrer.
