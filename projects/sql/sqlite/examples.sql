-- Example queries of the playground (SQLite dialect). Each starts with "-- example: <key>"; its title is
-- the i18n key sql.example.<key>.

-- example: ranking
SELECT implementation, family, format, rank, round(median_ms, 1) AS median_ms, round(slowdown, 2) AS slowdown
FROM format_ranking
WHERE resolution = (SELECT max(resolution) FROM mesh)
ORDER BY implementation, family, rank;

-- example: complexity
SELECT implementation, format, round(exponent, 3) AS exponent, round(r2, 4) AS r2, points
FROM complexity
ORDER BY implementation, format;

-- example: overhead
SELECT format, round(avg(ratio), 2) AS mean_ratio, round(min(ratio), 2) AS min_ratio, round(max(ratio), 2) AS max_ratio
FROM topology_overhead
GROUP BY format
ORDER BY format;

-- example: throughput
SELECT family, implementation, format, round(mtri_per_s, 2) AS mtri_per_s
FROM timing
WHERE resolution = (SELECT max(resolution) FROM mesh)
ORDER BY mtri_per_s DESC;

-- example: scaling
SELECT family, format, implementation, resolution, triangles, round(median_ms, 2) AS median_ms,
       round(time_ratio, 2) AS time_ratio, round(local_exponent, 2) AS local_exponent
FROM scaling
WHERE family = 'torus' AND format = 'ply'
ORDER BY implementation, triangles;

-- example: raw
SELECT c.run_at, me.family, me.triangles, f.format, i.name AS implementation, m.repetition, m.duration_ms
FROM measurement m
JOIN campaign c USING (campaign_id)
JOIN file f USING (file_id)
JOIN mesh me USING (mesh_id)
JOIN implementation i USING (implementation_id)
ORDER BY m.duration_ms DESC
LIMIT 20;
