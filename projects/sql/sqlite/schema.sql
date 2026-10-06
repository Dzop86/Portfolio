-- SQLite copy of the benchmark database, for the in-browser playground (sql.js). Same tables and
-- constraints as schema/01_schema.sql, in SQLite's dialect. SQLite has no percentile_cont, regr_slope
-- or stored functions: the median comes from window functions, the complexity fit from the
-- least-squares sums, and ln() is supplied by JavaScript (src/sqlplay/core.js).

CREATE TABLE campaign (
  campaign_id  INTEGER PRIMARY KEY,
  run_at       TEXT NOT NULL UNIQUE,
  runtime      TEXT NOT NULL,
  os           TEXT NOT NULL,
  arch         TEXT NOT NULL,
  cpu          TEXT NOT NULL,
  git_commit   TEXT NOT NULL CHECK (git_commit = 'unknown' OR (length(git_commit) BETWEEN 7 AND 40 AND git_commit NOT GLOB '*[^0-9a-f]*')),
  warmup       INTEGER NOT NULL CHECK (warmup >= 0),
  repetitions  INTEGER NOT NULL CHECK (repetitions >= 1)
) STRICT;

CREATE TABLE mesh (
  mesh_id         INTEGER PRIMARY KEY,
  family          TEXT NOT NULL CHECK (family IN ('torus', 'cylinder', 'sphere')),
  resolution      INTEGER NOT NULL CHECK (resolution >= 3),
  vertices        INTEGER NOT NULL CHECK (vertices > 0),
  triangles       INTEGER NOT NULL CHECK (triangles > 0),
  euler           INTEGER NOT NULL,
  boundary_loops  INTEGER NOT NULL CHECK (boundary_loops >= 0),
  UNIQUE (family, resolution),
  CHECK (boundary_loops > 0 OR (triangles % 2 = 0 AND euler = vertices - triangles / 2))
) STRICT;

CREATE TABLE file (
  file_id  INTEGER PRIMARY KEY,
  mesh_id  INTEGER NOT NULL REFERENCES mesh ON DELETE CASCADE,
  format   TEXT NOT NULL CHECK (format IN ('obj', 'ply', 'stl')),
  bytes    INTEGER NOT NULL CHECK (bytes > 0),
  UNIQUE (mesh_id, format)
) STRICT;

CREATE TABLE implementation (
  implementation_id  INTEGER PRIMARY KEY,
  name               TEXT NOT NULL UNIQUE,
  language           TEXT NOT NULL,
  work               TEXT NOT NULL
) STRICT;

CREATE TABLE measurement (
  campaign_id        INTEGER NOT NULL REFERENCES campaign ON DELETE CASCADE,
  file_id            INTEGER NOT NULL REFERENCES file ON DELETE CASCADE,
  implementation_id  INTEGER NOT NULL REFERENCES implementation,
  repetition         INTEGER NOT NULL CHECK (repetition >= 1),
  duration_ms        REAL NOT NULL CHECK (duration_ms > 0 AND duration_ms < 9e999),
  PRIMARY KEY (campaign_id, file_id, implementation_id, repetition)
) STRICT;

CREATE TRIGGER measurement_repetition BEFORE INSERT ON measurement
WHEN NEW.repetition > (SELECT repetitions FROM campaign WHERE campaign_id = NEW.campaign_id)
BEGIN
  SELECT RAISE(ABORT, 'repetition exceeds the repetitions of its campaign');
END;

CREATE INDEX measurement_file_history ON measurement (file_id, implementation_id, campaign_id);

INSERT INTO implementation (name, language, work) VALUES
  ('lib-c', 'C11 (WebAssembly)', 'read the file, count edges and boundary edges'),
  ('topologie', 'C++20 (WebAssembly)', 'read the file, build the half-edge structure, invariants and curvature');

