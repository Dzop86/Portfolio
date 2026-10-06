#!/bin/sh
# Run by the postgres image after the schema files (alphabetical order), on the first start only.
set -eu
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB" -f /bench/04_load.sql < /bench/measurements.csv
