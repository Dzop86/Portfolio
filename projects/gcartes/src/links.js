// How a link is drawn, as in G-map textbooks (sprint 40): alpha0 a short stroke across the gap between the two darts
// of an edge, alpha1 an arc bending towards the corner where two darts of a face meet, alpha2 a double stroke between
// two faces. Screen coordinates; shared by the decomposition, the cube net and the tests.

/** Offset between the two strokes of an alpha2 link. */
export const DOUBLE_GAP = 1.8;

/**
 * SVG path data of an alpha_i link from point `a` to point `b`; `corner` is the corner an alpha1 arc bends towards
 * (the vertex the two darts share).
 */
export function linkPath(alpha, a, b, corner = null) {
  const f = (p) => `${p[0].toFixed(1)},${p[1].toFixed(1)}`;
  if (alpha === 1 && corner) {
    // Controlled by the corner: the arc passes halfway between the chord and the vertex, turning around it.
    return `M${f(a)} Q${f(corner)} ${f(b)}`;
  }
  if (alpha === 2) {
    const len = Math.hypot(b[0] - a[0], b[1] - a[1]) || 1;
    const n = [(-(b[1] - a[1]) / len) * (DOUBLE_GAP / 2), ((b[0] - a[0]) / len) * (DOUBLE_GAP / 2)];
    const plus = (p, s) => [p[0] + s * n[0], p[1] + s * n[1]];
    return `M${f(plus(a, 1))} L${f(plus(b, 1))} M${f(plus(a, -1))} L${f(plus(b, -1))}`;
  }
  return `M${f(a)} L${f(b)}`;
}
