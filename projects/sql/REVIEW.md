# Relecture humaine, sql (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [ ] `schema/02_views.sql` : les vues répondent-elles aux questions annoncées (médiane, rang, exposant, régressions) ?
- [ ] `bench/run.mjs` : la mesure est-elle honnête (chauffe, répétitions, vérification avant enregistrement) ?
- [ ] `tests/pgtap/04_indexes.sql` : le test de plan est-il robuste ?

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `schema/01_schema.sql` | (Claude) Première contrainte d'Euler fausse : elle ne vérifiait que la parité de F, sans lien avec χ | χ = V − F/2 pour un maillage fermé, testée avec un contre-exemple |
| 2026-10-06 | `tests/pgtap/01_schema.sql` | (Claude) Le test de suppression en cascade relisait l'identifiant de la campagne après l'avoir supprimée : il passait toujours | Identifiants capturés dans une table temporaire avant la suppression |
| 2026-10-06 | `tests/pgtap/02_data.sql` | (Claude) Colonne `a.family` inexistante (elle est dans `mesh`) | Corrigé ; détecté au premier passage de `pg_prove` |
| 2026-10-06 | `.github/workflows/sql.yml` | (Claude) `pg_prove` sur un dossier ne trouvait aucun test (« NOTESTS ») | `--ext .sql -r` |
| 2026-10-06 | `../lib-c/src/topology.c` | (Claude) Trouvé par les mesures : le comptage d'arêtes par `qsort` coûte deux fois la lecture (45 ms contre 24 ms en natif sur 262 144 triangles) et rend lib-c plus lente que topologie en WebAssembly | Non corrigé dans ce sprint, à décider : tri par base (radix) ou table de hachage |
| | | | |
