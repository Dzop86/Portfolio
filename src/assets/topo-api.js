// Wrapper over the WebAssembly build of the C++ topology library (projects/topologie), shared by the
// viewer and the tests.
import { MAX_BYTES } from './meshlib-api.js';

/** Instantiates the Emscripten module produced by projects/topologie/scripts/build-wasm.sh. */
export function loadTopo(createTopo, options = {}) {
  return createTopo(options);
}

const INVALID_MESH = 10;

/**
 * Reads an OBJ or PLY file given as bytes. On success returns the invariants and copies of the arrays
 * (positions centred in the unit sphere, triangle indices, curvature density and angle defect per vertex);
 * otherwise { ok: false, status, message, line } with status 1..4 from lib-c, 'invalid' or 'too-large'.
 */
export function readTopology(lib, bytes) {
  if (bytes.byteLength > MAX_BYTES) return { ok: false, status: 'too-large' };
  const ptr = lib._malloc(Math.max(bytes.byteLength, 1));
  if (!ptr) return { ok: false, status: 'too-large' };
  try {
    lib.HEAPU8.set(bytes, ptr);
    const status = lib._topoc_read(ptr, bytes.byteLength);
    if (status !== 0) {
      return {
        ok: false,
        status: status === INVALID_MESH ? 'invalid' : status,
        message: lib.UTF8ToString(lib._topoc_error()),
        line: lib._topoc_error_line(),
      };
    }
    const nv = lib._topoc_vertex_count();
    const ni = lib._topoc_index_count();
    // Views are taken after every call that may grow memory, then copied out.
    const f32 = (p, n) => lib.HEAPF32.slice(p / 4, p / 4 + n);
    const { totalCurvature, ...invariants } = JSON.parse(lib.UTF8ToString(lib._topoc_summary()));
    return {
      ok: true,
      invariants,
      totalCurvature,
      positions: f32(lib._topoc_positions(), 3 * nv),
      indices: lib.HEAPU32.slice(lib._topoc_indices() / 4, lib._topoc_indices() / 4 + ni),
      curvature: f32(lib._topoc_curvature(), nv),
      defect: f32(lib._topoc_defect(), nv),
      boundary: lib.HEAPU8.slice(lib._topoc_boundary(), lib._topoc_boundary() + nv),
    };
  } finally {
    lib._free(ptr);
  }
}

/**
 * Curvature to colour each vertex with. On the boundary the angle defect measures how much the boundary
 * turns, not the Gaussian curvature, so boundary vertices take the mean of their already known neighbours
 * (through shared triangles): interior ones first, then, pass after pass, boundary ones filled before them
 * (a corner may touch no interior vertex). Vertices that never get a value read as 0.
 */
export function interiorCurvature(curvature, boundary, indices) {
  const values = Float32Array.from(curvature);
  const known = Uint8Array.from(boundary, (b) => (b ? 0 : 1));
  for (let progress = true; progress;) {
    progress = false;
    const sum = new Float64Array(values.length);
    const count = new Uint32Array(values.length);
    for (let i = 0; i < indices.length; i += 3) {
      const tri = [indices[i], indices[i + 1], indices[i + 2]];
      for (const v of tri) {
        if (known[v]) continue;
        for (const w of tri) {
          if (!known[w]) continue;
          sum[v] += values[w];
          count[v] += 1;
        }
      }
    }
    count.forEach((n, v) => {
      if (n === 0) return;
      values[v] = sum[v] / n;
      known[v] = 1;
      progress = true;
    });
  }
  known.forEach((k, v) => { if (!k) values[v] = 0; });
  return values;
}

/**
 * Colour scale by quantiles: t(k) is the share of interior vertices whose |K| is at most |k|, signed like k,
 * in [-1, 1]. Curvature of real meshes is heavy-tailed (sharp creases reach 1000 times the median), so a
 * linear scale would leave almost everything neutral. `ticks` gives the |K| shown at 50 % and 90 % of the
 * legend. Boundary vertices are left out (their defect measures the turning of the boundary).
 */
export function quantileScale(curvature, boundary) {
  const sorted = [];
  curvature.forEach((k, v) => { if (!boundary[v]) sorted.push(Math.abs(k)); });
  sorted.sort((a, b) => a - b);
  const n = sorted.length;
  const rank = (x) => { // number of values <= x
    let lo = 0;
    let hi = n;
    while (lo < hi) {
      const mid = (lo + hi) >> 1;
      if (sorted[mid] <= x) lo = mid + 1;
      else hi = mid;
    }
    return lo;
  };
  return {
    t: (k) => (k === 0 || n === 0 ? 0 : Math.sign(k) * (rank(Math.abs(k)) / n)),
    ticks: [0.5, 0.9].map((at) => ({ at, value: n ? sorted[Math.floor(at * (n - 1))] : 0 })),
  };
}

