// Generalized maps of dimension 2, written from the textbook definitions (Lienhardt): darts, three
// involutions alpha0, alpha1, alpha2, cells as orbits. Shared by the course page and the tests.
//   - alpha_i is an involution: alpha_i(alpha_i(d)) = d; d is "i-free" when alpha_i(d) = d;
//   - alpha0 o alpha2 is an involution too (the cycle alpha0 alpha2 alpha0 alpha2 closes);
//   - in dimension 2: vertex = orbit <alpha1, alpha2>, edge = <alpha0, alpha2>, face = <alpha0, alpha1>,
//     connected component = <alpha0, alpha1, alpha2>.

export const ORBITS = {
  vertex: [1, 2],
  edge: [0, 2],
  face: [0, 1],
  component: [0, 1, 2],
};

export class GMap {
  constructor() {
    /** alpha[i][d]: the dart linked to d by alpha_i (d itself when free). */
    this.alpha = [[], [], []];
  }

  get size() {
    return this.alpha[0].length;
  }

  addDart() {
    const d = this.size;
    for (const a of this.alpha) a.push(d);
    return d;
  }

  /** Links a and b by alpha_i (both must be i-free, or already linked together). */
  link(i, a, b) {
    const ai = this.alpha[i];
    if (ai[a] === b && ai[b] === a) return;
    if (ai[a] !== a || ai[b] !== b) throw new Error(`alpha${i}: dart ${ai[a] !== a ? a : b} is already linked`);
    ai[a] = b;
    ai[b] = a;
  }

  isFree(i, d) {
    return this.alpha[i][d] === d;
  }

  /** Violated constraints, as messages; empty for a valid 2-G-map. */
  check() {
    const problems = [];
    for (let d = 0; d < this.size; d++) {
      for (let i = 0; i <= 2; i++) {
        const e = this.alpha[i][d];
        if (!(e >= 0 && e < this.size)) problems.push(`alpha${i}(${d}) is not a dart`);
        else if (this.alpha[i][e] !== d) problems.push(`alpha${i} is not an involution at dart ${d}`);
      }
      const [a0, , a2] = this.alpha;
      if (a0[a2[a0[a2[d]]]] !== d) problems.push(`alpha0 alpha2 alpha0 alpha2 does not close at dart ${d}`);
    }
    return problems;
  }

  /** Darts reachable from d through the given involutions, in breadth-first order. */
  orbit(d, dims) {
    const seen = new Set([d]);
    const queue = [d];
    for (let k = 0; k < queue.length; k++) {
      for (const i of dims) {
        const e = this.alpha[i][queue[k]];
        if (!seen.has(e)) {
          seen.add(e);
          queue.push(e);
        }
      }
    }
    return queue;
  }

  /** Partition of the darts into orbits of the given type. */
  cells(dims) {
    const owner = new Array(this.size).fill(-1);
    const out = [];
    for (let d = 0; d < this.size; d++) {
      if (owner[d] >= 0) continue;
      const orbit = this.orbit(d, dims);
      for (const e of orbit) owner[e] = out.length;
      out.push(orbit);
    }
    return out;
  }

  /** Boundary loops: 2-free darts, linked by alpha0 and by turning around a vertex to the next 2-free dart. */
  boundaryLoops() {
    const free = [];
    for (let d = 0; d < this.size; d++) if (this.isFree(2, d)) free.push(d);
    const parent = new Map(free.map((d) => [d, d]));
    const find = (x) => {
      while (parent.get(x) !== x) x = parent.get(x);
      return x;
    };
    const join = (a, b) => parent.set(find(a), find(b));
    for (const d of free) {
      join(d, this.alpha[0][d]);
      let e = this.alpha[1][d];
      while (!this.isFree(2, e)) e = this.alpha[1][this.alpha[2][e]];
      join(d, e);
    }
    return new Set(free.map(find)).size;
  }

