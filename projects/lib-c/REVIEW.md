# Relecture humaine, lib-c (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `src/obj.c`, `reserve` : débordements de `capacity * size`, comportement quand `realloc` échoue.
- [x] `src/obj.c`, `parse_corner` : indices négatifs, indice 0, très grands nombres.
- [x] `src/obj.c` : la mémoire est-elle libérée sur tous les chemins d'erreur ? (ASan tourne en CI.)
- [x] `src/ply.c`, `mesh_read_ply` : contrôle des comptes avant allocation, conversions `double` vers entiers.
- [x] `tests/test_obj.c` : manque-t-il un cas OBJ courant (lignes `l`, faces de plus de 3 sommets avec textures) ?

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« valide les relectures »).

- [x] Sprint 42 : `src/topology.c`, `mesh__sort_keys` (tri par base stable, histogrammes en une lecture, passes sautées) et les clés compactées ; `tests/test_topology.c`. Le tampon de la taille des clés te convient-il ?

> Case cochée par Claude le 8 octobre 2026, à la demande explicite de Charles (« j'ai review le sprint 42, c'est ok »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `src/obj.c` | (Claude) Indices lus en `long`, sur 32 bits sous Windows : un indice au-delà de 2^31 aurait débordé alors que les sommets sont indexés sur 32 bits non signés | `strtoll` et `long long` |
| 2026-10-06 | `src/obj.c` | (Claude) `reserve` prenait un `void **` obtenu par cast de `mesh_vec3 **` : violation de l'aliasing strict | `reserve` renvoie le nouveau pointeur |
| 2026-10-06 | `src/obj.c` | (Claude) Un `errno == ERANGE` sur `strtod` rejetait les coordonnées minuscules (sous-dépassement vers 0) mais acceptait `inf` et `nan` | Test `isfinite` sur la valeur lue |
| 2026-10-06 | `CMakeLists.txt` | (Claude) Détecté par les tests : `TEST_ASSERT_EQUAL_DOUBLE` échouait avec « Double Precision Disabled » | `UNITY_INCLUDE_DOUBLE` sur la cible `unity` |
| 2026-10-06 | `src/ply.c` | (Claude) Trouvé par le fuzzer : avec deux éléments `vertex`, le tableau était dimensionné sur le dernier mais rempli depuis le premier, d'où un débordement de tas | Seul le premier élément `vertex` compte, test de régression |
| 2026-10-06 | `src/ply.c` | (Claude) Trouvé par le fuzzer : un élément sans propriété annoncé avec 10 milliards d'items échappait au contrôle de taille et bouclait plusieurs minutes | Élément non vide sans propriété refusé, test |
| 2026-10-06 | `src/ply.c` | (Claude) Trouvé par le fuzzer (UBSan) : un compte de liste négatif (type `char` ou `int`) était converti en `size_t`, comportement indéfini | Compte négatif refusé, test ; `float-cast-overflow` ajouté à `MESH_SANITIZE`, car GCC ne l'active pas avec `undefined` |
| 2026-10-06 | `src/ply.c` | (Claude) Un octet NUL dans une ligne d'en-tête la tronquait silencieusement | Refusé comme erreur de syntaxe, test |
| 2026-10-06 | `src/stl.c` | (Claude) Détecté par LeakSanitizer : sur une ligne contenant un NUL, le tampon renvoyé par `realloc` était perdu avant la sortie de boucle | Le tampon est conservé avant tout autre test |
| 2026-10-06 | `src/stl.c` | (Claude) Détecté par un test : après soudure, les sommets sortaient dans l'ordre du tri et non du fichier | Numérotation par première apparition |
| 2026-10-06 | `src/obj.c` | (Claude) Un export ZBrush réel (Pikachu de Charles, gardé en local) était refusé à cause d'un octet NUL final | NUL et blancs de fin ignorés, test (C10) |
| 2026-10-08 | `src/topology.c` | (Claude) Constat du sprint 10 (`projects/sql/REVIEW.md`), décidé par Charles (D51) : `qsort` des clés d'arêtes coûtait deux fois la lecture | Tri par base : 1,7 à 1,9 fois plus rapide en natif sur tout le comptage, 1,6 fois sur la lecture complète en WebAssembly ; les 11 bits par chiffre gardés après essai de 8, 13 et 16 (mesures à ±10 %) |
| 2026-10-08 | `src/topology.c` | (Claude) Détecté en compilant le WebAssembly : `size_t` y fait 32 bits, la comparaison du nombre de sommets avec 2^32 est toujours fausse, et Clang la refuse en `-Werror` (rien en natif) | Comparaison en 64 bits |
| 2026-10-08 | `src/topology.c` | (Claude) Mutations : tri non stable, une passe en moins, mauvais saut de passe, copie finale oubliée, toutes attrapées. Survit, équivalente : une base de V − 1 au lieu de V (le petit sommet venant en premier, a·(V−1) + b reste injectif) | Rien à corriger |
| | | | |