/** Total curvature in turns (multiples of 2 pi), rounded to 3 decimals; adding 0 turns -0 into 0. */
export function turns(total) {
  return Math.round((total / (2 * Math.PI)) * 1000) / 1000 + 0;
}

const CRITICAL_KINDS = ['min', 'saddle', 'max', 'other'];
export const AXES = { x: [1, 0, 0], y: [0, 1, 0], z: [0, 0, 1] };

/**
 * Height along `direction` of the mesh read last, and its critical points (C++ topo::elevation): height
 * per vertex (viewer units), vertices from lowest to highest, Euler characteristic of the sublevel set
 * after each of them, and the critical points in the order of the filtration, each with its index
 * (+1 minimum or maximum, 1 - k for a saddle whose lower link has k pieces).
 */
export function elevation(lib, direction = AXES.y) {
  if (lib._topoc_elevation(...direction) !== 0) throw new Error('elevation: no mesh read, or a zero direction');
  const n = lib._topoc_vertex_count();
  const height = lib.HEAPF32.slice(lib._topoc_height() / 4, lib._topoc_height() / 4 + n);
  const order = lib.HEAPU32.slice(lib._topoc_order() / 4, lib._topoc_order() / 4 + n);
  const euler = lib.HEAP32.slice(lib._topoc_sublevel_euler() / 4, lib._topoc_sublevel_euler() / 4 + n);
  const critical = JSON.parse(lib.UTF8ToString(lib._topoc_critical()))
    .map(([vertex, kind, index]) => ({ vertex, kind: CRITICAL_KINDS[kind], index }));
  return { height, order, euler, critical };
}

/** Counts of the critical points, saddles with their multiplicity, and the sum of the indices (= chi). */
export function criticalCounts(critical) {
  const c = { min: 0, saddle: 0, max: 0, other: 0, sum: 0 };
  for (const p of critical) {
    c[p.kind] += p.kind === 'saddle' ? -p.index : 1;
    c.sum += p.index;
  }
  return c;
}

/**
 * Persistence diagram of the last elevation (C++ topo::persistence, D48): Betti numbers over Z/2 and the
 * pairs, each with its dimension (0 component, 1 loop, 2 cavity), the vertices where it is born and dies
 * (deathVertex null for an essential class) and their heights in the viewer's units (death Infinity).
 * Above the library's limit of triangles, { tooLarge: true, limit } instead.
 */
export function persistence(lib, { height }) {
  const status = lib._topoc_persistence();
  if (status === 13) return { tooLarge: true, limit: lib._topoc_persistence_limit() };
  if (status !== 0) throw new Error('persistence: compute the elevation first');
  const { betti, pairs } = JSON.parse(lib.UTF8ToString(lib._topoc_pairs()));
  return {
    betti,
    pairs: pairs.map(([dimension, b, d]) => ({
      dimension, birthVertex: b, deathVertex: d < 0 ? null : d, birth: height[b], death: d < 0 ? Infinity : height[d],
    })),
  };
}

/**
 * What the diagram shows above a persistence threshold `tau`: the pairs that live longer than `tau` (essential
 * classes always; at 0, everything but the zero-length pairs of plateau ties, made by tie-breaking), at most `max` of them drawn (the most persistent first) and how many are not, the counts
 * of kept finite pairs and essential classes per dimension, and the vertices of the kept pairs (the critical
 * points worth marking).
 */
export function diagram(pairs, tau, max = 2000) {
  const kept = pairs.filter((p) => p.death - p.birth > tau);
  const finite = [0, 0, 0], essential = [0, 0, 0];
  const vertices = new Set();
  for (const p of kept) {
    (p.deathVertex === null ? essential : finite)[p.dimension] += 1;
    vertices.add(p.birthVertex);
    if (p.deathVertex !== null) vertices.add(p.deathVertex);
  }
  // Compared, not subtracted: two essential classes both live Infinity, and Infinity - Infinity is NaN.
  const life = (p) => p.death - p.birth;
  const drawn = [...kept].sort((a, b) => (life(a) !== life(b) ? (life(b) > life(a) ? 1 : -1) : a.birth - b.birth)).slice(0, max);
  return { kept, drawn, hidden: kept.length - drawn.length, finite, essential, vertices };
}

