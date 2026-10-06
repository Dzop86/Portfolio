-- Questions the database answers. Run with: psql -d bench -f queries/examples.sql

-- 1. Which format reads fastest, for each library, on the largest meshes?
SELECT implementation, family, format, rank, round(median_ms::numeric, 2) AS median_ms, round(slowdown::numeric, 2) AS slowdown
FROM format_ranking
WHERE resolution = (SELECT max(resolution) FROM mesh)
ORDER BY implementation, family, rank;

-- 2. How does the time grow with the size? (1 = linear)
SELECT implementation, format, round(exponent::numeric, 3) AS exponent, round(r2::numeric, 4) AS r2, points
FROM complexity
ORDER BY implementation, format;

-- 3. What does the half-edge structure cost on top of reading the file?
SELECT format, round(avg(ratio)::numeric, 2) AS mean_ratio, round(min(ratio)::numeric, 2) AS min_ratio,
       round(max(ratio)::numeric, 2) AS max_ratio
FROM topology_overhead
GROUP BY format
ORDER BY format;

-- 4. Throughput per family and library at the largest size, in million triangles per second.
SELECT family, implementation, format, round(mtri_per_s::numeric, 2) AS mtri_per_s
FROM timing
WHERE resolution = (SELECT max(resolution) FROM mesh)
ORDER BY mtri_per_s DESC;

-- 5. How stable are the measurements? Coefficient of variation, worst first.
SELECT family, resolution, format, implementation, round((stddev_ms / mean_ms)::numeric, 3) AS cv
FROM timing
ORDER BY cv DESC
LIMIT 5;

-- 6. Did anything get more than 20 % slower than in the previous campaign?
SELECT * FROM regressions(0.2);
