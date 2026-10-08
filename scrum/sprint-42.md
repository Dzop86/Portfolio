# Sprint 42 : le comptage d'arêtes de lib-c

**Objectif :** décision de Charles (D51) : corriger le goulot révélé par les mesures du projet SQL. lib-c compte les arêtes en triant trois clés par triangle avec `qsort`, un appel indirect par comparaison : 45 ms de tri pour 24 ms de lecture sur 262 144 triangles. Un tri par base (radix) en temps linéaire, mesuré avant et après, sans changer un seul résultat.

**Goal:** Charles's decision (D51): fix the bottleneck that the SQL project's measurements revealed. lib-c counts edges by sorting three keys per triangle with `qsort`, an indirect call per comparison: 45 ms of sorting for 24 ms of reading on 262,144 triangles. A linear-time radix sort, measured before and after, without changing a single result.

| Story | Points | État |
|---|---|---|
| En tant que développeur, je compte les arêtes d'un gros maillage sans que le tri domine la lecture (lib-c) : tri par base des clés d'arêtes (clés compactées sur le nombre de sommets, chiffres de 11 bits, seulement les passes utiles), résultats identiques ; Unity (tri comparé à `qsort` sur des tableaux aléatoires, vides, d'une valeur, déjà triés, aux bornes ; comptes inchangés sur les maillages de test et des tores de toutes tailles) ; vérifié en cassant le code ; mesuré avant et après en natif et en WebAssembly ; ASan, Valgrind, fuzzing ; CI 3 OS. | 2 | À faire |

## Rétro (à compléter par Charles)
- Ce qui a marché :
- Ce que l'IA a mal fait :
- À changer au prochain sprint :
