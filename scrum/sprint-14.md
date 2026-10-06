# Sprint 14 : un éditeur LaTeX en direct, et l'article du portfolio

**Objectif :** sur la fiche du projet latex, le visiteur édite un article qui présente ce portfolio et voit le rendu se mettre à jour pendant qu'il tape, façon Overleaf ; les diagnostics et le plan le mènent à la bonne ligne.

| Story | Points | État |
|---|---|---|
| En tant que visiteur, j'édite un document (latex) et je vois son rendu en direct : texte et aperçu côte à côte (onglets sur mobile), diagnostics et plan cliquables, téléchargement du .tex ; tests Playwright, mobile compris. | 2 | Fait |
| En tant que recruteur, je lis l'article (latex) qui présente le portfolio, en français et en anglais : objectif, méthode, fil rouge des maillages avec ses formules, projets, références ; rendu sans aucun diagnostic, vérifié par un test. | 1 | Fait |

**Tests :** article sans diagnostic dans les deux langues (node:test) ; éditeur dans 5 navigateurs (Playwright, axe dans les deux thèmes). **Trouvé par les tests :** un second `h1` dans la page, un contraste de 4,31:1 en thème clair.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
