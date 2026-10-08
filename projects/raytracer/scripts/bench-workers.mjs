// Measures the gain of several workers on the project page's image (480 x 270, the three spheres), with the
// same WebAssembly module and the same bands as the page, in Node threads instead of browser web workers.
// Usage: node projects/raytracer/scripts/bench-workers.mjs [--passes N] [--out projects/raytracer/data/bench.json]
import { Worker, isMainThread, parentPort, workerData } from 'node:worker_threads';
import { cpus } from 'node:os';
import { writeFileSync } from 'node:fs';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { join } from 'node:path';

const ROOT = fileURLToPath(new URL('../../../', import.meta.url));
// As file:// URLs: on Windows, import() refuses an absolute path (seen in CI with the topology benchmark).
const load = (path) => import(pathToFileURL(join(ROOT, path)).href);
const api = await load('src/assets/raytracer-api.js');
const { bandsFor } = await load('src/assets/raytracer-bands.js');
const [WIDTH, HEIGHT] = [480, 270];

if (!isMainThread) {
  // One worker: load the module, wait for the start signal, render its bands for every pass.
  const lib = await api.loadRaytracer((await load('src/assets/wasm/raytracer.js')).default);
  api.resize(lib, WIDTH, HEIGHT);
  api.setScene(lib, 'spheres');
  const bands = bandsFor(HEIGHT, workerData.workers, workerData.k);
  parentPort.postMessage('loaded');
  parentPort.once('message', () => {
    for (let p = 0; p < workerData.passes; p++) for (const [y0, y1] of bands) api.renderRows(lib, y0, y1);
    parentPort.postMessage('done');
  });
} else {
  const arg = (name, fallback) => {
    const i = process.argv.indexOf(name);
    return i > 0 ? process.argv[i + 1] : fallback;
  };
  const passes = Number(arg('--passes', 8));
  const out = arg('--out', join(ROOT, 'projects/raytracer/data/bench.json'));
  const cores = cpus().length;
  const counts = [1, 2, 4, 8].filter((n) => n <= cores);

  async function run(workers) {
    const pool = Array.from({ length: workers }, (_, k) => new Worker(fileURLToPath(import.meta.url), { workerData: { workers, k, passes } }));
    const next = (w) => new Promise((resolve, reject) => { w.once('message', resolve); w.once('error', reject); });
    await Promise.all(pool.map(next));
    const start = performance.now();
    await Promise.all(pool.map((w) => { const done = next(w); w.postMessage('start'); return done; }));
    const ms = (performance.now() - start) / passes;
    await Promise.all(pool.map((w) => w.terminate()));
    return ms;
  }

  const rows = [];
  for (const workers of counts) {
    // The best of three runs: the others are slowed by whatever else the machine is doing.
    let best = Infinity;
    for (let i = 0; i < 3; i++) best = Math.min(best, await run(workers));
    rows.push({ workers, msPerPass: Math.round(best * 10) / 10 });
    console.log(`${workers} worker(s): ${best.toFixed(1)} ms per pass`);
  }
  for (const row of rows) row.speedup = Math.round((rows[0].msPerPass / row.msPerPass) * 100) / 100;
  const bench = {
    image: `${WIDTH} x ${HEIGHT}`, scene: 'spheres', passes,
    machine: { cpu: cpus()[0].model.trim(), cores, node: process.version },
    rows,
  };
  writeFileSync(out, `${JSON.stringify(bench, null, 2)}\n`);
  console.log(`written to ${out}`);
}
