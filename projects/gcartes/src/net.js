// The cube unfolded as a cross, for the course's figure. Each square lists its corners top-left,
// top-right, bottom-right, bottom-left; the letters name the cube's vertices, so a cube edge appears on
// exactly two squares (side by side in the net, or apart when the net is folded back).
//            Top
//     Left  Front  Right  Back
//           Bottom
import { fromFaces } from './gmap.js';

export const CUBE_NET = [
  { key: 'top', col: 1, row: 0, corners: ['e', 'f', 'b', 'a'] },
  { key: 'left', col: 0, row: 1, corners: ['e', 'a', 'd', 'h'] },
  { key: 'front', col: 1, row: 1, corners: ['a', 'b', 'c', 'd'] },
  { key: 'right', col: 2, row: 1, corners: ['b', 'f', 'g', 'c'] },
  { key: 'back', col: 3, row: 1, corners: ['f', 'e', 'h', 'g'] },
  { key: 'bottom', col: 1, row: 2, corners: ['d', 'c', 'g', 'h'] },
];

export const VERTEX_NAMES = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];

/** G-map of the net (48 darts) and, per dart, its square, side and vertex letter. */
export function cubeNetMap() {
  const faces = CUBE_NET.map((sq) => sq.corners.map((c) => VERTEX_NAMES.indexOf(c)));
  const { map, darts } = fromFaces(faces);
  return { map, darts: darts.map((d) => ({ ...d, square: CUBE_NET[d.face], letter: VERTEX_NAMES[d.vertex] })) };
}

/**
 * Where to draw each dart in a net of squares of the given size: a short segment along its side,
 * from 15 % to 45 % of the side starting at its vertex, moved inside the square; `end` is the vertex
 * end (drawn with a dot).
 */
export function dartGeometry(darts, size = 100, inset = 14) {
  return darts.map((d) => {
    const x0 = d.square.col * size;
    const y0 = d.square.row * size;
    const corner = [[0, 0], [size, 0], [size, size], [0, size]].map(([x, y]) => [x0 + x, y0 + y]);
    const s = d.side;
    const atStart = d.vertex === CUBE_NET[d.face].corners.map((c) => VERTEX_NAMES.indexOf(c))[s];
    const from = atStart ? corner[s] : corner[(s + 1) % 4];
    const to = atStart ? corner[(s + 1) % 4] : corner[s];
    const centre = [x0 + size / 2, y0 + size / 2];
    const along = (t) => [from[0] + (to[0] - from[0]) * t, from[1] + (to[1] - from[1]) * t];
    const mid = along(0.5);
    const len = Math.hypot(centre[0] - mid[0], centre[1] - mid[1]);
    const n = [((centre[0] - mid[0]) / len) * inset, ((centre[1] - mid[1]) / len) * inset];
    const shift = ([x, y]) => [x + n[0], y + n[1]];
    return { end: shift(along(0.15)), start: shift(along(0.45)) };
  });
}
