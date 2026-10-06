// Core of the SQL playground, shared by its web worker and the Node tests: builds the SQLite copy of the
// benchmark database from the schema and the CSV of projects/sql, and runs a visitor's query with a row cap.
import { parseCsv } from '../../projects/sql/bench/csv.mjs';

/** Rows returned to the page at most; the query still runs to its end so the count stays right. */
export const MAX_ROWS = 200;

const STAGING = `CREATE TEMP TABLE staging (
  run_at TEXT, runtime TEXT, os TEXT, arch TEXT, cpu TEXT, git_commit TEXT, warmup INTEGER, repetitions INTEGER,
  family TEXT, resolution INTEGER, vertices INTEGER, triangles INTEGER, euler INTEGER, boundary_loops INTEGER,
  format TEXT, bytes INTEGER, implementation TEXT, repetition INTEGER, duration_ms REAL)`;

// Same normalisation as projects/sql/schema/04_load.sql.
const NORMALISE = `
INSERT INTO campaign (run_at, runtime, os, arch, cpu, git_commit, warmup, repetitions)
  SELECT DISTINCT run_at, runtime, os, arch, cpu, git_commit, warmup, repetitions FROM staging;
INSERT INTO mesh (family, resolution, vertices, triangles, euler, boundary_loops)
  SELECT DISTINCT family, resolution, vertices, triangles, euler, boundary_loops FROM staging;
INSERT INTO file (mesh_id, format, bytes)
  SELECT DISTINCT me.mesh_id, s.format, s.bytes FROM staging s JOIN mesh me USING (family, resolution);
INSERT INTO measurement (campaign_id, file_id, implementation_id, repetition, duration_ms)
  SELECT c.campaign_id, f.file_id, i.implementation_id, s.repetition, s.duration_ms
  FROM staging s
  JOIN campaign c USING (run_at)
  JOIN mesh me USING (family, resolution)
  JOIN file f ON f.mesh_id = me.mesh_id AND f.format = s.format
  LEFT JOIN implementation i ON i.name = s.implementation;
DROP TABLE staging;`;

/**
 * Opens a new in-memory database: math functions SQLite lacks in sql.js, the schema, then the CSV.
 * Throws if a row breaks a constraint (the whole load is one transaction).
 */
export function openBenchDb(SQL, schema, csv) {
  const db = new SQL.Database();
  db.create_function('ln', (x) => (x > 0 ? Math.log(x) : null));
  db.create_function('sqrt', (x) => (x >= 0 ? Math.sqrt(x) : null));
  db.run('PRAGMA foreign_keys = ON');
  db.exec(schema);
  const [header, ...rows] = parseCsv(csv);
  db.exec('BEGIN');
  try {
    db.exec(STAGING);
    const insert = db.prepare(`INSERT INTO staging VALUES (${header.map(() => '?').join(', ')})`);
    try {
      for (const row of rows) insert.run(row);
    } finally {
      insert.free();
    }
    db.exec(NORMALISE);
    db.exec('COMMIT');
  } catch (e) {
    db.exec('ROLLBACK');
    db.close();
    throw e;
  }
  return db;
}

/**
 * Runs one or more statements and returns the result of the last one that yields columns (or, if none
 * does, the number of rows changed): { columns, rows, total, truncated, changes }. SQL errors throw.
 */
export function runQuery(db, sql, maxRows = MAX_ROWS) {
  let result = null;
  for (const stmt of db.iterateStatements(sql)) {
    try {
      const columns = stmt.getColumnNames();
      const rows = [];
      let total = 0;
      while (stmt.step()) {
        if (total++ < maxRows) rows.push(stmt.get());
      }
      if (columns.length > 0) result = { columns, rows, total, truncated: total > maxRows };
    } finally {
      stmt.free();
    }
  }
  return result ?? { columns: [], rows: [], total: 0, truncated: false, changes: db.getRowsModified() };
}

/** Tables and views with their columns, for the schema panel: [{ name, type, columns: [name] }]. */
export function describe(db) {
  const objects = runQuery(db, `SELECT name, type FROM sqlite_schema
    WHERE type IN ('table', 'view') AND name NOT LIKE 'sqlite_%' ORDER BY type, rowid`).rows;
  return objects.map(([name, type]) => ({
    name,
    type,
    columns: runQuery(db, `SELECT name FROM pragma_table_info('${name.replaceAll("'", "''")}')`).rows.map(([c]) => c),
  }));
}

/** Splits a file of examples marked "-- example: key" into [{ key, sql }]. */
export function parseExamples(text) {
  return text.split(/^-- example: */m).slice(1).map((block) => {
    const [key, ...rest] = block.split('\n');
    return { key: key.trim(), sql: rest.join('\n').trim() };
  });
}
