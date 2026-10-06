# PostgreSQL with the benchmark database loaded at first start, plus pgTAP and pg_prove for the tests.
#   docker build -t bench-db projects/sql && docker run --rm -e POSTGRES_PASSWORD=... -p 5432:5432 bench-db
FROM postgres:17-bookworm
RUN apt-get update \
 && apt-get install -y --no-install-recommends postgresql-17-pgtap libtap-parser-sourcehandler-pgtap-perl \
 && rm -rf /var/lib/apt/lists/*
ENV POSTGRES_DB=bench
COPY schema/01_schema.sql schema/02_views.sql schema/03_indexes.sql /docker-entrypoint-initdb.d/
COPY docker/05_load.sh /docker-entrypoint-initdb.d/
COPY schema/04_load.sql data/measurements.csv /bench/
COPY tests/pgtap /bench/tests
