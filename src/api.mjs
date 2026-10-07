// Static JSON API (D43): what the dashboards (React, then Angular) read, built from the same files as the
// pages. Versioned under api/v1/; a breaking change goes to v2 and leaves v1 in place.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { ROOT, burndown, projectPage, techsOf, velocity, roadmapState } from './lib.mjs';
import { TOPO_SAMPLES } from './templates.mjs';

export const API_VERSION = 'v1';

/** Median of a non-empty list of numbers (the mean of the two middle ones for an even count). */
export function median(values) {
  if (values.length === 0) throw new Error('median of an empty list');
  const v = [...values].sort((a, b) => a - b);
  const mid = Math.floor(v.length / 2);
  return v.length % 2 ? v[mid] : (v[mid - 1] + v[mid]) / 2;
}

/**
 * Reading times of projects/sql's campaign: one row per library, mesh family, resolution and format,
 * with the median of its repetitions (the same statistic as the database's timing view).
 */
export function meshIo(csv) {
  const [header, ...lines] = csv.trim().split(/\r?\n/);
  const cols = header.split(',');
  const at = (name) => {
    const i = cols.indexOf(name);
    if (i < 0) throw new Error(`measurements.csv has no "${name}" column`);
    return i;
  };
  const idx = Object.fromEntries(['implementation', 'family', 'resolution', 'format', 'vertices', 'triangles', 'bytes', 'duration_ms', 'cpu', 'runtime', 'run_at']
    .map((c) => [c, at(c)]));
  const groups = new Map();
  let machine = null;
  for (const line of lines) {
    const f = line.split(',');
    machine ??= { cpu: f[idx.cpu], runtime: f[idx.runtime], date: f[idx.run_at].slice(0, 10) };
    const key = [f[idx.implementation], f[idx.family], f[idx.resolution], f[idx.format]].join('|');
    if (!groups.has(key)) {
      groups.set(key, {
        implementation: f[idx.implementation], family: f[idx.family], resolution: Number(f[idx.resolution]), format: f[idx.format],
        vertices: Number(f[idx.vertices]), triangles: Number(f[idx.triangles]), bytes: Number(f[idx.bytes]), runs: [],
      });
    }
    groups.get(key).runs.push(Number(f[idx.duration_ms]));
  }
  const results = [...groups.values()]
    .map(({ runs, ...g }) => ({ ...g, repetitions: runs.length, median_ms: median(runs) }))
    .sort((a, b) => a.implementation.localeCompare(b.implementation) || a.family.localeCompare(b.family)
      || a.format.localeCompare(b.format) || a.resolution - b.resolution);
  return { machine, results };
}

/** Every file of the API, as { "projects.json": object, ... }. Deterministic: no build date inside. */
export function buildApi(data) {
  const { projects, sprints, scrum } = data;
  const read = (path) => readFileSync(join(ROOT, path), 'utf8');
  const state = roadmapState(sprints);
  const files = {
    'projects.json': {
      techs: techsOf(projects),
      projects: projects.map((p) => ({
        id: p.id, name: p.name, pitch: p.pitch, sprint: p.sprint, points: p.points, status: p.status, group: p.group,
        techs: p.techs, stack: p.stack, code: p.links?.code ?? null,
        // Relative to the site's root, like the API itself.
        page: { fr: `fr/${projectPage(p.id)}.html`, en: `en/${projectPage(p.id)}.html` },
      })),
    },
    'sprints.json': {
      sprintCount: scrum.sprintCount,
      done: state.done,
      // The titles and story texts are in French only: the API gives the bilingual goal and the points.
      sprints: sprints.map((s) => ({
        number: s.number, goal: s.goal,
        stories: s.stories.map((st) => ({ points: st.points, done: st.done, closed: st.closed })),
      })),
      velocity: velocity(sprints),
      // Up to the latest sprint opened, as on the project management page.
      burndown: burndown(projects, Math.max(...sprints.map((s) => s.number))),
    },
    'parallel-bench.json': JSON.parse(read('projects/parallele/data/bench.json')),
    'mesh-io.json': meshIo(read('projects/sql/data/measurements.csv')),
    'ml.json': (({ model, classes, points, test_accuracy: testAccuracy, max_logit_gap: maxLogitGap }) =>
      ({ model, classes, points, test_accuracy: testAccuracy, onnx_max_logit_gap: maxLogitGap }))(JSON.parse(read('projects/ml/export/pointnet.json'))),
  };
  // The meshes of the topology viewer and the WebAssembly build of projects/topologie that reads them,
  // both published by the site (paths relative to its root).
  files['meshes.json'] = {
    wasm: 'assets/wasm/topo.js',
    samples: TOPO_SAMPLES.map((id) => ({
      id, file: `assets/samples/topologie/${id}.obj`,
      name: { fr: data.i18n.fr[`topo.sample.${id}`], en: data.i18n.en[`topo.sample.${id}`] },
    })),
  };
  files['index.json'] = { version: API_VERSION, files: Object.keys(files).sort() };
  return files;
}
