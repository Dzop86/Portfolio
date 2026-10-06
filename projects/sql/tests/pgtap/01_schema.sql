-- Structure and constraints: what the schema must refuse. Rolled back, so the loaded data is untouched.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgtap;
SELECT plan(20);

SELECT tables_are('public', ARRAY['campaign', 'mesh', 'file', 'implementation', 'measurement']);
SELECT col_is_pk('measurement', ARRAY['campaign_id', 'file_id', 'implementation_id', 'repetition']);
SELECT fk_ok('measurement', 'campaign_id', 'campaign', 'campaign_id');
SELECT fk_ok('measurement', 'file_id', 'file', 'file_id');
SELECT fk_ok('measurement', 'implementation_id', 'implementation', 'implementation_id');
SELECT fk_ok('file', 'mesh_id', 'mesh', 'mesh_id');
SELECT views_are('public', ARRAY['timing', 'format_ranking', 'scaling', 'complexity', 'topology_overhead']);
SELECT has_function('regressions', ARRAY['double precision']);

-- Fixtures: one campaign announcing 3 repetitions, one closed mesh and its OBJ file.
INSERT INTO campaign (run_at, runtime, os, arch, cpu, git_commit, warmup, repetitions)
VALUES ('2000-01-01', 'test', 'test', 'test', 'test', 'abcdef0', 0, 3);
INSERT INTO mesh (family, resolution, vertices, triangles, euler, boundary_loops) VALUES ('torus', 999, 8, 16, 0, 0);
INSERT INTO file (mesh_id, format, bytes) SELECT mesh_id, 'obj', 100 FROM mesh WHERE resolution = 999;
CREATE TEMPORARY TABLE ids AS
SELECT (SELECT campaign_id FROM campaign WHERE run_at = '2000-01-01') AS c,
       (SELECT file_id FROM file JOIN mesh USING (mesh_id) WHERE resolution = 999) AS f,
       (SELECT implementation_id FROM implementation WHERE name = 'lib-c') AS i;

SELECT lives_ok($$INSERT INTO measurement SELECT c, f, i, 1, 0.5 FROM ids$$, 'a valid measurement is accepted');
SELECT throws_ok($$INSERT INTO measurement SELECT c, f, i, 1, 0.7 FROM ids$$, '23505', NULL,
  'the same repetition cannot be recorded twice');
SELECT throws_ok($$INSERT INTO measurement SELECT c, f, i, 2, 0 FROM ids$$, '23514', NULL,
  'a duration must be positive');
SELECT throws_ok($$INSERT INTO measurement SELECT c, f, i, 2, 'Infinity' FROM ids$$, '23514', NULL,
  'a duration must be finite');
SELECT throws_ok($$INSERT INTO measurement SELECT c, f, i, 4, 1 FROM ids$$, '23514',
  'repetition 4 exceeds the repetitions of campaign ' || (SELECT c FROM ids),
  'a repetition cannot exceed what its campaign announced');
SELECT throws_ok($$INSERT INTO measurement SELECT c, f, -1, 2, 1 FROM ids$$, '23503', NULL,
  'an unknown implementation is refused');
SELECT throws_ok($$INSERT INTO file (mesh_id, format, bytes) SELECT mesh_id, 'gltf', 1 FROM mesh WHERE resolution = 999$$,
  '23514', NULL, 'only OBJ, PLY and STL files are measured');
SELECT throws_ok($$INSERT INTO campaign (run_at, runtime, os, arch, cpu, git_commit, warmup, repetitions)
  VALUES ('2000-01-02', 't', 't', 't', 't', 'main', 0, 1)$$, '23514', NULL, 'a commit is a hexadecimal hash');
SELECT throws_ok($$INSERT INTO mesh (family, resolution, vertices, triangles, euler, boundary_loops)
  VALUES ('sphere', 998, 10, 16, 0, 0)$$, '23514', NULL,
  'a closed mesh must satisfy χ = V - F / 2 (here 10 - 8 = 2, not 0)');
SELECT lives_ok($$INSERT INTO mesh (family, resolution, vertices, triangles, euler, boundary_loops)
  VALUES ('cylinder', 998, 12, 16, 0, 2)$$, 'an open mesh is not held to the closed-surface formula');

DELETE FROM campaign WHERE run_at = '2000-01-01';
SELECT is((SELECT count(*) FROM measurement JOIN ids ON campaign_id = c), 0::bigint,
  'deleting a campaign deletes its measurements');
SELECT throws_ok($$DELETE FROM implementation WHERE name = 'lib-c'$$, '23503', NULL,
  'an implementation with measurements cannot be deleted');

SELECT * FROM finish();
ROLLBACK;
