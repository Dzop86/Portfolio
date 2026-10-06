# othello : un Othello en C, de ses règles à son IA

[![othello](https://github.com/Dzop86/Portfolio/actions/workflows/othello.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/othello.yml)

Réécriture d'un jeu programmé pendant mes études : un moteur d'Othello en C11, exact et rapide, une IA minimax alpha-bêta et une partie dans le terminal. Il est jouable sur la [fiche du projet](https://dzop86.github.io/Portfolio/fr/project-othello.html), compilé en WebAssembly.

*Othello in C11: bitboard engine checked by perft against the published counts, alpha-beta AI, terminal game; Unity tests on Linux, Windows and macOS, with ASan and UBSan.*

## Le moteur (`src/board.c`)
- Le plateau est fait de deux entiers de 64 bits, un par couleur (case a1 = bit 0, h8 = bit 63). Les coups légaux se calculent pour les 64 cases à la fois, par décalages de bits dans les huit directions ; des masques empêchent un décalage de passer d'un bord à l'autre.
- Retournements, passe obligatoire (et seulement obligatoire), fin de partie quand aucun camp ne peut jouer, score, notation `a1` à `h8`.
- **Perft** : le nombre de positions atteignables à chaque profondeur depuis le départ est comparé aux valeurs publiées, jusqu'à 8 coups (390 216 positions). Retirer un seul masque de bord fait tomber le compte à la profondeur 7 (55 097 au lieu de 55 092) : vérifié.

## L'IA (`src/ai.c`)
- Negamax avec élagage alpha-bêta, profondeur réglable, coins essayés en premier.
- Évaluation : valeur des cases (coins précieux, cases voisines des coins dangereuses), mobilité, coins acquis ; une partie finie vaut la différence de pions, plus que toute heuristique.
- Déterministe : à valeur égale, la case la plus petite l'emporte.

## Dans le navigateur
`scripts/build-wasm.sh` compile moteur et IA en WebAssembly (Emscripten 6.0.11 dans Docker, 4,5 Ko) avec l'API de `tools/wasm_api.c` (partie et historique pour annuler) ; la CI vérifie que le build commité correspond aux sources. La page propose la couleur, le niveau (profondeur 1 à 6), l'annulation, montre les coups possibles et le dernier coup, annonce les passes ; elle se joue à la souris, au toucher et au clavier. Choix : D28 dans le [`DECISIONS.md` du site](../../DECISIONS.md).

## Jouer
```sh
cmake -S . -B build && cmake --build build
./build/othello                              # vous (noirs, X) contre l'IA (blancs, profondeur 4)
./build/othello --black ai:3 --white ai:6    # deux IA ; human pour une personne
printf 'd3\nc5\n' | ./build/othello          # coups lus sur l'entrée standard
```

## Tests
- **Plateau** (`tests/test_board.c`) : position de départ et ses quatre coups, retournements dans chaque direction, coups illégaux refusés sans rien changer, pas de débordement d'un bord à l'autre, passe et fin de partie, notation, perft jusqu'à 8.
- **IA** (`tests/test_ai.c`) : sur 40 positions et 4 profondeurs, alpha-bêta donne exactement la valeur du minimax en visitant moins de la moitié des positions ; coup légal et déterministe ; évaluation symétrique ; à un coup de profondeur, le coin libre est pris ; l'IA à profondeur 3 bat un joueur aléatoire au moins 18 fois sur 20.
- **Partie en terminal** (CTest) : deux IA jouent une partie complète ; une personne scriptée joue, se voit refuser un coup illégal, et l'entrée qui s'arrête avant la fin donne le code 1 ; un mauvais argument donne une erreur.
- **WebAssembly** (`tests/unit/othello-wasm.test.mjs` du site) : perft jusqu'à 7 et une partie complète entre deux IA, identiques au natif ; coup illégal refusé, annulation.
- **Page** (Playwright, 5 navigateurs, et 375 px) : coup du joueur et réponse de l'IA, clavier, annulation, partie avec les blancs, cases de 44 px au moins, accessibilité (axe) dans les deux thèmes.
- **CI** (`.github/workflows/othello.yml`) : Linux (GCC), Windows (MSVC) et macOS (Clang), avertissements traités en erreurs, plus une compilation ASan + UBSan, et la vérification du build WebAssembly.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
