# Décisions, othello

## O1. Bitboards
**Choix :** deux masques de 64 bits ; coups et retournements par décalages dans les huit directions.
**Pourquoi :** tous les coups légaux d'une position se calculent en une cinquantaine d'opérations, ce qui rend l'IA rapide, même en WebAssembly ; une erreur de bord se voit immédiatement dans perft.
**Alternatives :** un tableau de 64 cases parcouru case par case (plus lisible, dix fois plus lent).

## O2. Perft comme référence
**Choix :** le générateur de coups est validé en comptant les positions atteignables (passes comptées comme des coups) et en comparant aux valeurs publiées.
**Pourquoi :** quelques positions écrites à la main ne couvrent pas tous les cas ; 390 216 positions en couvrent beaucoup, et la moindre erreur change le compte.

## O3. Negamax alpha-bêta, déterministe
**Choix :** la racine cherche chaque coup avec une fenêtre complète et départage par la case la plus petite ; en dessous, alpha-bêta ordonné (coins d'abord).
**Pourquoi :** le même coup pour la même position, sur toute machine : les tests et la démo sont reproductibles.
**Limite :** pas de table de transposition ni d'approfondissement itératif ; la profondeur est fixe.
