import { describe, expect, it } from 'vitest';
import type { MeshIoFile, ParallelBenchFile, Project, SprintsFile } from '../src/api';
import { distinct, filterProjects, meanVelocity, meshIoSeries, parallelAtLargest, projectSummary } from '../src/model';

const project = (id: string, status: Project['status'], points: number, techs: string[]): Project => ({
  id, status, points, techs, name: { fr: id, en: id }, pitch: { fr: '', en: '' }, sprint: 'S1', group: 'web', stack: [], code: null,
  page: { fr: `fr/project-${id}.html`, en: `en/project-${id}.html` },
});
const projects = [project('a', 'done', 5, ['C']), project('b', 'in-progress', 8, ['C', 'Python']), project('c', 'planned', 3, ['Ada'])];

describe('projects', () => {
  it('filters by status and technology together', () => {
    expect(filterProjects(projects, 'all', '').map((p) => p.id)).toEqual(['a', 'b', 'c']);
    expect(filterProjects(projects, 'all', 'C').map((p) => p.id)).toEqual(['a', 'b']);
    expect(filterProjects(projects, 'done', 'C').map((p) => p.id)).toEqual(['a']);
    expect(filterProjects(projects, 'planned', 'C')).toEqual([]);
  });
  it('sums the points of the projects done', () => {
    expect(projectSummary(projects)).toEqual({ total: 3, done: 1, inProgress: 1, points: 16, pointsDone: 5 });
  });
});

describe('sprints', () => {
  const sprints = (done: number): SprintsFile => ({
    sprintCount: 4, done, sprints: [],
    velocity: [{ number: 1, committed: 5, done: 4 }, { number: 2, committed: 6, done: 6 }, { number: 3, committed: 7, done: 0 }],
    burndown: { total: 10, remaining: [10] },
  });
  it('averages the finished sprints only', () => {
    expect(meanVelocity(sprints(2))).toBe(5);
    expect(meanVelocity(sprints(0))).toBe(0);
  });
});

describe('parallel benchmark', () => {
  const bench: ParallelBenchFile = {
    machine: { cpu: 'x', logical_processors: 2, openmp_threads: 2, opencl: '', cuda: '', note: '' }, runs: 3,
    results: [
      { backend: 'openmp', triangles: 10, vertices: 5, ms: 2, max_defect_error: 0 },
      { backend: 'sequential', triangles: 10, vertices: 5, ms: 8, max_defect_error: 0 },
      { backend: 'sequential', triangles: 100, vertices: 50, ms: 80, max_defect_error: 0 },
      { backend: 'cuda-float', triangles: 100, vertices: 50, ms: 10, max_defect_error: 1e-6 },
      { backend: 'openmp', triangles: 100, vertices: 50, ms: 20, max_defect_error: 0 },
    ],
  };
  it('keeps the largest size, in the fixed order, with speed-ups over sequential', () => {
    const { size, rows } = parallelAtLargest(bench);
    expect(size).toBe(100);
    expect(rows.map((r) => [r.backend, r.speedup])).toEqual([['sequential', 1], ['openmp', 4], ['cuda-float', 8]]);
  });
  it('needs a sequential time to compare with', () => {
    expect(() => parallelAtLargest({ ...bench, results: bench.results.filter((r) => r.backend !== 'sequential') })).toThrow(/sequential/);
  });
});

describe('mesh reading', () => {
  const row = (implementation: string, family: string, format: string, triangles: number, median_ms: number) =>
    ({ implementation, family, format, triangles, median_ms, resolution: 0, vertices: 0, bytes: 0, repetitions: 7 });
  const io: MeshIoFile = {
    machine: { cpu: 'x', runtime: 'node', date: '2026-10-06' },
    results: [row('lib-c', 'torus', 'ply', 400, 4), row('lib-c', 'torus', 'obj', 400, 6), row('lib-c', 'torus', 'obj', 100, 2), row('topologie', 'torus', 'obj', 100, 1), row('lib-c', 'sphere', 'obj', 100, 3)],
  };
  it('gives one series per format, sorted by size, for one library and family', () => {
    expect(meshIoSeries(io, 'lib-c', 'torus')).toEqual([
      { format: 'obj', points: [{ x: 100, y: 2 }, { x: 400, y: 6 }] },
      { format: 'ply', points: [{ x: 400, y: 4 }] },
    ]);
    expect(meshIoSeries(io, 'nothing', 'torus')).toEqual([]);
  });
  it('distinct keeps the first occurrence order', () => {
    expect(distinct([3, 1, 3, 2, 1])).toEqual([3, 1, 2]);
  });
});
