# Sprint 11 : un mini-langage, de la grammaire aux types

**Objectif :** le projet langage définit « Maille », un petit langage fonctionnel : un analyseur Flex/Bison en C produit l'arbre syntaxique, un interpréteur OCaml en infère les types (Hindley-Milner) et l'évalue. Les maillages entreront dans le langage au sprint 12, avec la démo sur le site.

| Story | Points | État |
|---|---|---|
| En tant que développeur, j'analyse un programme Maille (langage) avec Flex et Bison en C : priorités, erreurs situées (ligne, colonne), arbre syntaxique en S-expression ; tests de référence CTest, CI Linux, Windows et macOS. | 3 | Fait |
| En tant que développeur, j'infère les types (Hindley-Milner, polymorphisme du let) et j'évalue l'arbre (langage) en OCaml ; tests dune et chaîne complète C puis OCaml, CI sur les trois systèmes. | 2 | Fait |

**Tests :** analyseur, 1 programme de tests unitaires C et 4 cas de référence (ASan, UBSan, Valgrind) ; interpréteur, tests dune ; chaîne complète sur 9 exemples ; CI sur Linux, Windows et macOS.

**Prévu au sprint 12 (3 points) :** les maillages dans le langage (tore, sphère, invariants), analyseur en WebAssembly et interpréteur en JavaScript (js_of_ocaml), arbre syntaxique affiché sur la fiche du projet.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
