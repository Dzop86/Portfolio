// The four steps that turn two squares sharing an edge into a G-map, as drawn in G-map textbooks:
//   0. the object: two faces, seven edges, six vertices;
//   1. cut by alpha2: the faces come apart, a blue alpha2 link remembers the shared edge;
//   2. cut by alpha1: each face falls into its sides, red alpha1 links remember the corners;
//   3. cut by alpha0: each side splits into two darts, a black alpha0 link remembers the side.
// Pure geometry (unit squares), shared by the course page and the tests.
import { fromFaces } from './gmap.js';

/** Two unit squares side by side: corners of A then B, each listed counter-clockwise. */
export const TWO_SQUARES = {
  points: [[0, 0], [1, 0], [2, 0], [2, 1], [1, 1], [0, 1]],
  faces: [[0, 1, 4, 5], [1, 2, 3, 4]],
};

const lerp = (a, b, t) => [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t];

/** How far each cut moves things apart, step by step: [face shrink, side shrink, dart gap as a share of half a side]. */
const GAPS = [[0, 0, 0], [0.1, 0, 0], [0.1, 0.12, 0], [0.1, 0.12, 0.3]];

/**
 * Shapes of step k (0 to 3): filled faces (step 0 only), plain sides (steps 0 to 2), darts (step 3),
 * vertex dots (step 0), and links [{ alpha, from, to }] between the pieces a cut separated.
 */
export function decompositionStep(k, shape = TWO_SQUARES) {
  const [faceGap, sideGap, dartGap] = GAPS[k];
  const { map, darts } = fromFaces(shape.faces);
  // Each face shrinks towards its centre, each side towards its middle, each half away from it.
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
      sides.push({ face: f, side: s, from: lerp(a, b, sideGap), to: lerp(b, a, sideGap) });
    });
  });
  // Dart d of fromFaces is side (d >> 1), at its start for even d and at its end for odd d.
  const dartGeom = darts.map((_, d) => {
    const side = sides[d >> 1];
    const mid = lerp(side.from, side.to, 0.5);
    const atStart = d % 2 === 0;
    const end = atStart ? side.from : side.to;
    // A dart runs from its vertex end to a point short of the middle, leaving a gap between the halves.
    return { end, inner: lerp(mid, end, dartGap) };
  });
  const links = [];
  const middleOfSide = (d) => lerp(sides[d >> 1].from, sides[d >> 1].to, 0.5);
  for (let d = 0; d < map.size; d++) {
    for (let i = 0; i <= 2; i++) {
      const e = map.alpha[i][d];
      if (e <= d) continue;
      if (i === 2 && k >= 1) {
        // Before the sides are cut, one alpha2 link per shared edge (between its two copies).
        if (k === 1) {
          if (d % 2 === 0) links.push({ alpha: 2, from: middleOfSide(d), to: middleOfSide(e) });
        } else {
          const p = k === 3 ? lerp(dartGeom[d].end, dartGeom[d].inner, 0.5) : (d % 2 === 0 ? sides[d >> 1].from : sides[d >> 1].to);
          const q = k === 3 ? lerp(dartGeom[e].end, dartGeom[e].inner, 0.5) : (e % 2 === 0 ? sides[e >> 1].from : sides[e >> 1].to);
          links.push({ alpha: 2, from: p, to: q });
        }
      }
      if (i === 1 && k >= 2) {
        const p = k === 3 ? dartGeom[d].end : (d % 2 === 0 ? sides[d >> 1].from : sides[d >> 1].to);
        const q = k === 3 ? dartGeom[e].end : (e % 2 === 0 ? sides[e >> 1].from : sides[e >> 1].to);
        links.push({ alpha: 1, from: p, to: q });
      }
      if (i === 0 && k === 3) links.push({ alpha: 0, from: dartGeom[d].inner, to: dartGeom[e].inner });
    }
  }
  return {
    faces: k === 0 ? faceCorners : [],
    sides: k < 3 ? sides.map((s) => [s.from, s.to]) : [],
    darts: k === 3 ? dartGeom.map((g) => [g.inner, g.end]) : [],
    dots: k === 0 ? shape.points : [],
    links,
  };
}
