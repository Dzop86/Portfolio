# Décisions, topologie

## T1. C++20 au-dessus de lib-c
**Choix :** le lecteur OBJ/PLY est celui de lib-c, intégré par `add_subdirectory(../lib-c)` ; le C++ ne refait que la structure et les calculs.
**Pourquoi :** fil rouge du portfolio, un seul lecteur fuzzé et testé ; la CI de topologie se relance quand lib-c change.
**Limite :** les deux projets doivent rester côte à côte dans le dépôt.

## T2. Demi-arêtes implicites par face
**Choix :** la demi-arête `h` appartient à la face `h / 3` et `next` reste dans la face ; seules `twin` et `flipped` sont calculées, par tri des clés d'arête (comme lib-c).
**Pourquoi :** pas de pointeurs, tableaux contigus, construction en O(T log T) ; les maillages sont déjà triangulés par lib-c.
**Limite :** une arête partagée par 3 faces ou plus n'a pas de jumelle ; elle est listée dans `non_manifold_edges()`. Les demi-arêtes de bord ne sont pas matérialisées.

## T3. Orientation incohérente conservée
**Choix :** deux faces qui parcourent une arête commune dans le même sens sont quand même jumelées, avec `flipped = true`.
**Pourquoi :** l'orientabilité (sprint 4, story 2) se décide justement en propageant ces parités ; refuser le maillage perdrait l'information.
