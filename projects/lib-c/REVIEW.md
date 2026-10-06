# Relecture humaine, lib-c (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `src/obj.c`, `reserve` : débordements de `capacity * size`, comportement quand `realloc` échoue.
- [ ] `src/obj.c`, `parse_corner` : indices négatifs, indice 0, très grands nombres.
- [ ] `src/obj.c` : la mémoire est-elle libérée sur tous les chemins d'erreur ? (ASan tourne en CI.)
- [ ] `tests/test_obj.c` : manque-t-il un cas OBJ courant (lignes `l`, faces de plus de 3 sommets avec textures) ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/obj.c` | (Claude) Indices lus en `long`, sur 32 bits sous Windows : un indice au-delà de 2^31 aurait débordé alors que les sommets sont indexés sur 32 bits non signés | `strtoll` et `long long` |
| 2026-10-06 | `src/obj.c` | (Claude) `reserve` prenait un `void **` obtenu par cast de `mesh_vec3 **` : violation de l'aliasing strict | `reserve` renvoie le nouveau pointeur |
| 2026-10-06 | `src/obj.c` | (Claude) Un `errno == ERANGE` sur `strtod` rejetait les coordonnées minuscules (sous-dépassement vers 0) mais acceptait `inf` et `nan` | Test `isfinite` sur la valeur lue |
| 2026-10-06 | `CMakeLists.txt` | (Claude) Détecté par les tests : `TEST_ASSERT_EQUAL_DOUBLE` échouait avec « Double Precision Disabled » | `UNITY_INCLUDE_DOUBLE` sur la cible `unity` |
| | | | |
