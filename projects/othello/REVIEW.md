# Relecture humaine, othello (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/board.c` : directions et masques de bord, passe, fin de partie.
- [x] `src/ai.c` : évaluation, alpha-bêta, départage.
- [x] Le jeu ressemble-t-il à celui de mes études ?
- [x] `../../src/assets/othelloplay.js` : déroulé d'une partie, passes, annulation.

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« accepte les reviews »). Case du sprint 18 (script de la partie) cochée le même jour, à sa demande (« valide les reviews »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `tests/test_ai.c` | (Claude) Mon test « le coin libre est pris » à profondeur 2 était faux : l'IA jouait e3 d'abord, à raison (les blancs doivent passer et ne peuvent pas prendre a1) | Test à un coup de profondeur, la raison écrite dans le test |
| 2026-10-06 | `tests/test_board.c` | (Claude) Un test d'abord écrit en tâtonnant (commentaires contradictoires) | Réécrit, chaque attendu vérifié à la main sur la position |
| 2026-10-06 | `../../src/assets/style.css` | (Claude) Détecté par Playwright : sur un écran de 390 px, les cases ne faisaient que 43,1 px (règle des 44 px) à cause de la colonne des coordonnées | Coordonnées masquées sous 560 px, plateau à la largeur de l'écran moins 8 px ; test à 375 px |
| 2026-10-06 | `../../src/assets/style.css` | (Claude) Deux couleurs d'ombre écrites en dur, contre la règle des jetons | Déplacées dans `tokens.css` |
| | | | |
