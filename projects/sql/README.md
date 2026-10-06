# sql : une base PostgreSQL pour les mesures de performance

[![sql](https://github.com/Dzop86/Portfolio/actions/workflows/sql.yml/badge.svg)](https://github.com/Dzop86/Portfolio/actions/workflows/sql.yml)

Base PostgreSQL qui range des **mesures réelles** de performance des bibliothèques du portfolio, [lib-c](../lib-c/) et [topologie](../topologie/) compilées en WebAssembly, et les analyse en SQL : vues, fonctions de fenêtrage, régression log-log, index justifié par le plan d'exécution.

*PostgreSQL database of real timings of the portfolio's mesh libraries (WebAssembly builds): constrained schema, window-function views, empirical complexity, regression check between campaigns; pgTAP tests in CI.*

## Les mesures
`bench/run.mjs` génère des tores, des cylindres ouverts et des sphères (de 1 024 à 262 144 triangles, `bench/meshes.mjs`), les écrit en OBJ, PLY binaire et STL binaire, et chronomètre chaque bibliothèque 7 fois après 2 passes de chauffe. Avant d'être enregistrée, chaque réponse est comparée aux invariants connus de la forme (χ, boucles de bord, orientation cohérente) : une mesure d'un résultat faux n'entre pas dans la base.

La campagne commitée (`data/measurements.csv`, 630 mesures) a été prise sous Node 22, Linux x64 (WSL 2), Intel Core i5-10400F. Les chiffres ne valent que pour cette machine ; le schéma accueille d'autres campagnes.

## Le schéma
- `campaign` (qui a mesuré, où, à quel commit), `mesh` (famille, taille, invariants), `file` (un maillage dans un format), `implementation`, `measurement` (une durée par répétition).
- Contraintes : durée positive et finie, formats et familles connus, hash de commit, χ = V − F/2 pour un maillage fermé ; un déclencheur refuse une répétition au-delà de ce que la campagne annonce.
- Chargement (`schema/04_load.sql`) : le CSV plat passe par une table temporaire, puis est normalisé en une transaction ; une bibliothèque inconnue fait échouer le chargement au lieu d'être ignorée.

## Les analyses (`schema/02_views.sql`)
| Vue | Question | Outils SQL |
|---|---|---|
| `timing` | Médiane, p90, moyenne, écart type, débit | `percentile_cont`, `stddev_samp` |
| `format_ranking` | Quel format se lit le plus vite ? | `rank() OVER`, `min() OVER` |
| `scaling` | Comment le temps grandit d'une taille à la suivante ? | `lag() OVER` |
| `complexity` | Exposant empirique (1 = linéaire) | `regr_slope`, `regr_r2` sur log-log |
| `topology_overhead` | Coût de topologie par rapport à lib-c | agrégats `FILTER` |
| `regressions(seuil)` | Qu'est-ce qui a ralenti depuis la campagne précédente ? | fonction SQL, `lag()` par fichier |

Ce que dit la campagne (`queries/examples.sql`) :
- Le **PLY binaire** se lit 2 à 6 fois plus vite que l'OBJ et le STL : pas de texte à analyser, et le STL répète chaque coin, que le lecteur doit souder.
- Les deux bibliothèques sont **linéaires** : exposant de 1,01 à 1,09, R² ≥ 0,99.
- **topologie va plus vite que lib-c**, alors qu'elle relit le même fichier avec le même lecteur et construit en plus les demi-arêtes et la courbure : 0,4 à 1,1 fois le temps de lib-c. La cause est le comptage d'arêtes de lib-c, qui trie trois clés par triangle avec `qsort` (un appel indirect par comparaison) : en natif, sur 262 144 triangles, 45 ms de tri pour 24 ms de lecture. Voir [`REVIEW.md`](REVIEW.md).

## Index (`schema/03_indexes.sql`)
La clé primaire de `measurement` commence par la campagne. L'historique d'un fichier d'une campagne à l'autre (ce que lit `regressions`) a son propre index, `measurement_file_history`. Sur les 630 lignes réelles, PostgreSQL préfère à juste titre lire toute la table ; le test `tests/pgtap/04_indexes.sql` ajoute donc 126 000 mesures fictives, vérifie que le plan passe par l'index, puis qu'il redevient un parcours complet sans lui (le tout annulé en fin de test).

## Lancer
```sh
node projects/sql/bench/run.mjs            # nouvelle campagne (environ 3 min), réécrit data/measurements.csv
docker build -t bench-db projects/sql      # PostgreSQL 17, schéma et données chargés au premier démarrage
docker run -d --name bench-db -e POSTGRES_PASSWORD=<à choisir> bench-db
docker exec bench-db pg_prove -U postgres -d bench --ext .sql -r /bench/tests/
docker exec -i bench-db psql -U postgres -d bench < projects/sql/queries/examples.sql
```

## Tests
- **Node** (`tests/node/bench.test.mjs`, 10 tests) : nombres de sommets et de triangles de chaque famille, indices valides, encodages déterministes et de la bonne taille, CSV relu à l'identique, invariants retrouvés par les deux bibliothèques dans les trois formats, vérification qui détecte une mauvaise réponse, campagne rapide complète.
- **pgTAP** (`tests/pgtap/`, 46 vérifications) : structure et clés, chaque contrainte et le déclencheur, cohérence des données chargées, chaque vue sur des mesures fabriquées à la main dont la réponse est connue, plan d'exécution avec et sans index. Vérifié en cassant le code : médiane remplacée par la moyenne, déclencheur supprimé, `regressions` qui compare à la campagne suivante, chaque fois des tests échouent.
- **CI** (`.github/workflows/sql.yml`) : script de mesure et tests Node sur Linux, Windows et macOS ; image PostgreSQL, tests pgTAP et requêtes d'exemple sous Linux.

## Limites
- Une seule machine, une seule campagne : la fonction `regressions` est testée sur des campagnes fabriquées, pas encore sur deux vraies.
- Mesures en WebAssembly sous Node, pas en natif : elles décrivent ce que vit la démo du site, pas les bibliothèques compilées en C et C++.
- La base PostgreSQL n'est pas en ligne ; la fiche du projet en proposera une copie SQLite interrogeable dans le navigateur (sprint 10, deuxième story).

Maillages générés par le code, aucune donnée de laboratoire. Relecture : [`REVIEW.md`](REVIEW.md), choix : [`DECISIONS.md`](DECISIONS.md).
