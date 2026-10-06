-- Analysis layer: one row per (campaign, file, implementation), then comparisons built on it with
-- window functions. Medians, not means: a single slow repetition (garbage collection, another process)
-- shifts the mean but barely the median.

CREATE VIEW timing AS
SELECT m.campaign_id,
       f.file_id,
       f.mesh_id,
       me.family,
       me.resolution,
       me.triangles,
       f.format,
       f.bytes,
       i.name AS implementation,
       count(*)                                                    AS runs,
       percentile_cont(0.5) WITHIN GROUP (ORDER BY m.duration_ms) AS median_ms,
       min(m.duration_ms)                                          AS min_ms,
       percentile_cont(0.9) WITHIN GROUP (ORDER BY m.duration_ms) AS p90_ms,
       avg(m.duration_ms)                                          AS mean_ms,
       stddev_samp(m.duration_ms)                                  AS stddev_ms,
       -- Million triangles per second at the median.
       me.triangles / percentile_cont(0.5) WITHIN GROUP (ORDER BY m.duration_ms) / 1000 AS mtri_per_s
FROM measurement m
JOIN file f USING (file_id)
JOIN mesh me USING (mesh_id)
JOIN implementation i USING (implementation_id)
GROUP BY m.campaign_id, f.file_id, me.mesh_id, i.implementation_id;

-- For each mesh and implementation: formats ranked from fastest, and how much slower each is than the best.
CREATE VIEW format_ranking AS
SELECT campaign_id, family, resolution, triangles, implementation, format, median_ms,
       rank() OVER w                       AS rank,
       median_ms / min(median_ms) OVER w   AS slowdown
FROM timing
WINDOW w AS (PARTITION BY campaign_id, mesh_id, implementation ORDER BY median_ms);

-- Along the sizes of one family: how the time grows when the mesh grows (1 = linear between two sizes).
CREATE VIEW scaling AS
SELECT campaign_id, family, format, implementation, resolution, triangles, median_ms,
       triangles::double precision / lag(triangles) OVER w AS size_ratio,
       median_ms / lag(median_ms) OVER w                  AS time_ratio,
       ln(median_ms / lag(median_ms) OVER w) / ln(triangles::double precision / lag(triangles) OVER w)
                                                          AS local_exponent
FROM timing
WINDOW w AS (PARTITION BY campaign_id, family, format, implementation ORDER BY triangles);

-- Empirical complexity: slope of log(time) against log(size) over every size (1 = linear, 2 = quadratic),
-- with the R² of the fit.
CREATE VIEW complexity AS
SELECT campaign_id, implementation, format,
       regr_slope(ln(median_ms), ln(triangles)) AS exponent,
       regr_r2(ln(median_ms), ln(triangles))    AS r2,
       count(*)                                  AS points
FROM timing
GROUP BY campaign_id, implementation, format;

-- Cost of the half-edge structure and curvature: topologie's time over lib-c's on the same file.
CREATE VIEW topology_overhead AS
SELECT campaign_id, family, resolution, triangles, format,
       max(median_ms) FILTER (WHERE implementation = 'lib-c')     AS libc_ms,
       max(median_ms) FILTER (WHERE implementation = 'topologie') AS topologie_ms,
       max(median_ms) FILTER (WHERE implementation = 'topologie')
         / max(median_ms) FILTER (WHERE implementation = 'lib-c') AS ratio
FROM timing
GROUP BY campaign_id, file_id, family, resolution, triangles, format;

-- Regression check between campaigns: each file and implementation compared with its previous campaign.
-- Returns the cases whose median grew by more than `threshold` (0.2 = 20 % slower).
CREATE FUNCTION regressions(threshold double precision DEFAULT 0.2)
RETURNS TABLE (campaign_id integer, previous_campaign_id integer, family text, resolution integer,
               format text, implementation text, previous_ms double precision, median_ms double precision,
               change double precision)
LANGUAGE sql STABLE AS $$
  SELECT *
  FROM (
    SELECT t.campaign_id,
           lag(t.campaign_id) OVER w,
           t.family, t.resolution, t.format, t.implementation,
           lag(t.median_ms) OVER w,
           t.median_ms,
           t.median_ms / lag(t.median_ms) OVER w - 1
    FROM timing t
    JOIN campaign c USING (campaign_id)
    WINDOW w AS (PARTITION BY t.file_id, t.implementation ORDER BY c.run_at)
  ) AS compared (campaign_id, previous_campaign_id, family, resolution, format, implementation,
                 previous_ms, median_ms, change)
  WHERE change > threshold
  ORDER BY change DESC;
$$;
