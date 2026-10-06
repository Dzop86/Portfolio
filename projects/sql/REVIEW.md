# Relecture humaine, sql (Charles)

Ce fichier est le mien : j'y note ce que j'ai vérifié et corrigé dans le code généré.
Claude y ajoute aussi ses constats au fil de l'eau, préfixés « (Claude) » ; les cases à cocher restent les miennes.

## Points à relire en priorité
- [x] `schema/02_views.sql` : les vues répondent-elles aux questions annoncées (médiane, rang, exposant, régressions) ?
- [x] `bench/run.mjs` : la mesure est-elle honnête (chauffe, répétitions, vérification avant enregistrement) ?
- [x] `tests/pgtap/04_indexes.sql` : le test de plan est-il robuste ?
- [x] `../../src/assets/sqlplay.js` : arrêt d'une requête trop longue, rechargement, messages d'erreur.

> Cases cochées par Claude le 6 octobre 2026, à la demande explicite de Charles (« valide le sprint 10 du sql, review ok »).

## Constats

| Date | Fichier | Problème trouvé | Correction |
|---|---|---|---|
| 2026-10-06 | `schema/01_schema.sql` | (Claude) Première contrainte d'Euler fausse : elle ne vérifiait que la parité de F, sans lien avec χ | χ = V − F/2 pour un maillage fermé, testée avec un contre-exemple |
| 2026-10-06 | `tests/pgtap/01_schema.sql` | (Claude) Le test de suppression en cascade relisait l'identifiant de la campagne après l'avoir supprimée : il passait toujours | Identifiants capturés dans une table temporaire avant la suppression |
| 2026-10-06 | `tests/pgtap/02_data.sql` | (Claude) Colonne `a.family` inexistante (elle est dans `mesh`) | Corrigé ; détecté au premier passage de `pg_prove` |
| 2026-10-06 | `.github/workflows/sql.yml` | (Claude) `pg_prove` sur un dossier ne trouvait aucun test (« NOTESTS ») | `--ext .sql -r` |
| 2026-10-06 | `../lib-c/src/topology.c` | (Claude) Trouvé par les mesures : le comptage d'arêtes par `qsort` coûte deux fois la lecture (45 ms contre 24 ms en natif sur 262 144 triangles) et rend lib-c plus lente que topologie en WebAssembly | Non corrigé dans ce sprint, à décider : tri par base (radix) ou table de hachage |
| 2026-10-06 | `sqlite/schema.sql` | (Claude) Médiane fausse dans la copie SQLite : `count(*) OVER` avec un `ORDER BY` compte de façon cumulée (cadre par défaut), donc « la ligne du milieu » glissait ; vu en comparant aux médianes de PostgreSQL (458,6 contre 474,9 ms) | Fenêtre sans `ORDER BY` pour le compte ; test qui compare chaque médiane à celle calculée en JavaScript (il échoue si l'on remet le bogue, vérifié) |
| 2026-10-06 | `../../Dockerfile` | (Claude) L'image du site ne copiait pas les fichiers de `projects/sql` dont le build a besoin | Trois `COPY` ajoutés ; image construite et fichiers servis vérifiés |
| 2026-10-06 | `../../tests/e2e/site.spec.js` | (Claude) Le test du bac à sable dépassait 30 s sous Firefox (trois chargements de la base et 5 s d'attente voulue) | Délai du test porté à 60 s |
| | | | |