  /**
   * Orientable when the darts of each component can be coloured in two colours so that every link
   * (other than a dart free for it) joins two colours: the two colours are the two orientations.
   */
  orientable() {
    const colour = new Array(this.size).fill(-1);
    for (let s = 0; s < this.size; s++) {
      if (colour[s] >= 0) continue;
      colour[s] = 0;
      const queue = [s];
      for (let k = 0; k < queue.length; k++) {
        const d = queue[k];
        for (let i = 0; i <= 2; i++) {
          const e = this.alpha[i][d];
          if (e === d) continue;
          if (colour[e] < 0) {
            colour[e] = 1 - colour[d];
            queue.push(e);
          } else if (colour[e] === colour[d]) {
            return false;
          }
        }
      }
    }
    return true;
  }

  /** Number of cells of each kind, Euler characteristic, boundary, orientability and genus. */
  invariants() {
    const vertices = this.cells(ORBITS.vertex).length;
    const edges = this.cells(ORBITS.edge).length;
    const faces = this.cells(ORBITS.face).length;
    const components = this.cells(ORBITS.component).length;
    const euler = vertices - edges + faces;
    const boundaryLoops = this.boundaryLoops();
    const orientable = this.orientable();
    // chi = 2c - 2g - b for an orientable surface; chi = 2c - g - b counts crosscaps otherwise.
    const genus = orientable ? (2 * components - euler - boundaryLoops) / 2 : 2 * components - euler - boundaryLoops;
    return { darts: this.size, vertices, edges, faces, components, euler, boundaryLoops, orientable, genus };
  }
}

/**
 * G-map of a polygonal surface given as faces (lists of vertex indices). Each side of each face gets
 * two darts, one at each end (alpha0 between them); consecutive sides meet by alpha1 at their shared
 * vertex; two faces sharing a side are sewn by alpha2, dart to dart at the same vertex.
 * Returns the map and, for each dart, its face, side and vertex (for drawing it).
 */
export function fromFaces(faces) {
  const map = new GMap();
  const darts = [];
  const sides = new Map();
  faces.forEach((face, f) => {
    if (face.length < 3) throw new Error(`face ${f} has fewer than 3 vertices`);
    const first = map.size;
    face.forEach((v, s) => {
      const w = face[(s + 1) % face.length];
      const a = map.addDart();
      const b = map.addDart();
      darts.push({ face: f, side: s, vertex: v }, { face: f, side: s, vertex: w });
      map.link(0, a, b);
      const key = v < w ? `${v},${w}` : `${w},${v}`;
      const list = sides.get(key) ?? [];
      list.push({ a, b, v });
      sides.set(key, list);
    });
    for (let s = 0; s < face.length; s++) {
      const end = first + 2 * s + 1;
      const next = first + 2 * ((s + 1) % face.length);
      map.link(1, end, next);
    }
  });
  for (const [key, list] of sides) {
    if (list.length > 2) throw new Error(`side ${key} is shared by ${list.length} faces (not a surface)`);
    if (list.length === 2) {
      const [p, q] = list;
      // Dart at vertex v of one side to the dart at the same vertex v of the other.
      const qv = q.v === p.v ? q.a : q.b;
      const qw = q.v === p.v ? q.b : q.a;
      map.link(2, p.a, qv);
      map.link(2, p.b, qw);
    }
  }
  return { map, darts };
}

/** Faces of a cube, vertices 0..7 (bit 0 = x, bit 1 = y, bit 2 = z), every face seen from outside. */
export const CUBE_FACES = [
  [0, 2, 3, 1], [4, 5, 7, 6], [0, 1, 5, 4], [2, 6, 7, 3], [0, 4, 6, 2], [1, 3, 7, 5],
];

/** n x m grid of quads with wrapped columns; rows wrap too when closed (torus), else a cylinder. */
export function gridFaces(n, m, closed) {
  const idx = (i, j) => (i % n) * m + (j % m);
  const faces = [];
  for (let i = 0; i < n; i++) {
    for (let j = 0; j < (closed ? m : m - 1); j++) faces.push([idx(i, j), idx(i + 1, j), idx(i + 1, j + 1), idx(i, j + 1)]);
  }
  return faces;
}

/** Moebius strip of k quads: the last quad joins the two ends with a half twist. */
export function moebiusFaces(k) {
  const top = (i) => i;
  const bottom = (i) => k + i;
  const faces = [];
  for (let i = 0; i < k - 1; i++) faces.push([top(i), top(i + 1), bottom(i + 1), bottom(i)]);
  faces.push([top(k - 1), bottom(0), top(0), bottom(k - 1)]);
  return faces;
}
