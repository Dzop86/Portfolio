// The five steps that build the G-map of two squares sharing an edge by increasing dimension, as G-map textbooks
// define it (sprint 40):
//   0. the object: two faces, seven edges, six vertices;
//   1. alpha0: each side of each face becomes two darts, a black alpha0 link joins the two halves of an edge;
//   2. alpha1: in each face, the two darts that meet at a corner are linked by a red alpha1 arc;
//   3. alpha2: the two faces are sewn along the edge they share, blue double alpha2 strokes;
//   4. the G-map: 16 darts and their links, the faces back in place.
// Pure geometry (unit squares), shared by the course page and the tests.
import { fromFaces } from './gmap.js';

/** Two unit squares side by side: corners of A then B, each listed counter-clockwise. */
export const TWO_SQUARES = {
  points: [[0, 0], [1, 0], [2, 0], [2, 1], [1, 1], [0, 1]],
  faces: [[0, 1, 4, 5], [1, 2, 3, 4]],
};

export const STEPS = 5;

const lerp = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t];

/** Per step: [face shrink towards its centre, side shrink towards its middle, gap between the halves of a side]. */
const GAPS = [[0, 0, 0], [0.12, 0.1, 0.18], [0.12, 0.1, 0.18], [0.12, 0.1, 0.18], [0.06, 0.1, 0.18]];
// Which links each step draws.
const ALPHAS = [[], [0], [0, 1], [0, 1, 2], [0, 1, 2]];

/**
 * Shapes of step k (0 to 4): filled faces (steps 0 and 4), plain sides (step 0), darts [inner, end] (steps 1 to 4),
 * vertex dots (step 0), and links [{ alpha, darts: [d, e], from, to, corner }]: alpha0 between the inner ends of the
 * two darts of a side, alpha1 between their vertex ends with the face corner they turn around, alpha2 between the
 * middles of two darts facing each other across the shared edge.
 */
export function decompositionStep(k, shape = TWO_SQUARES) {
  const [faceGap, sideGap, dartGap] = GAPS[k];
  const { map, darts } = fromFaces(shape.faces);
  const faceCorners = shape.faces.map((face) => {
    const pts = face.map((v) => shape.points[v]);
    const c = [pts.reduce((s, p) => s + p[0], 0) / pts.length, pts.reduce((s, p) => s + p[1], 0) / pts.length];
    return pts.map((p) => lerp(p, c, faceGap));
  });
  const sides = [];
  shape.faces.forEach((face, f) => {
    face.forEach((_, s) => {
      const a = faceCorners[f][s];
      const b = faceCorners[f][(s + 1) % face.length];
      sides.push({ face: f, side: s, corners: [a, b], from: lerp(a, b, sideGap), to: lerp(b, a, sideGap) });
    });
  });
  // Dart d of fromFaces is side (d >> 1), at its start for even d and at its end for odd d.
  const dartGeom = darts.map((_, d) => {
    const side = sides[d >> 1];
    const mid = lerp(side.from, side.to, 0.5);
    const atStart = d % 2 === 0;
    const end = atStart ? side.from : side.to;
    return { end, inner: lerp(mid, end, dartGap), corner: atStart ? side.corners[0] : side.corners[1] };
  });
  const middle = (d) => lerp(dartGeom[d].end, dartGeom[d].inner, 0.5);
  const links = [];
  for (const i of ALPHAS[k]) {
    for (let d = 0; d < map.size; d++) {
      const e = map.alpha[i][d];
      if (e <= d) continue;
      if (i === 0) links.push({ alpha: 0, darts: [d, e], from: dartGeom[d].inner, to: dartGeom[e].inner, corner: null });
      if (i === 1) links.push({ alpha: 1, darts: [d, e], from: dartGeom[d].end, to: dartGeom[e].end, corner: dartGeom[d].corner });
      if (i === 2) links.push({ alpha: 2, darts: [d, e], from: middle(d), to: middle(e), corner: null });
    }
  }
  return {
    faces: k === 0 || k === 4 ? faceCorners : [],
    sides: k === 0 ? sides.map((s) => [s.from, s.to]) : [],
    darts: k > 0 ? dartGeom.map((g) => [g.inner, g.end]) : [],
    dots: k === 0 ? shape.points : [],
    links,
  };
}
