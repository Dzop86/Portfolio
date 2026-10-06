# Relecture humaine, bataille (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/war.adb` : règles du pli et de la bataille, fin de partie.
- [x] `src/cards.ads` : types et contrats des paquets.
- [x] Les règles sont-elles celles du jeu de mes études ?

> Cases cochées par Claude le 7 octobre 2026, à la demande explicite de Charles (« valide relecture, review »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-07 | `src/cards.adb` | (Claude) `Image` allouait de nouvelles chaînes à chaque appel (fuite mémoire) | Expressions `case` sans allocation |
| 2026-10-07 | `src/war.adb` | (Claude) Détecté par les vérifications d'exécution d'Ada : l'indice du mélange convertissait 0 en `Positive` | Conversion en `Natural` avant d'ajouter 1 |
| 2026-10-07 | `src/bataille.adb` | (Claude) Un gestionnaire de `Constraint_Error` sur tout le programme faisait passer cette erreur pour une erreur d'arguments | Gestionnaire limité à la lecture des arguments |
| 2026-10-07 | `src/stats.adb` | (Claude) Détecté par le contrôle de débordement : sur 100 000 parties, le total des plis fois 100 dépassait 32 bits | Totaux sur 64 bits, test de grands totaux |
| 2026-10-07 | `src/bataille.adb` | (Claude) Symboles de couleur encodés deux fois en UTF-8 par `Text_IO` | Sortie écrite telle quelle (B3) |
| 2026-10-07 | `../../src/templates.mjs` | (Claude) Détecté par axe : le bloc de commandes défilait sur mobile sans être atteignable au clavier (ici et sur la fiche de la bataille navale) | Bloc focalisable |
| | | | |