-- Median: the middle row (or the mean of the two middle rows) of each group, numbered by a window function.
CREATE VIEW timing AS
WITH ranked AS (
  SELECT m.*,
         row_number() OVER (g ORDER BY duration_ms) AS rn,
         -- No ORDER BY here: with one, the default frame stops at the current row and count(*) runs.
         count(*) OVER g AS n
  FROM measurement m
  WINDOW g AS (PARTITION BY campaign_id, file_id, implementation_id)
)
SELECT r.campaign_id, f.file_id, f.mesh_id, me.family, me.resolution, me.triangles, f.format, f.bytes,
       i.name AS implementation,
       count(*)                                                     AS runs,
       avg(r.duration_ms) FILTER (WHERE r.rn IN ((r.n + 1) / 2, (r.n + 2) / 2)) AS median_ms,
       min(r.duration_ms)                                           AS min_ms,
       avg(r.duration_ms)                                           AS mean_ms,
       sqrt((sum(r.duration_ms * r.duration_ms) - sum(r.duration_ms) * avg(r.duration_ms)) / (count(*) - 1)) AS stddev_ms,
       me.triangles / (avg(r.duration_ms) FILTER (WHERE r.rn IN ((r.n + 1) / 2, (r.n + 2) / 2))) / 1000 AS mtri_per_s
FROM ranked r
JOIN file f USING (file_id)
JOIN mesh me USING (mesh_id)
JOIN implementation i USING (implementation_id)
GROUP BY r.campaign_id, f.file_id, i.implementation_id;

CREATE VIEW format_ranking AS
SELECT campaign_id, family, resolution, triangles, implementation, format, median_ms,
       rank() OVER w                      AS rank,
       median_ms / min(median_ms) OVER w  AS slowdown
FROM timing
WINDOW w AS (PARTITION BY campaign_id, mesh_id, implementation ORDER BY median_ms);

CREATE VIEW scaling AS
SELECT campaign_id, family, format, implementation, resolution, triangles, median_ms,
       triangles * 1.0 / lag(triangles) OVER w                                  AS size_ratio,
       median_ms / lag(median_ms) OVER w                                        AS time_ratio,
       ln(median_ms / lag(median_ms) OVER w) / ln(triangles * 1.0 / lag(triangles) OVER w) AS local_exponent
FROM timing
WINDOW w AS (PARTITION BY campaign_id, family, format, implementation ORDER BY triangles);

-- Least-squares slope and R² of ln(time) against ln(size).
CREATE VIEW complexity AS
WITH pts AS (SELECT campaign_id, implementation, format, ln(triangles) AS x, ln(median_ms) AS y FROM timing),
sums AS (
  SELECT campaign_id, implementation, format, count(*) AS n, sum(x) AS sx, sum(y) AS sy,
         sum(x * x) AS sxx, sum(y * y) AS syy, sum(x * y) AS sxy
  FROM pts GROUP BY campaign_id, implementation, format
)
SELECT campaign_id, implementation, format,
       (n * sxy - sx * sy) / (n * sxx - sx * sx) AS exponent,
       (n * sxy - sx * sy) * (n * sxy - sx * sy) / ((n * sxx - sx * sx) * (n * syy - sy * sy)) AS r2,
       n AS points
FROM sums;

CREATE VIEW topology_overhead AS
SELECT campaign_id, family, resolution, triangles, format,
       max(median_ms) FILTER (WHERE implementation = 'lib-c')     AS libc_ms,
       max(median_ms) FILTER (WHERE implementation = 'topologie') AS topologie_ms,
       max(median_ms) FILTER (WHERE implementation = 'topologie')
         / max(median_ms) FILTER (WHERE implementation = 'lib-c') AS ratio
FROM timing
GROUP BY campaign_id, file_id;

-- What regressions(threshold) returns in PostgreSQL, without the filter: add WHERE change > 0.2.
CREATE VIEW campaign_change AS
SELECT * FROM (
  SELECT t.campaign_id,
         lag(t.campaign_id) OVER w AS previous_campaign_id,
         t.family, t.resolution, t.format, t.implementation,
         lag(t.median_ms) OVER w AS previous_ms,
         t.median_ms,
         t.median_ms / lag(t.median_ms) OVER w - 1 AS change
  FROM timing t JOIN campaign c USING (campaign_id)
  WINDOW w AS (PARTITION BY t.file_id, t.implementation ORDER BY c.run_at)
)
WHERE previous_campaign_id IS NOT NULL;
