-- Loads the flat CSV written by bench/run.mjs into the normalised tables, in one transaction.
-- psql script: \copy reads the CSV from psql's standard input, so the server needs no access to the file.
--   psql -f schema/04_load.sql < data/measurements.csv
BEGIN;

CREATE TEMPORARY TABLE staging (
  run_at timestamptz, runtime text, os text, arch text, cpu text, git_commit text, warmup smallint,
  repetitions smallint, family text, resolution integer, vertices integer, triangles integer, euler integer,
  boundary_loops integer, format text, bytes integer, implementation text, repetition smallint,
  duration_ms double precision
) ON COMMIT DROP;

\copy staging FROM pstdin WITH (FORMAT csv, HEADER true)

INSERT INTO campaign (run_at, runtime, os, arch, cpu, git_commit, warmup, repetitions)
SELECT DISTINCT run_at, runtime, os, arch, cpu, git_commit, warmup, repetitions FROM staging
ON CONFLICT (run_at) DO NOTHING;

INSERT INTO mesh (family, resolution, vertices, triangles, euler, boundary_loops)
SELECT DISTINCT family, resolution, vertices, triangles, euler, boundary_loops FROM staging
ON CONFLICT (family, resolution) DO NOTHING;

INSERT INTO file (mesh_id, format, bytes)
SELECT DISTINCT me.mesh_id, s.format, s.bytes
FROM staging s JOIN mesh me USING (family, resolution)
ON CONFLICT (mesh_id, format) DO NOTHING;

-- An unknown implementation name leaves implementation_id NULL and fails on NOT NULL: nothing is
-- silently dropped.
INSERT INTO measurement (campaign_id, file_id, implementation_id, repetition, duration_ms)
SELECT c.campaign_id, f.file_id, i.implementation_id, s.repetition, s.duration_ms
FROM staging s
JOIN campaign c USING (run_at)
JOIN mesh me USING (family, resolution)
JOIN file f ON f.mesh_id = me.mesh_id AND f.format = s.format
LEFT JOIN implementation i ON i.name = s.implementation;

COMMIT;
