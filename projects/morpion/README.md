# morpion : le morpion, en Python

[![morpion](https://github.com/Dzop86/Portfolio/actions/workflows/morpion.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/morpion.yml)

Réécriture d'un jeu de mes études : le morpion contre une IA qui ne perd jamais, en Python typé. Les tests le prouvent en jouant toutes les parties possibles contre elle.

*Tic-tac-toe against a minimax AI that never loses, in typed Python: beginner and unbeatable levels, French and English, a move book for the web page; pytest checks the AI against every possible game, on Linux, Windows and macOS.*

## Le jeu
- **Positions** : une chaîne de neuf cases (`"X"`, `"O"` ou `"."`), lue ligne par ligne. Immuable et hachable, elle sert de clé au cache de l'IA et au livre de coups.
- **Règles** (`board.py`) : les X commencent, coups légaux, gagnant, positions atteignables (5 478, comme le veut le résultat classique).
- **IA** (`ai.py`) : minimax sous sa forme negamax, sur tout l'arbre du jeu, mis en cache par position. Une victoire vaut 1 plus le nombre de cases encore vides : l'IA gagne le plus vite possible et, perdue, résiste le plus longtemps possible. Au niveau débutant, elle joue n'importe quel coup légal ; au niveau imbattable, l'un de ses meilleurs coups, tiré au hasard.
- **Livre de coups** (`book.py`, `data/book.json`) : pour chaque position atteignable, sa valeur et les meilleurs coups. C'est ce que lira la fiche du projet ; un test vérifie que le fichier commité est à jour.

## Lancer
Python 3.12 ou plus, sans dépendance :
```sh
python -m venv .venv && source .venv/bin/activate
pip install -e ".[test]"
morpion                                   # vous jouez les X, IA imbattable, en français
morpion --as O --level beginner --lang en # les O, IA débutante, en anglais
python -m morpion.book > data/book.json   # régénérer le livre de coups
pytest -q && mypy                         # tests et types
```

## Tests
- **pytest** (36 tests) : règles (lignes gagnantes, coups refusés, 5 478 positions dont 958 finales, accord avec une vérification brute des 3^9 grilles), IA (une partie parfaite est nulle, gain immédiat, parade, centre contre un coin, 255 168 parties dont 131 184 gagnées par X, 77 904 par O et 46 080 nulles), **IA jamais battue** : avec les X comme avec les O, contre tous les coups possibles de l'adversaire et pour chacun de ses meilleurs coups ; livre de coups complet et à jour ; partie complète dans le terminal, saisies invalides, options. Vérifié en cassant le code : si une victoire vaut toujours 1, l'IA ne gagne plus au plus vite et un test échoue.
- **mypy** en mode strict, sur le code et les tests.
- **CI** (`.github/workflows/morpion.yml`) : Python 3.12 et 3.13 sur Linux, Windows et macOS, avertissements traités en erreurs, puis une partie complète.

Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
