# Sprint 12 : des maillages dans le langage, et le langage dans le navigateur

**Objectif :** Maille manipule des maillages (tore, sphère, cylindre) et calcule leurs invariants topologiques ; sur la fiche du projet, le visiteur écrit un programme, voit son arbre syntaxique, son type et sa valeur, calculés dans son navigateur.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je construis des maillages en Maille (langage) et j'en calcule les invariants (sommets, arêtes, faces, caractéristique d'Euler, bords, genre) ; tests dune vérifiés contre les formules connues. | 1 | Fait |
| En tant que visiteur, j'exécute un programme Maille (langage) dans le navigateur : analyseur C en WebAssembly, interpréteur OCaml en JavaScript (js_of_ocaml), arbre syntaxique affiché ; tests Node et Playwright, mobile compris. | 2 | Fait |

**Tests :** invariants des trois familles contre les formules ; version web comparée à la chaîne native sur 11 exemples (18 tests Node) ; démo testée dans 5 navigateurs. **Trouvé :** entiers de 32 bits sous js_of_ocaml, exposants à trois chiffres sous Windows.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
