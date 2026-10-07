// What the 3D viewer computes, apart from React and WebGL: reading a mesh with the WebAssembly build of
// projects/topologie (the module the site publishes), colours by curvature, the vertex under the pointer.
// The reading and the colour scale are the site's own (src/assets/topo-api.js), shared, not copied.
import { interiorCurvature, quantileScale, readTopology, turns } from '../../../../src/assets/topo-api.js';

export interface Invariants {
  components: number;
  boundaryLoops: number;
  euler: number;
  genus: number | null;
  orientable: boolean;
  manifold: boolean;
}

export interface Topology {
  ok: true;
  invariants: Invariants;
  totalCurvature: number;
  positions: Float32Array;
  indices: Uint32Array;
  curvature: Float32Array;
  defect: Float32Array;
  boundary: Uint8Array;
}

/** lib-c's status (1 to 4), an invalid mesh, a file over the size limit, or the module that did not load. */
export interface TopologyError {
  ok: false;
  status: number | 'invalid' | 'too-large' | 'load';
  message?: string;
  line?: number;
}

export type Reader = (bytes: Uint8Array) => Promise<Topology | TopologyError>;

type TopoModule = { default: (options?: object) => Promise<unknown> };

/**
 * A reader over the WebAssembly module at `url`, loaded on first use; a module that fails to load is
 * an error, and the next read tries again.
 */
export function wasmReader(url: string, load: (url: string) => Promise<TopoModule> = (u) => import(/* @vite-ignore */ u)): Reader {
  let lib: Promise<unknown> | null = null;
  return async (bytes) => {
    try {
      lib ??= load(url).then((m) => m.default());
      return readTopology(await lib, bytes) as Topology | TopologyError;
    } catch {
      lib = null;
      return { ok: false, status: 'load' };
    }
  };
}

export type Rgb = [number, number, number];

export interface Palette {
  negative: Rgb;
  zero: Rgb;
  positive: Rgb;
}

/**
 * One RGB triple per vertex: the neutral colour towards the negative (saddle) or positive (dome) one, by
 * the quantile of |K| among interior vertices; boundary vertices take their neighbours' curvature.
 * Also the |K| at the 50 % and 90 % marks of the legend.
 */
export function vertexColours(t: Pick<Topology, 'curvature' | 'boundary' | 'indices'>, palette: Palette) {
  const values = interiorCurvature(t.curvature, t.boundary, t.indices) as Float32Array;
  const scale = quantileScale(t.curvature, t.boundary) as { t: (k: number) => number; ticks: { at: number; value: number }[] };
  const out = new Float32Array(3 * values.length);
  values.forEach((k, i) => {
    const s = scale.t(k);
    const to = s < 0 ? palette.negative : palette.positive;
    const a = Math.abs(s);
    for (let c = 0; c < 3; c++) out[3 * i + c] = palette.zero[c]! + (to[c]! - palette.zero[c]!) * a;
  });
  return { colours: out, ticks: scale.ticks };
}

/** Of a triangle's three corners, the one nearest to a point (where the pointer hit the triangle). */
export function nearestCorner(positions: ArrayLike<number>, corners: [number, number, number], p: { x: number; y: number; z: number }): number {
  const d2 = (v: number) => (positions[3 * v]! - p.x) ** 2 + (positions[3 * v + 1]! - p.y) ** 2 + (positions[3 * v + 2]! - p.z) ** 2;
  return corners.reduce((a, b) => (d2(a) <= d2(b) ? a : b));
}

/** Total curvature in turns (multiples of 2 pi), 3 decimals, never -0. */
export const totalTurns = (total: number): number => turns(total) as number;
