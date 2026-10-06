-- The loaded campaign is complete and its meshes have the invariants of their family.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgtap;
SELECT plan(8);

SELECT ok((SELECT count(*) FROM campaign) >= 1, 'at least one campaign is loaded');
SELECT set_eq('SELECT DISTINCT family FROM mesh', ARRAY['torus', 'cylinder', 'sphere'], 'three mesh families');
SELECT is_empty($$
  SELECT me.family, me.resolution FROM mesh me
  WHERE (SELECT count(*) FROM file f WHERE f.mesh_id = me.mesh_id) <> 3
$$, 'every mesh is measured in its three formats');
SELECT is_empty($$
  SELECT c.campaign_id, f.file_id, i.name, count(m.repetition)
  FROM campaign c CROSS JOIN file f CROSS JOIN implementation i
  LEFT JOIN measurement m ON (m.campaign_id, m.file_id, m.implementation_id) = (c.campaign_id, f.file_id, i.implementation_id)
  GROUP BY c.campaign_id, f.file_id, i.name, c.repetitions
  HAVING count(m.repetition) NOT IN (0, c.repetitions)
$$, 'a file measured by an implementation has all the repetitions of its campaign');
SELECT is_empty($$SELECT * FROM mesh WHERE triangles <> 4 * resolution * resolution$$,
  'every family has 4 k² triangles at resolution k');
SELECT is_empty($$
  SELECT * FROM mesh WHERE (family, euler, boundary_loops) NOT IN (('torus', 0, 0), ('cylinder', 0, 2), ('sphere', 2, 0))
$$, 'torus and cylinder have χ = 0, the sphere χ = 2; only the cylinder has a boundary (two loops)');
SELECT is_empty($$
  SELECT mesh.family, mesh.resolution FROM file a JOIN file b USING (mesh_id) JOIN mesh USING (mesh_id)
  WHERE a.format = 'stl' AND b.format = 'ply' AND a.bytes <= b.bytes
$$, 'binary STL repeats each corner, so it is larger than binary PLY');
SELECT is_empty($$SELECT * FROM measurement WHERE duration_ms > 60000$$, 'no measurement took more than a minute');

SELECT * FROM finish();
ROLLBACK;
