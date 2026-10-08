// Computing times of the project page's table (sprint 40, extended persistence since sprint 41): the native C++ (tools/topo_bench, built in Release) and
// the committed WebAssembly build through the viewer's wrapper, in Node, on the same standing tori of 10 000 to
// 1 000 000 triangles. WebAssembly also times the reading of the OBJ file (its size is in the table: the page refuses
// files over 32 MB); above the page's limit of triangles, persistence and Reeb graph are refused (null).
// Usage: node projects/topologie/scripts/bench.mjs --native projects/topologie/build-bench/topo_bench [--runs 3]
//        [--out projects/topologie/data/bench.json]
import { execFileSync } from 'node:child_process';
import { cpus } from 'node:os';
import { writeFileSync } from 'node:fs';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { join } from 'node:path';

const ROOT = fileURLToPath(new URL('../../../', import.meta.url));
// As file:// URLs: on Windows, import() refuses an absolute path ("D:\..."), seen in CI.
const load = (path) => import(pathToFileURL(join(ROOT, path)).href);
const api = await load('src/assets/topo-api.js');

/** The OBJ text of bench::torus(n, m) (tools/bench.hpp): same vertices, same triangles, six decimals. */
export function torusObj(n, m) {
  const lines = [];
  for (let i = 0; i < n; i++) {
    for (let j = 0; j < m; j++) {
      const u = (2 * Math.PI * i) / n, v = (2 * Math.PI * j) / m;
      lines.push(`v ${((2 + Math.cos(v)) * Math.cos(u)).toFixed(6)} ${((2 + Math.cos(v)) * Math.sin(u)).toFixed(6)} ${Math.sin(v).toFixed(6)}`);
    }
  }
  for (let i = 0; i < n; i++) {
    for (let j = 0; j < m; j++) {
      const a = i * m + j + 1, b = ((i + 1) % n) * m + j + 1, c = ((i + 1) % n) * m + ((j + 1) % m) + 1, d = i * m + ((j + 1) % m) + 1;
      lines.push(`f ${a} ${b} ${c}`, `f ${a} ${c} ${d}`);
    }
  }
  return `${lines.join('\n')}\n`;
}

// Same sizes as bench::kSizes.
export const SIZES = [[100, 50], [200, 75], [500, 100], [750, 200], [1000, 500]];

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const arg = (name, fallback) => {
    const i = process.argv.indexOf(name);
    return i > 0 ? process.argv[i + 1] : fallback;
  };
  const runs = Number(arg('--runs', 3));
  const out = arg('--out', join(ROOT, 'projects/topologie/data/bench.json'));
  const native = JSON.parse(execFileSync(arg('--native', join(ROOT, 'projects/topologie/build-bench/topo_bench')), [String(runs)], { encoding: 'utf8' }));

  const createTopo = (await load('src/assets/wasm/topo.js')).default;
  const rows = [];
  for (const [k, [n, m]] of SIZES.entries()) {
    const bytes = new TextEncoder().encode(torusObj(n, m));
    const best = { read: Infinity, elevation: Infinity, persistence: Infinity, reeb: Infinity, extended: Infinity };
    let refused = false;
    for (let r = 0; r < runs; r++) {
      // A fresh module per run: memory grown by the largest mesh does not help the next one.
      const lib = await api.loadTopo(createTopo);
      const read = api.timed(() => api.readTopology(lib, bytes, Infinity));
      if (!read.value.ok) throw new Error(`torus ${n} x ${m}: ${read.value.status}`);
      const e = api.timed(() => api.elevation(lib));
      const p = api.timed(() => api.persistence(lib, e.value));
      const g = api.timed(() => api.reeb(lib));
      const x = api.timed(() => api.extendedPersistence(lib, e.value));
      refused = Boolean(p.value.tooLarge);
      best.read = Math.min(best.read, read.ms);
      best.elevation = Math.min(best.elevation, e.ms);
      best.persistence = Math.min(best.persistence, p.ms);
      best.reeb = Math.min(best.reeb, g.ms);
      best.extended = Math.min(best.extended, x.ms);
      if (!refused && (p.value.betti.join() !== '1,2,1' || g.value.loops !== 1 || x.value.extended.length !== 4)) throw new Error(`torus ${n} x ${m}: wrong results`);
    }
    const round = (x) => Math.round(x * 100) / 100;
    rows.push({
      triangles: 2 * n * m,
      bytes: bytes.byteLength,
      native: { elevation: native[k].elevation, persistence: native[k].persistence, reeb: native[k].reeb, extended: native[k].extended },
      wasm: {
        read: round(best.read),
        elevation: round(best.elevation),
        persistence: refused ? null : round(best.persistence),
        reeb: refused ? null : round(best.reeb),
        extended: refused ? null : round(best.extended),
      },
    });
    console.log(rows.at(-1));
  }
  const bench = {
    mesh: 'standing torus',
    runs,
    machine: { cpu: cpus()[0].model.trim(), node: process.version },
    limits: { bytes: (await load('src/assets/meshlib-api.js')).MAX_BYTES, triangles: (await api.loadTopo(createTopo))._topoc_persistence_limit() },
    rows,
  };
  writeFileSync(out, `${JSON.stringify(bench, null, 2)}\n`);
  console.log(`written to ${out}`);
}
