// Synthetic meshes for the benchmarks, and their OBJ, binary PLY and binary STL encodings.
// Deterministic: the same family and resolution always give the same bytes. No external data.

/**
 * Every family has exactly 4 k² triangles at resolution k, so sizes line up across families:
 * - torus: closed, genus 1 (χ = 0, no boundary);
 * - cylinder: open tube (χ = 0, two boundary loops);
 * - sphere: UV sphere closed by a fan at each pole (χ = 2).
 */
export const FAMILIES = {
  torus: { euler: 0, boundaryLoops: 0, make: torus },
  cylinder: { euler: 0, boundaryLoops: 2, make: cylinder },
  sphere: { euler: 2, boundaryLoops: 0, make: sphere },
};

export const FORMATS = ['obj', 'ply', 'stl'];

/** Mesh of a family at resolution k (k >= 3): { vertices: Float64Array (xyz), triangles: Uint32Array (abc) }. */
export function makeMesh(family, k) {
  if (!FAMILIES[family]) throw new Error(`Unknown family "${family}"`);
  if (!Number.isInteger(k) || k < 3) throw new Error(`Resolution must be an integer >= 3, got ${k}`);
  return FAMILIES[family].make(k);
}

// A grid of n columns (wrapping around) by m rows; rows wrap too when closed.
function grid(n, m, closedRows, point) {
  const vertices = new Float64Array(3 * n * m);
  for (let i = 0; i < n; i++) {
    for (let j = 0; j < m; j++) vertices.set(point(i, j), 3 * (i * m + j));
  }
  const bands = closedRows ? m : m - 1;
  const triangles = new Uint32Array(6 * n * bands);
  const idx = (i, j) => (i % n) * m + (j % m);
  let t = 0;
  for (let i = 0; i < n; i++) {
    for (let j = 0; j < bands; j++) {
      const a = idx(i, j), b = idx(i + 1, j), c = idx(i + 1, j + 1), d = idx(i, j + 1);
      triangles.set([a, b, c, a, c, d], t);
      t += 6;
    }
  }
  return { vertices, triangles };
}

function torus(k, R = 1, r = 0.4) {
  const n = 2 * k, m = k;
  return grid(n, m, true, (i, j) => {
    const u = (2 * Math.PI * i) / n, v = (2 * Math.PI * j) / m;
    return [(R + r * Math.cos(v)) * Math.cos(u), (R + r * Math.cos(v)) * Math.sin(u), r * Math.sin(v)];
  });
}

function cylinder(k) {
  const n = 2 * k, m = k + 1;
  return grid(n, m, false, (i, j) => {
    const u = (2 * Math.PI * i) / n;
    return [Math.cos(u), Math.sin(u), (2 * j) / (m - 1) - 1];
  });
}

function sphere(k) {
  // Rings 1..k strictly between the poles, then the two poles; 2 k (k - 1) band triangles + 2 · 2 k fan triangles.
  const n = 2 * k, rings = k;
  const ring = grid(n, rings, false, (i, j) => {
    const u = (2 * Math.PI * i) / n, v = (Math.PI * (j + 1)) / (rings + 1);
    return [Math.sin(v) * Math.cos(u), Math.sin(v) * Math.sin(u), Math.cos(v)];
  });
  const nv = n * rings;
  const vertices = new Float64Array(3 * (nv + 2));
  vertices.set(ring.vertices);
  vertices.set([0, 0, 1, 0, 0, -1], 3 * nv);
  const triangles = new Uint32Array(ring.triangles.length + 6 * n);
  triangles.set(ring.triangles);
  let t = ring.triangles.length;
  for (let i = 0; i < n; i++) {
    const i1 = (i + 1) % n;
    // Same orientation as the bands: north fan, then south fan.
    triangles.set([nv, i1 * rings, i * rings], t);
    triangles.set([nv + 1, i * rings + rings - 1, i1 * rings + rings - 1], t + 3);
    t += 6;
  }
  return { vertices, triangles };
}

/** Encodes a mesh as bytes in one of FORMATS. */
export function encode(mesh, format) {
  if (format === 'obj') return encodeObj(mesh);
  if (format === 'ply') return encodePly(mesh);
  if (format === 'stl') return encodeStl(mesh);
  throw new Error(`Unknown format "${format}"`);
}

function encodeObj({ vertices, triangles }) {
  const lines = ['# Synthetic mesh, projects/sql/bench/meshes.mjs'];
  for (let i = 0; i < vertices.length; i += 3) {
    lines.push(`v ${vertices[i].toFixed(6)} ${vertices[i + 1].toFixed(6)} ${vertices[i + 2].toFixed(6)}`);
  }
  for (let i = 0; i < triangles.length; i += 3) {
    lines.push(`f ${triangles[i] + 1} ${triangles[i + 1] + 1} ${triangles[i + 2] + 1}`);
  }
  return new TextEncoder().encode(`${lines.join('\n')}\n`);
}

function encodePly({ vertices, triangles }) {
  const nv = vertices.length / 3, nt = triangles.length / 3;
  const header = new TextEncoder().encode([
    'ply', 'format binary_little_endian 1.0', 'comment Synthetic mesh, projects/sql/bench/meshes.mjs',
    `element vertex ${nv}`, 'property float x', 'property float y', 'property float z',
    `element face ${nt}`, 'property list uchar int vertex_indices', 'end_header', '',
  ].join('\n'));
  const out = new Uint8Array(header.length + 12 * nv + 13 * nt);
  out.set(header);
  const view = new DataView(out.buffer);
  let p = header.length;
  for (let i = 0; i < vertices.length; i++, p += 4) view.setFloat32(p, vertices[i], true);
  for (let i = 0; i < triangles.length; i += 3) {
    view.setUint8(p, 3);
    for (let c = 0; c < 3; c++) view.setInt32(p + 1 + 4 * c, triangles[i + c], true);
    p += 13;
  }
  return out;
}

function encodeStl({ vertices, triangles }) {
  const nt = triangles.length / 3;
  const out = new Uint8Array(84 + 50 * nt);
  out.set(new TextEncoder().encode('Synthetic mesh, projects/sql/bench/meshes.mjs'));
  const view = new DataView(out.buffer);
  view.setUint32(80, nt, true);
  let p = 84;
  for (let t = 0; t < triangles.length; t += 3) {
    const [a, b, c] = [0, 1, 2].map((s) => 3 * triangles[t + s]);
    const e1 = [0, 1, 2].map((x) => vertices[b + x] - vertices[a + x]);
    const e2 = [0, 1, 2].map((x) => vertices[c + x] - vertices[a + x]);
    const nrm = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]];
    const len = Math.hypot(...nrm) || 1;
    nrm.forEach((x, i) => view.setFloat32(p + 4 * i, x / len, true));
    [a, b, c].forEach((v, s) => {
      for (let x = 0; x < 3; x++) view.setFloat32(p + 12 + 12 * s + 4 * x, vertices[v + x], true);
    });
    p += 50; // the last two bytes (attribute count) stay 0
  }
  return out;
}
