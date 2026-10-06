# langage : Maille, un mini-langage fonctionnel

[![langage](https://github.com/Dzop86/Portfolio/actions/workflows/langage.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/langage.yml)

Maille est un petit langage fonctionnel à la syntaxe d'OCaml. Un **analyseur Flex/Bison en C** (`parser/`) lit le programme et produit son arbre syntaxique ; un **interpréteur OCaml** (`interp/`) en infère les types (Hindley-Milner) puis l'évalue.

*Small functional language: a Flex/Bison parser in C prints the syntax tree, an OCaml interpreter infers its types (Hindley-Milner, let-polymorphism) and evaluates it. Tested on Linux, Windows and macOS.*

```
let compose f g x = f (g x) in
let twice f = compose f f in
twice (twice (fun s -> s ^ "!")) "maille"
```
```
- : string = "maille!!!!"
```

## Le langage
- Valeurs : entiers (63 bits), flottants, booléens, chaînes ; fonctions de première classe, curryfiées.
- `let x = e in e`, `let f x y = e in e`, `let rec f n = e in e`, `fun x y -> e`, `if c then a else b`, application par juxtaposition `f x y`, commentaires `# ...`.
- Opérateurs, du moins au plus prioritaire : `||`, `&&`, comparaisons `== != < <= > >=` (non associatives, polymorphes), `^` (concaténation, à droite), `+ -` et `+. -.`, `* / %` et `*. /.`, `-` et `not` préfixes, application. Comme en OCaml, les flottants ont leurs propres opérateurs.
- Fonctions prédéfinies : `sqrt`, `float_of_int`, `int_of_float`, `string_of_int`, `string_of_float`, `string_length`.
- **Maillages** (type `mesh`) : `torus k`, `sphere k`, `cylinder k` (4k² triangles, k de 3 à 256, mêmes familles que le [benchmark SQL](../sql/)), `union a b` ; invariants `vertices`, `edges`, `faces`, `euler`, `boundary_loops`, `components`, `genus` (χ = 2c − 2g − b), calculés une fois au premier usage.
- Exemples commentés dans `examples/` : factorielle, Collatz, composition, polymorphisme, Newton, genre d'une scène, formule d'Euler, et une erreur de chaque sorte.

## L'analyseur (C, Flex, Bison)
- Lexer et parser réentrants (aucune variable globale), lisant un tampon en mémoire : prêts pour le WebAssembly du sprint 12.
- Priorités écrites comme une règle par niveau : la grammaire n'a aucun conflit, et Bison est lancé avec `-Wall -Werror`.
- Erreurs situées (ligne, colonne en caractères, UTF-8 compris) : `input:2:9: error: syntax error, unexpected in`. Erreurs lexicales nommées : caractère inattendu, chaîne non fermée, échappement inconnu, entier trop grand, nom en majuscule.
- Arbre en S-expression, chaque nœud avec sa position : `(binop 1:3 + (int 1:1 1) (int 1:5 2))`. `let f x y = e` et `fun x y -> e` deviennent des fonctions à un argument imbriquées.
- Profondeur bornée à 1 000 niveaux : toute récursion (affichage, libération, interpréteur) reste dans la pile de 64 Ko du WebAssembly.

## L'interpréteur (OCaml)
- Inférence Hindley-Milner par niveaux (algorithme de Rémy), polymorphisme du `let`, test d'occurrence : `fun x -> x x` est refusé (« the type would be infinite »).
- Messages à la manière d'OCaml, situés sur l'expression fautive : `2:8: type error: this expression has type string but an expression was expected of type int`.
- Évaluation par valeur, fermetures, `&&` et `||` paresseux. Deux budgets arrêtent un programme qui s'emballe : 10 millions d'étapes et 10 000 appels imbriqués. Division par zéro et comparaison de fonctions sont des erreurs d'exécution situées.
- Les flottants s'affichent avec le moins de chiffres qui les relisent à l'identique, comme dans l'analyseur.

## Lancer
```sh
cmake -S parser -B parser/build && cmake --build parser/build      # Flex et Bison 3.6+
ctest --test-dir parser/build --output-on-failure
dune test                                                         # depuis projects/langage
MAILLEC=$PWD/parser/build/maillec dune build @integration         # chaîne complète sur examples/
parser/build/maillec examples/compose.maille | dune exec interp/bin/main.exe -- -
```
Sous Windows, `choco install winflexbison3` fournit Flex et Bison ; sous macOS, `brew install bison flex` (le Bison du système est trop ancien).

## Tests
- **C** (`parser/tests/test_parser.c`) : littéraux, priorités et associativités, désucrage, erreurs et leur position, limites de profondeur ; **cas de référence** (`parser/tests/cases/`) comparés par CTest, arbre ou erreur.
- **OCaml** (`interp/test/test_maille.ml`) : lecteur de S-expressions, inférence (polymorphisme, composition, récursion, paramètre monomorphe, test d'occurrence), évaluation, budgets, affichage des flottants ; invariants de chaque famille comparés aux formules (V, E, F, χ, bords, composantes, genre) à plusieurs résolutions, orientation cohérente. Vérifié en cassant le code : sans généralisation du `let` ou sans test d'occurrence, les tests échouent.
- **Chaîne complète** (`interp/test/integration.ml`) : `maillec` puis l'interpréteur sur chaque `examples/*.maille`, résultat comparé à `*.out`.
- **CI** (`.github/workflows/langage.yml`) : analyseur sur Linux (GCC), Windows (MSVC, winflexbison) et macOS (Clang, Bison de Homebrew) ; ASan + UBSan et Valgrind sous Linux ; interpréteur et chaîne complète sur les trois systèmes, avec l'analyseur compilé sur chacun.

## Limites
- Pas de n-uplets, de listes ni de types définis par l'utilisateur.
- Le genre suppose une surface orientable (vrai pour les trois familles et leurs unions).
- Les entiers débordent comme en OCaml (arithmétique modulo 2⁶³), sans erreur.
- `-x` ne s'applique qu'aux entiers ; pour un flottant, `0.0 -. x`.
- `1 + if c then 1 else 2` demande des parenthèses : `let`, `fun` et `if` ne se placent qu'en tête d'expression.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
