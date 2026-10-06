-- The analysis views on hand-made measurements whose answers are known in advance.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgtap;
SELECT plan(14);

-- Two campaigns; the second is slower on one file. Meshes 1001..1003 (torus, resolutions far from the real ones).
INSERT INTO campaign (run_at, runtime, os, arch, cpu, git_commit, warmup, repetitions) VALUES
  ('2001-01-01', 't', 't', 't', 't', 'aaaaaaa', 0, 5),
  ('2001-02-01', 't', 't', 't', 't', 'bbbbbbb', 0, 5);
INSERT INTO mesh (family, resolution, vertices, triangles, euler, boundary_loops)
SELECT 'torus', r, 2 * r * r, 4 * r * r, 0, 0 FROM generate_series(1001, 1003) r;
INSERT INTO file (mesh_id, format, bytes)
SELECT mesh_id, fmt, 1 FROM mesh, unnest(ARRAY['obj', 'ply']) fmt WHERE resolution > 1000;
CREATE TEMPORARY TABLE fixture AS
SELECT c.campaign_id AS c, c.run_at, f.file_id AS f, me.resolution, me.triangles, f.format, i.implementation_id AS i, i.name
FROM campaign c, file f JOIN mesh me USING (mesh_id), implementation i
WHERE c.run_at IN ('2001-01-01', '2001-02-01') AND me.resolution > 1000;

-- Durations proportional to the size (exactly linear), PLY twice as fast as OBJ, topologie 3 times lib-c.
-- Repetitions 1..5 add 0, 0, 0, 1, 100 % of the base: median = base, mean pulled up by the outlier.
INSERT INTO measurement (campaign_id, file_id, implementation_id, repetition, duration_ms)
SELECT c, f, i, rep,
       base * (CASE rep WHEN 4 THEN 1.01 WHEN 5 THEN 2 ELSE 1 END)
FROM (
  SELECT c, f, i, triangles * 1e-4
         * (CASE format WHEN 'obj' THEN 2 ELSE 1 END)
         * (CASE name WHEN 'topologie' THEN 3 ELSE 1 END)
         * (CASE WHEN run_at = '2001-02-01' AND resolution = 1002 AND format = 'obj' AND name = 'lib-c' THEN 1.5 ELSE 1 END)
         AS base
  FROM fixture
) b, generate_series(1, 5) rep;

CREATE TEMPORARY VIEW t AS
SELECT tm.* FROM timing tm JOIN fixture fx ON (tm.campaign_id, tm.file_id) = (fx.c, fx.f) AND tm.implementation = fx.name;
CREATE TEMPORARY VIEW first_campaign AS SELECT campaign_id FROM campaign WHERE run_at = '2001-01-01';

SELECT is((SELECT count(*) FROM t), 2 * 6 * 2::bigint, 'one timing row per campaign, file and implementation');
SELECT is((SELECT runs FROM t WHERE resolution = 1001 AND format = 'ply' AND implementation = 'lib-c' LIMIT 1), 5::bigint,
  'each row counts its five repetitions');
SELECT cmp_ok((SELECT abs(median_ms - 4 * 1001 * 1001 * 1e-4) FROM t
  WHERE resolution = 1001 AND format = 'ply' AND implementation = 'lib-c' AND campaign_id IN (SELECT * FROM first_campaign)),
  '<', 1e-9::double precision, 'the median ignores the outlier repetition');
SELECT cmp_ok((SELECT mean_ms / median_ms FROM t
  WHERE resolution = 1001 AND format = 'ply' AND implementation = 'lib-c' AND campaign_id IN (SELECT * FROM first_campaign)),
  '>', 1.2::double precision, 'the mean does not (it is 20 % higher)');
SELECT cmp_ok((SELECT abs(mtri_per_s - 4 * 1001 * 1001 / median_ms / 1000) FROM t
  WHERE resolution = 1001 AND format = 'ply' AND implementation = 'lib-c' AND campaign_id IN (SELECT * FROM first_campaign)),
  '<', 1e-9::double precision, 'throughput is triangles over the median');

SELECT results_eq($$
  SELECT format, rank, round(slowdown::numeric, 6) FROM format_ranking
  WHERE campaign_id IN (SELECT * FROM first_campaign) AND resolution = 1003 AND implementation = 'topologie'
  ORDER BY rank
$$, $$VALUES ('ply', 1::bigint, 1.000000), ('obj', 2::bigint, 2.000000)$$,
  'format_ranking puts PLY first and OBJ at twice its time');

SELECT is_empty($$
  SELECT * FROM scaling WHERE campaign_id IN (SELECT * FROM first_campaign) AND resolution > 1000
    AND (resolution = 1001) <> (local_exponent IS NULL)
$$, 'scaling has no previous size for the smallest mesh, and a value for the others');
SELECT is_empty($$
  SELECT * FROM scaling WHERE campaign_id IN (SELECT * FROM first_campaign) AND resolution > 1001
    AND abs(local_exponent - 1) > 1e-9
$$, 'time proportional to size gives a local exponent of 1');

SELECT is_empty($$
  SELECT * FROM complexity WHERE campaign_id IN (SELECT * FROM first_campaign) AND (abs(exponent - 1) > 1e-9 OR abs(r2 - 1) > 1e-9 OR points <> 3)
$$, 'complexity fits an exponent of 1 with R² = 1 on three sizes');

SELECT is_empty($$
  SELECT * FROM topology_overhead WHERE campaign_id IN (SELECT * FROM first_campaign) AND resolution > 1000 AND abs(ratio - 3) > 1e-9
$$, 'topology_overhead finds topologie 3 times slower than lib-c');

SELECT results_eq($$
  SELECT resolution, format, implementation, round(change::numeric, 6) FROM regressions(0.2)
  WHERE resolution > 1000
$$, $$VALUES (1002, 'obj', 'lib-c', 0.500000)$$, 'regressions finds the one file that got 50 % slower');
SELECT is((SELECT previous_campaign_id FROM regressions(0.2) WHERE resolution = 1002),
  (SELECT campaign_id FROM first_campaign), 'and compares it with the previous campaign');
SELECT is_empty($$SELECT * FROM regressions(0.6) WHERE resolution > 1000$$, 'a 50 % slowdown is under a 60 % threshold');
SELECT is((SELECT count(*) FROM regressions(-1) WHERE resolution > 1000), 2 * 6::bigint,
  'with no threshold every file of the second campaign is compared, none of the first');

SELECT * FROM finish();
ROLLBACK;
