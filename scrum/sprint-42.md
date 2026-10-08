# Sprint 42 : le comptage d'arêtes de lib-c

**Objectif :** décision de Charles (D51) : corriger le goulot révélé par les mesures du projet SQL. lib-c compte les arêtes en triant trois clés par triangle avec `qsort`, un appel indirect par comparaison : 45 ms de tri pour 24 ms de lecture sur 262 144 triangles. Un tri par base (radix) en temps linéaire, mesuré avant et après, sans changer un seul résultat.

**Goal:** Charles's decision (D51): fix the bottleneck that the SQL project's measurements revealed. lib-c counts edges by sorting three keys per triangle with `qsort`, an indirect call per comparison: 45 ms of sorting for 24 ms of reading on 262,144 triangles. A linear-time radix sort, measured before and after, without changing a single result.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je compte les arêtes d'un gros maillage sans que le tri domine la lecture (lib-c) : tri par base des clés d'arêtes (clés compactées sur le nombre de sommets, chiffres de 11 bits, seulement les passes utiles), résultats identiques ; Unity (tri comparé à `qsort` sur des tableaux aléatoires, vides, d'une valeur, déjà triés, aux bornes ; comptes inchangés sur les maillages de test et des tores de toutes tailles) ; vérifié en cassant le code ; mesuré avant et après en natif et en WebAssembly ; ASan, Valgrind, fuzzing ; CI 3 OS. | 2 | Fait |

**Tests :** 3 cas Unity de plus (`tests/test_topology.c` : le tri comparé à `qsort` sur 7 tailles × 5 largeurs de clés, déjà trié et inversé ; doublons, toutes clés égales, bornes de 64 bits ; tores de 20 tailles, 3n² arêtes et χ = 0), 8 en tout, sous ASan et UBSan ; Valgrind (dans un conteneur, la machine n'en a pas) ; Clang 19 sans avertissement ; WebAssembly reconstruit (ceux de la topologie et du raytracer, qui compilent aussi lib-c, ne changent pas : le comptage n'y est pas appelé). Vérifié en cassant le code : quatre mutations attrapées, une équivalente. **Mesuré :** comptage complet sur 262 088 triangles, 35 → 18 à 20 ms en natif ; sur 1 048 352, 161 → 88 à 95 ms ; lecture complète de l'OBJ de 262 088 triangles en WebAssembly, 919 → 562 ms. **Trouvé en route :** en WebAssembly, `size_t` sur 32 bits rendait une comparaison toujours fausse, refusée par Clang.

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
