// What the views compute from the API, as pure functions.
import type { MeshIoFile, ParallelBenchFile, Project, SprintsFile, Status } from './api';

export type StatusFilter = Status | 'all';

export function filterProjects(projects: Project[], status: StatusFilter, tech: string): Project[] {
  return projects.filter((p) => (status === 'all' || p.status === status) && (tech === '' || p.techs.includes(tech)));
}

export function projectSummary(projects: Project[]) {
  const count = (s: Status) => projects.filter((p) => p.status === s).length;
  const points = projects.reduce((acc, p) => acc + p.points, 0);
  const pointsDone = projects.filter((p) => p.status === 'done').reduce((acc, p) => acc + p.points, 0);
  return { total: projects.length, done: count('done'), inProgress: count('in-progress'), points, pointsDone };
}

/** Mean points delivered by the finished sprints (0 before the first one). */
export function meanVelocity(sprints: SprintsFile): number {
  const finished = sprints.velocity.filter((v) => v.number <= sprints.done);
  return finished.length ? finished.reduce((acc, v) => acc + v.done, 0) / finished.length : 0;
}

export const BACKENDS = ['sequential', 'openmp', 'opencl', 'cuda-double', 'cuda-float'] as const;
export type Backend = (typeof BACKENDS)[number];

/** The versions measured at the largest size, in a fixed order, with their speed-up over sequential. */
export function parallelAtLargest(bench: ParallelBenchFile) {
  const size = Math.max(...bench.results.map((r) => r.triangles));
  const at = bench.results.filter((r) => r.triangles === size);
  const base = at.find((r) => r.backend === 'sequential');
  if (!base) throw new Error('the benchmark has no sequential time at its largest size');
  const rows = BACKENDS.flatMap((b) => {
    const r = at.find((x) => x.backend === b);
    return r ? [{ backend: b, ms: r.ms, speedup: base.ms / r.ms, error: r.max_defect_error }] : [];
  });
  return { size, rows };
}

/** Reading time against size, one series per format, for one library and one mesh family. */
export function meshIoSeries(io: MeshIoFile, implementation: string, family: string) {
  const rows = io.results.filter((r) => r.implementation === implementation && r.family === family);
  const formats = [...new Set(rows.map((r) => r.format))].sort();
  return formats.map((format) => ({
    format,
    points: rows
      .filter((r) => r.format === format)
      .sort((a, b) => a.triangles - b.triangles)
      .map((r) => ({ x: r.triangles, y: r.median_ms })),
  }));
}

export function distinct<T>(values: T[]): T[] {
  return [...new Set(values)];
}
