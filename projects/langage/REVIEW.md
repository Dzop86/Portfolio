# Relecture humaine, langage (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `parser/src/parser.y` : priorités, désucrage de `let f x y`, gestion mémoire sur erreur (`CHECK`, `%destructor`).
- [ ] `parser/src/lexer.l` : positions (colonnes en caractères), chaînes et échappements.
- [ ] `interp/lib/types.ml` : généralisation, test d'occurrence, messages d'erreur.
- [ ] `interp/lib/eval.ml` : fermetures de `let rec`, budgets.
- [ ] `interp/lib/mesh.ml` : invariants (bords, composantes, genre).
- [ ] `../../src/assets/mailleplay.js` : démo, arbre, positions dans l'éditeur.

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `parser/src/parser.y` | (Claude) Les opérateurs passaient par `strdup` sans vérifier l'échec | Chaînes constantes dans un membre `op` de l'union, sans allocation |
| 2026-10-06 | `parser/src/lexer.l` | (Claude) Détecté par un test : une erreur en fin de fichier pointait sur le dernier jeton (`1 +` en 1:3 au lieu de 1:4) | Règle `<<EOF>>` qui place la position après le dernier caractère |
| 2026-10-06 | `parser/src/ast.h` | (Claude) Profondeur bornée à 10 000 : les tests débordaient la pile sous ASan, et le WebAssembly (64 Ko de pile) n'aurait pas tenu | Borne à 1 000 (L3) |
| 2026-10-06 | `parser/tests/test_parser.c` | (Claude) Attendu faux dans mon propre test de `let rec` (colonne du paramètre) | Attendu corrigé, positions recomptées à la main |
| 2026-10-06 | `interp/lib/types.ml` | (Claude) Variables de type nommées à l'envers (`'b -> 'a`) : OCaml évalue les opérandes de `^` de droite à gauche | Gauche évaluée d'abord dans un `let` |
| 2026-10-06 | `interp/test/test_maille.ml` | (Claude) Positions des arbres de test données par un compteur, faussées par l'ordre d'évaluation d'OCaml | Positions vérifiées par la chaîne complète sur des S-expressions écrites à la main |
| 2026-10-06 | `parser/` (local) | (Claude) LeakSanitizer plante (boucle de `DEADLYSIGNAL`) sur certaines entrées dans le conteneur Docker sous WSL, au hasard selon les adresses ; Valgrind ne trouve ni fuite ni erreur sur les mêmes entrées | Laissé à la CI : ASan avec détection de fuites et Valgrind sous Linux standard |
| 2026-10-06 | `.github/workflows/langage.yml` | (Claude) Première CI : la chaîne complète échouait sur les trois systèmes avant de démarrer ; `ls` sur le motif Windows absent renvoyait 2, fatal sous `bash -o pipefail` | `find` à la place ; l'analyseur (3 OS, ASan avec détection de fuites, Valgrind) et `dune test` passaient déjà |
| 2026-10-06 | `interp/lib/eval.ml` | (Claude) CI Windows : `printf` y écrit trois chiffres d'exposant (`1e+020`) | Exposant normalisé à deux chiffres, ici et dans l'analyseur C ; tests |
| 2026-10-06 | `interp/lib/eval.ml` | (Claude) Détecté par le test de la version web : sous js_of_ocaml, l'`int` d'OCaml a 32 bits, `fact 20` donnait −2102132736 | Entiers `Int64` partout (littéraux, arithmétique, invariants) ; littéraux jusqu'à 2⁶³ − 1 (D22) |
| 2026-10-06 | `../../tests/unit/privacy.test.mjs` | (Claude) Le scan prenait la chaîne `"0123456789abcdef"` du runtime de js_of_ocaml pour un numéro de téléphone | Liste d'exceptions exacte (`0123456789`), testée : un vrai numéro à côté est toujours détecté |
| | | | |