/**
 * Reeb graph of the last elevation (C++ topo::reeb_graph, D48): nodes (vertex, arcs down, arcs up) in the order
 * of the filtration, arcs from a lower to an upper node with the level set centroids in between (flat x, y, z
 * in the viewer's units), loops and components. Above the library's limits, { tooLarge: 'triangles' | 'nodes',
 * limit } instead.
 */
export function reeb(lib, samples = 32) {
  const status = lib._topoc_reeb(samples);
  if (status === 13) return { tooLarge: 'triangles', limit: lib._topoc_persistence_limit() };
  if (status === 14) return { tooLarge: 'nodes', limit: lib._topoc_reeb_limit() };
  if (status !== 0) throw new Error('reeb: compute the elevation first');
  const { loops, components, nodes, arcs } = JSON.parse(lib.UTF8ToString(lib._topoc_reeb_graph()));
  return {
    loops,
    components,
    nodes: nodes.map(([vertex, down, up]) => ({ vertex, down, up })),
    arcs: arcs.map(([lower, upper, path]) => ({ lower, upper, path })),
  };
}

/** Kind of a Reeb graph node, for its colour: nothing below a minimum, nothing above a maximum. */
export function nodeKind({ down, up }) {
  if (down === 0 && up === 0) return 'other';
  if (down === 0) return 'min';
  if (up === 0) return 'max';
  return 'saddle';
}

/**
 * The arcs as polylines (flat x, y, z) from their lower node, cut where they rise above height `h` along the unit
 * `direction`; an arc whose lower node is above `h` is left out.
 */
export function clipArcs(graph, positions, height, direction, h) {
  const at = (v) => [positions[3 * v], positions[3 * v + 1], positions[3 * v + 2]];
  const lines = [];
  for (const a of graph.arcs) {
    const lo = graph.nodes[a.lower].vertex, hi = graph.nodes[a.upper].vertex;
    if (height[lo] > h) continue;
    const points = [at(lo)];
    for (let k = 0; k < a.path.length; k += 3) points.push(a.path.slice(k, k + 3));
    points.push(at(hi));
    const heights = points.map((p, k) => (k === 0 ? height[lo] : k === points.length - 1 ? height[hi]
      : p[0] * direction[0] + p[1] * direction[1] + p[2] * direction[2]));
    const line = [...points[0]];
    for (let k = 1; k < points.length; k++) {
      if (heights[k] <= h) {
        line.push(...points[k]);
        continue;
      }
      const s = (h - heights[k - 1]) / (heights[k] - heights[k - 1]);
      line.push(...points[k - 1].map((c, i) => c + s * (points[k][i] - c)));
      break;
    }
    lines.push(line);
  }
  return lines;
}

/**
 * The lower-star filtration for drawing: triangles sorted by their highest vertex, so that the sublevel set
 * up to rank r is the first `faces(r)` triangles; and the rank of the last vertex at or below a height.
 */
export function filtration(indices, { height, order }) {
  const n = order.length;
  const rank = new Uint32Array(n);
  order.forEach((v, r) => { rank[v] = r; });
  const t = indices.length / 3;
  const top = new Uint32Array(t);
  for (let f = 0; f < t; f++) top[f] = Math.max(rank[indices[3 * f]], rank[indices[3 * f + 1]], rank[indices[3 * f + 2]]);
  const sorted = Array.from({ length: t }, (_, f) => f).sort((a, b) => top[a] - top[b] || a - b);
  const reordered = new Uint32Array(indices.length);
  sorted.forEach((f, k) => reordered.set(indices.subarray(3 * f, 3 * f + 3), 3 * k));
  const tops = Uint32Array.from(sorted, (f) => top[f]);
  return {
    indices: reordered,
    rank,
    // Triangles whose highest vertex has rank <= r: they come first in `indices`.
    faces(r) {
      let lo = 0, hi = tops.length;
      while (lo < hi) { const mid = (lo + hi) >> 1; if (tops[mid] <= r) lo = mid + 1; else hi = mid; }
      return lo;
    },
    // Rank of the highest vertex at or below height h, or -1 if none.
    rankAt(h) {
      let lo = 0, hi = n;
      while (lo < hi) { const mid = (lo + hi) >> 1; if (height[order[mid]] <= h) lo = mid + 1; else hi = mid; }
      return lo - 1;
    },
  };
}
