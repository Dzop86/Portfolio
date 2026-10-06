# Décisions, langage

## L1. Deux langages, un arbre en S-expression entre eux
**Choix :** l'analyseur C imprime l'arbre en S-expression, chaque nœud avec sa position ; l'interpréteur OCaml la relit.
**Pourquoi :** la pile annoncée (Flex/Bison et OCaml) impose deux langages ; un format texte les découple, se lit à l'œil, se compare dans les tests de référence, et servira tel quel au navigateur au sprint 12 (WebAssembly d'un côté, js_of_ocaml de l'autre).
**Alternatives :** liaison C-OCaml par FFI (plus fragile sur trois systèmes, et impossible entre WebAssembly et JavaScript sans pont), ocamllex et Menhir (tout en OCaml, mais hors de la pile du projet).

## L2. Analyseur réentrant, priorités en cascade
**Choix :** `%define api.pure full` et Flex `reentrant`, lecture d'un tampon ; une règle de grammaire par niveau de priorité plutôt que `%left` et `%prec`.
**Pourquoi :** aucun état global, donc plusieurs analyses possibles dans la même page ; une grammaire stratifiée n'a aucun conflit, ce que Bison vérifie avec `-Wall -Werror`.
**Limite :** `let`, `fun` et `if` ne sont acceptés qu'en tête d'expression (`1 + if ...` demande des parenthèses).

## L3. Profondeur d'arbre bornée à 1 000
**Choix :** un nœud plus profond que 1 000 niveaux est refusé à l'analyse, avec une erreur située.
**Pourquoi :** affichage, libération et interprétation sont récursifs ; la borne les garde dans les 64 Ko de pile du WebAssembly. Une première borne de 10 000 débordait déjà sous ASan avec la pile de 8 Mo (cadres plus gros).
**Alternatives :** parcours itératifs avec pile explicite (plus de code dans trois parcours, pour des programmes que personne n'écrit).

## L4. Inférence par niveaux, sans restriction de valeur
**Choix :** Hindley-Milner avec niveaux (Rémy) ; tout `let` est généralisé.
**Pourquoi :** la généralisation par niveaux évite de parcourir l'environnement ; le langage n'a pas de valeurs mutables, donc pas besoin de la restriction de valeur d'OCaml.

## L5. Budgets d'évaluation
**Choix :** 10 millions d'étapes et 10 000 appels imbriqués au plus, réglables (`Eval.limits`).
**Pourquoi :** un `let rec f x = f x` ne doit ni boucler ni faire tomber la page de démonstration ; l'erreur dit lequel des deux budgets est épuisé.
