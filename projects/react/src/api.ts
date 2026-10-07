// Types and loading of the site's static JSON API (D43, src/api.mjs at the repository root).

export type Lang = 'fr' | 'en';
export type Text = Record<Lang, string>;
export type Status = 'done' | 'in-progress' | 'planned';

export interface Project {
  id: string;
  name: Text;
  pitch: Text;
  sprint: string;
  points: number;
  status: Status;
  group: string;
  techs: string[];
  stack: string[];
  code: string | null;
  page: Text;
}

export interface ProjectsFile {
  techs: string[];
  projects: Project[];
}

export interface Sprint {
  number: number;
  goal: Text;
  stories: { points: number; done: boolean; closed: boolean }[];
}

export interface SprintsFile {
  sprintCount: number;
  done: number;
  sprints: Sprint[];
  velocity: { number: number; committed: number; done: number }[];
  burndown: { total: number; remaining: number[] };
}

export interface ParallelBenchFile {
  machine: { cpu: string; logical_processors: number; openmp_threads: number; opencl: string; cuda: string; note: string };
  runs: number;
  results: {
    backend: string;
    triangles: number;
    vertices: number;
    ms: number;
    max_defect_error: number;
    upload_ms?: number;
    kernels_ms?: number;
    download_ms?: number;
  }[];
}

export interface MeshIoFile {
  machine: { cpu: string; runtime: string; date: string };
  results: {
    implementation: string;
    family: string;
    resolution: number;
    format: string;
    vertices: number;
    triangles: number;
    bytes: number;
    repetitions: number;
    median_ms: number;
  }[];
}

export interface MlFile {
  model: string;
  classes: string[];
  points: number;
  test_accuracy: number;
  onnx_max_logit_gap: number;
}

export interface MeshesFile {
  wasm: string;
  samples: { id: string; file: string; name: Text }[];
}

export interface Api {
  projects: ProjectsFile;
  meshes: MeshesFile;
  sprints: SprintsFile;
  parallelBench: ParallelBenchFile;
  meshIo: MeshIoFile;
  ml: MlFile;
}

export class ApiError extends Error {
  constructor(
    readonly url: string,
    readonly status: number | null,
  ) {
    super(status === null ? `${url}: network error` : `${url}: HTTP ${status}`);
    this.name = 'ApiError';
  }
}

/** Where the API lives relative to the dashboard: the site serves it at ../api/v1/. */
export const API_BASE = '../api/v1/';

async function fetchJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  let response: Response;
  try {
    response = await fetch(url, signal ? { signal } : {});
  } catch (e) {
    if (e instanceof DOMException && e.name === 'AbortError') throw e;
    throw new ApiError(url, null);
  }
  if (!response.ok) throw new ApiError(url, response.status);
  return (await response.json()) as T;
}

/** Every file of the API, fetched in parallel; the first failure rejects the whole load. */
export async function loadApi(base = API_BASE, signal?: AbortSignal): Promise<Api> {
  const get = <T,>(file: string) => fetchJson<T>(`${base}${file}`, signal);
  const [projects, sprints, parallelBench, meshIo, ml, meshes] = await Promise.all([
    get<ProjectsFile>('projects.json'),
    get<SprintsFile>('sprints.json'),
    get<ParallelBenchFile>('parallel-bench.json'),
    get<MeshIoFile>('mesh-io.json'),
    get<MlFile>('ml.json'),
    get<MeshesFile>('meshes.json'),
  ]);
  return { projects, sprints, parallelBench, meshIo, ml, meshes };
}
