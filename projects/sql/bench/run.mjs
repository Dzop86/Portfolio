// Times the WebAssembly builds of lib-c and topologie on the synthetic meshes and writes one CSV row per
// measurement. Every result is checked against the known invariants before it is recorded.
//
//   node projects/sql/bench/run.mjs [--quick] [--out file.csv]
//
// --quick: two small resolutions and two repetitions (smoke test, used by CI). Without it the full
// campaign is written to projects/sql/data/measurements.csv, which the database loads.
import { execFileSync } from 'node:child_process';
import { writeFileSync } from 'node:fs';
import { arch, cpus, platform } from 'node:os';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { performance } from 'node:perf_hooks';
import { csvLine } from './csv.mjs';
import { FAMILIES, FORMATS, encode, makeMesh } from './meshes.mjs';

const HERE = dirname(fileURLToPath(import.meta.url));
const ROOT = join(HERE, '../../..');

export const COLUMNS = ['run_at', 'runtime', 'os', 'arch', 'cpu', 'git_commit', 'warmup', 'repetitions',
  'family', 'resolution', 'vertices', 'triangles', 'euler', 'boundary_loops', 'format', 'bytes',
  'implementation', 'repetition', 'duration_ms'];

export const FULL = { resolutions: [16, 32, 64, 128, 256], warmup: 2, repetitions: 7 };
export const QUICK = { resolutions: [4, 8], warmup: 1, repetitions: 2 };

/** The two libraries under test, each checked against what the mesh must give. */
export async function loadImplementations() {
  const { loadMeshLib, readMesh } = await import('../../../src/assets/meshlib-api.js');
  const { loadTopo, readTopology } = await import('../../../src/assets/topo-api.js');
  const meshlib = await loadMeshLib((await import('../../../src/assets/wasm/meshlib.js')).default);
  const topo = await loadTopo((await import('../../../src/assets/wasm/topo.js')).default);
  return {
    'lib-c': {
      run: (bytes) => readMesh(meshlib, bytes),
      check: (r, expected) => r.ok && r.vertices === expected.vertices && r.triangles === expected.triangles
        && r.euler === expected.euler,
    },
    topologie: {
      run: (bytes) => readTopology(topo, bytes),
      check: (r, expected) => r.ok && r.invariants.euler === expected.euler
        && r.invariants.boundaryLoops === expected.boundaryLoops && r.invariants.consistentlyOriented === true,
    },
  };
}

function gitCommit() {
  try {
    return execFileSync('git', ['rev-parse', '--short=12', 'HEAD'], { cwd: ROOT, encoding: 'utf8' }).trim();
  } catch {
    return 'unknown';
  }
}

/** Runs a campaign and returns its CSV text (header included). `log` receives one line per mesh. */
export async function runCampaign({ resolutions, warmup, repetitions }, log = () => {}) {
  const impls = await loadImplementations();
  const campaign = [new Date().toISOString(), `node ${process.versions.node}`, platform(), arch(),
    cpus()[0]?.model.trim() ?? 'unknown', gitCommit(), warmup, repetitions];
  const rows = [csvLine(COLUMNS)];
  for (const family of Object.keys(FAMILIES)) {
    for (const k of resolutions) {
      const mesh = makeMesh(family, k);
      const expected = { vertices: mesh.vertices.length / 3, triangles: mesh.triangles.length / 3, ...FAMILIES[family] };
      for (const format of FORMATS) {
        const bytes = encode(mesh, format);
        for (const [name, impl] of Object.entries(impls)) {
          for (let w = 0; w < warmup; w++) impl.run(bytes);
          for (let rep = 1; rep <= repetitions; rep++) {
            const start = performance.now();
            const result = impl.run(bytes);
            const ms = performance.now() - start;
            if (!impl.check(result, expected)) {
              throw new Error(`${name} gave a wrong result on ${family} k=${k} (${format}): ${JSON.stringify(result.invariants ?? result)}`);
            }
            rows.push(csvLine([...campaign, family, k, expected.vertices, expected.triangles, expected.euler,
              expected.boundaryLoops, format, bytes.byteLength, name, rep, ms.toFixed(4)]));
          }
        }
      }
      log(`${family} k=${k}: ${expected.triangles} triangles`);
    }
  }
  return `${rows.join('\n')}\n`;
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const args = process.argv.slice(2);
  const quick = args.includes('--quick');
  const outAt = args.indexOf('--out');
  const out = outAt >= 0 ? args[outAt + 1] : join(HERE, '../data/measurements.csv');
  const csv = await runCampaign(quick ? QUICK : FULL, (line) => console.log(line));
  writeFileSync(out, csv);
  console.log(`${csv.trimEnd().split('\n').length - 1} measurements written to ${out}`);
}
