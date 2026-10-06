# Relecture humaine, othello (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/board.c` : directions et masques de bord, passe, fin de partie.
- [x] `src/ai.c` : évaluation, alpha-bêta, départage.
- [x] Le jeu ressemble-t-il à celui de mes études ?

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« accepte les reviews »). Le projet reste en cours : le plateau jouable arrive au sprint 18.

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `tests/test_ai.c` | (Claude) Mon test « le coin libre est pris » à profondeur 2 était faux : l'IA jouait e3 d'abord, à raison (les blancs doivent passer et ne peuvent pas prendre a1) | Test à un coup de profondeur, la raison écrite dans le test |
| 2026-10-06 | `tests/test_board.c` | (Claude) Un test d'abord écrit en tâtonnant (commentaires contradictoires) | Réécrit, chaque attendu vérifié à la main sur la position |
| | | | |
