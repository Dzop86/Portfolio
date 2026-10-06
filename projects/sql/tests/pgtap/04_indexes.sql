-- The history of one file must use measurement_file_history once the table is large; without the index
-- the same query scans the whole table. The real campaign is too small for the planner to bother with an
-- index (a few hundred rows fit in a few pages), so 200 synthetic campaigns are added, then rolled back.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgtap;
SELECT plan(4);

SELECT has_index('measurement', 'measurement_file_history', ARRAY['file_id', 'implementation_id', 'campaign_id']);

INSERT INTO campaign (run_at, runtime, os, arch, cpu, git_commit, warmup, repetitions)
SELECT timestamptz '1990-01-01' + n * interval '1 day', 'synthetic', 't', 't', 't', 'unknown', 0, 7
FROM generate_series(1, 200) n;
INSERT INTO measurement (campaign_id, file_id, implementation_id, repetition, duration_ms)
SELECT c.campaign_id, f.file_id, i.implementation_id, rep, 1 + random()
FROM campaign c, file f, implementation i, generate_series(1, 7) rep
WHERE c.runtime = 'synthetic';
ANALYZE measurement;

-- Plan of the history query, as text, for a file and implementation known to exist.
CREATE FUNCTION pg_temp.history_plan() RETURNS text LANGUAGE plpgsql AS $$
DECLARE
  f integer := (SELECT min(file_id) FROM file);
  i integer := (SELECT min(implementation_id) FROM implementation);
  line text;
  plan text := '';
BEGIN
  FOR line IN EXECUTE format(
    'EXPLAIN SELECT campaign_id, duration_ms FROM measurement WHERE file_id = %s AND implementation_id = %s', f, i)
  LOOP
    plan := plan || line || E'\n';
  END LOOP;
  RETURN plan;
END $$;

SELECT ok((SELECT count(*) FROM measurement) > 100000, 'the table holds more than 100 000 measurements');
SELECT matches(pg_temp.history_plan(), 'Index (Only )?Scan using measurement_file_history|Bitmap Index Scan on measurement_file_history',
  'the history of one file uses the index');

DROP INDEX measurement_file_history;
SELECT matches(pg_temp.history_plan(), 'Seq Scan on measurement', 'without the index it scans the whole table');

SELECT * FROM finish();
ROLLBACK;
