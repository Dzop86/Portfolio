// Web worker of the SQL playground: owns the SQLite database so a slow query never freezes the page
// (the page terminates the worker after a timeout). Bundled with sql.js by esbuild into assets/sqlworker.js.
import initSqlJs from 'sql.js/dist/sql-wasm-browser.js';
import { describe, openBenchDb, runQuery } from './core.js';

const asset = (path) => new URL(path, import.meta.url).href;
let db;

async function open() {
  const [SQL, schema, csv] = await Promise.all([
    initSqlJs({ locateFile: () => asset('wasm/sql-wasm.wasm') }),
    fetch(asset('samples/sql/schema.sql')).then((r) => (r.ok ? r.text() : Promise.reject(new Error(r.statusText)))),
    fetch(asset('samples/sql/measurements.csv')).then((r) => (r.ok ? r.text() : Promise.reject(new Error(r.statusText)))),
  ]);
  db = openBenchDb(SQL, schema, csv);
  return describe(db);
}

self.onmessage = async ({ data }) => {
  try {
    if (data.type === 'open') {
      self.postMessage({ id: data.id, ok: true, schema: await open() });
    } else {
      const start = performance.now();
      const result = runQuery(db, data.sql);
      self.postMessage({ id: data.id, ok: true, result, ms: performance.now() - start });
    }
  } catch (e) {
    self.postMessage({ id: data.id, ok: false, error: String(e?.message ?? e), load: data.type === 'open' });
  }
};
