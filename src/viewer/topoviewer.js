// Topology viewer: a mesh coloured by Gaussian curvature, or by its height with the height filtration and
// its critical points (sprint 36), its persistence diagram (sprint 37), barcode and guided explanation (sprint 39) and
// its Reeb graph (sprint 38), with its invariants, all computed by the C++ library of
// projects/topologie compiled to WebAssembly. Bundled with three.js at build time (D17).
import {
  AmbientLight, BufferAttribute, BufferGeometry, Color, DirectionalLight, DoubleSide, Group, LineBasicMaterial, LineSegments, Mesh,
  MeshBasicMaterial, MeshStandardMaterial, PerspectiveCamera, Raycaster, Scene, SphereGeometry, Vector2, WebGLRenderer,
} from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import {
  AXES, aliveAt, barcode, clipArcs, criticalCounts, diagram, duration, elevation, filtration, interiorCurvature, levelBetween, loadTopo, nodeKind,
  persistence, quantileScale, readTopology, reeb, timed, tour, turns,
} from '../assets/topo-api.js';

const root = document.querySelector('[data-topo-viewer]');
if (root) start(root);

function start(root) {
  const labels = JSON.parse(root.dataset.labels);
  const lang = document.documentElement.lang;
  const canvas = root.querySelector('canvas');
  const stage = root.querySelector('.viewer-stage');
  const tip = root.querySelector('.viewer-tip');
  const result = root.querySelector('[data-result]');
  const errorBox = root.querySelector('[data-error]');
  const legend = root.querySelector('[data-legend]');
  const heightPanel = root.querySelector('[data-height]');
  const modes = [...root.querySelectorAll('[data-mode]')];
  const axisSelect = root.querySelector('[data-axis]');
  const level = root.querySelector('[data-level]');
  const levelValue = root.querySelector('[data-level-value]');
  const sublevel = root.querySelector('[data-sublevel]');
  const criticalText = root.querySelector('[data-critical]');
  const chi = root.querySelector('[data-chi]');
  const tau = root.querySelector('[data-tau]');
  const tauValue = root.querySelector('[data-tau-value]');
  const persSummary = root.querySelector('[data-pers-summary]');
  const persHidden = root.querySelector('[data-pers-hidden]');
  const persPlot = root.querySelector('[data-diagram]');
  const persRows = root.querySelector('[data-pers-table] tbody');
  const persDetails = persRows.closest('details');
  const bars = root.querySelector('[data-bars]');
  const barsAlive = root.querySelector('[data-bars-alive]');
  const barsHidden = root.querySelector('[data-bars-hidden]');
  const pickText = root.querySelector('[data-pick]');
  const tourBox = root.querySelector('[data-tour]');
  const tourStep = root.querySelector('[data-tour-step]');
  const tourText = root.querySelector('[data-tour-text]');
  const tourPrev = root.querySelector('[data-tour-prev]');
  const tourNext = root.querySelector('[data-tour-next]');
  const tourLead = tourText.textContent;
  const times = Object.fromEntries([...root.querySelectorAll('[data-time]')].map((el) => [el.dataset.time, el]));
  const shapes = JSON.parse(root.dataset.shapes);
  const reebShow = root.querySelector('[data-reeb-show]');
  const reebSummary = root.querySelector('[data-reeb-summary]');
  const reebFlat = root.querySelector('[data-reeb-flat]');
  const fill = (text, vars) => text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));
  const num = (n, digits = 3) => n.toLocaleString(lang, { maximumFractionDigits: digits });

  // WebAssembly is loaded from the published assets, next to this bundle.
  let lib;
  const topo = () => (lib ??= import(new URL('./wasm/topo.js', import.meta.url).href).then((m) => loadTopo(m.default)));

  // --- 3D scene; the invariants still work without WebGL. -------------------------------------
  let renderer = null;
  try {
    renderer = new WebGLRenderer({ canvas, antialias: true, alpha: true });
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  } catch {
    stage.replaceChildren(Object.assign(document.createElement('p'), { className: 'notice', textContent: labels.nowebgl }));
  }
  const scene = new Scene();
  const camera = new PerspectiveCamera(40, 1, 0.01, 100);
  camera.position.set(1.6, 1.4, 2.2);
  const light = new DirectionalLight(0xffffff, 2.2);
  camera.add(light);
  light.position.set(1, 1, 2);
  scene.add(camera, new AmbientLight(0xffffff, 0.9));
  const controls = renderer ? new OrbitControls(camera, canvas) : null;
  const render = () => renderer?.render(scene, camera);
  controls?.addEventListener('change', render);
  let shape = null;

  // CSS sizes the canvas (aspect-ratio); this only reads it, so it never resizes what it observes.
  function resize() {
    if (!renderer) return;
    const { clientWidth: width, clientHeight: height } = canvas;
    if (!width || !height) return;
    renderer.setSize(width, height, false);
    camera.aspect = width / height;
    camera.updateProjectionMatrix();
    render();
  }
  new ResizeObserver(resize).observe(canvas);

  // Diverging scale from the charter tokens: saddle (K < 0), flat, dome (K > 0).
  const token = (name) => new Color(getComputedStyle(root).getPropertyValue(name).trim());

  // Diverging scale by quantiles of |K| over interior vertices (see quantileScale): readable on smooth
  // samples and on sculpted models whose creases reach 1000 times the median curvature.
  function colours(r) {
    const values = interiorCurvature(r.curvature, r.boundary, r.indices);
    const scale = quantileScale(r.curvature, r.boundary);
    const [neg, zero, pos] = ['--curv-neg', '--curv-zero', '--curv-pos'].map(token);
    const out = new Float32Array(3 * values.length);
    const c = new Color();
    values.forEach((k, i) => {
      const t = scale.t(k);
      c.copy(zero).lerp(t < 0 ? neg : pos, Math.abs(t));
      out.set([c.r, c.g, c.b], 3 * i);
    });
    return { out, ticks: scale.ticks };
  }

  // --- height filtration (sprint 36) --------------------------------------------------------------
  const mode = () => modes.find((m) => m.checked)?.value ?? 'curvature';
  const markers = new Group();
  scene.add(markers);

  // Sequential scale from the charter: chocolate at the lowest vertex, pistachio at the highest.
  function heightColours(e) {
    const [low, high] = ['--elev-low', '--elev-high'].map(token);
    const lo = e.height[e.order[0]], hi = e.height[e.order[e.order.length - 1]];
    const out = new Float32Array(3 * e.height.length);
    const c = new Color();
    e.height.forEach((h, i) => {
      c.copy(low).lerp(high, hi > lo ? (h - lo) / (hi - lo) : 0);
      out.set([c.r, c.g, c.b], 3 * i);
    });
    return out;
  }

  // The elevation of the mesh shown, along the chosen axis; recomputed by the C++ code when the axis changes.
  function computeHeight(r) {
    // Each step of the C++ code timed as the visitor waits for it (sprint 40).
    const e = timed(() => elevation(r.lib, AXES[axisSelect.value]));
    r.elevation = e.value;
    r.filtration = filtration(r.indices, r.elevation);
    r.heightColours = heightColours(r.elevation);
    r.counts = criticalCounts(r.elevation.critical);
    const p = timed(() => persistence(r.lib, r.elevation));
    r.persistence = p.value;
    r.pairIndex = new Map((r.persistence.pairs ?? []).map((q, i) => [q, i]));
    const g = timed(() => reeb(r.lib));
    r.reeb = g.value;
    showTime('height', e.ms);
    showTime('persistence', r.persistence.tooLarge ? null : p.ms);
    showTime('reeb', r.reeb.tooLarge ? null : g.ms);
    const { height, order } = r.elevation;
    r.lo = height[order[0]];
    r.hi = height[order[order.length - 1]];
    r.tour = r.persistence.tooLarge ? [] : tour(r.persistence.pairs, r.lo, r.hi);
    r.tourAt = -1;
    r.picked = null;
  }

  function showTime(step, ms) {
    times[step].hidden = ms === null;
    if (ms !== null) times[step].textContent = fill(labels[step === 'read' ? 'time.read' : 'time'], { t: duration(ms, lang) });
  }

  // One sphere and one material per kind, shared by every marker and every mesh: clearing the group frees
  // nothing on the GPU because nothing new was allocated.
  const ball = new SphereGeometry(0.03, 12, 8);
  const markerMaterial = Object.fromEntries(Object.entries({ min: '--crit-min', saddle: '--crit-saddle', max: '--crit-max', other: '--crit-saddle' })
    .map(([kind, name]) => [kind, new MeshBasicMaterial({ color: token(name) })]));

  function placeMarkers(r) {
    markers.clear();
    halos.clear();
    pickText.hidden = true;
    for (const p of r.elevation.critical) {
      const m = new Mesh(ball, markerMaterial[p.kind]);
      m.position.set(r.positions[3 * p.vertex], r.positions[3 * p.vertex + 1], r.positions[3 * p.vertex + 2]);
      m.userData.rank = r.filtration.rank[p.vertex];
      m.userData.vertex = p.vertex;
      markers.add(m);
    }
  }

  // Step curve of chi(sublevel) against the height, with the threshold as a dashed line.
  function drawChi(r, h) {
    const { height, order, euler } = r.elevation;
    const lo = height[order[0]], hi = height[order[order.length - 1]];
    // A loop, not Math.max(...euler): a dropped file may have more vertices than a call has arguments.
    let top = 0, bottom = 0;
    for (const c of euler) { top = Math.max(top, c); bottom = Math.min(bottom, c); }
    const x = (v) => 30 + (hi > lo ? ((v - lo) / (hi - lo)) * 260 : 0);
    const y = (c) => 10 + (top > bottom ? ((top - c) / (top - bottom)) * 90 : 45);
    // Only the steps: chi changes at critical vertices, so the path stays short on large meshes.
    let d = `M${x(lo)},${y(0)}`, last = 0;
    order.forEach((v, k) => {
      if (euler[k] === last) return;
      d += ` H${x(height[v]).toFixed(1)} V${y(euler[k]).toFixed(1)}`;
      last = euler[k];
    });
    d += ` H${x(hi)}`;
    const ticks = [...new Set([top, 0, bottom])].map((c) => `<text x="0" y="${y(c) + 4}">${c}</text>`).join('');
    chi.innerHTML = `${chi.querySelector('title').outerHTML}<line class="chi-axis" x1="30" x2="290" y1="${y(0)}" y2="${y(0)}"/>${ticks}`
      + `<path class="chi-line" d="${d}"/><line class="chi-level" x1="${x(h)}" x2="${x(h)}" y1="5" y2="105"/>`;
  }

  // Keep the triangles whose three vertices are at or below the threshold, and the critical points there.
  function applyLevel() {
    if (!shape) return;
    const r = shape.userData;
    if (mode() !== 'height') {
      shape.geometry.setDrawRange(0, Infinity);
      markers.visible = false;
      halos.visible = false;
      reebLines.visible = false;
      Object.assign(shape.material, { transparent: false, opacity: 1, depthWrite: true, needsUpdate: true });
      render();
      return;
    }
    const { height, order, euler } = r.elevation;
    const lo = height[order[0]], hi = height[order[order.length - 1]];
    const h = lo + (Number(level.value) / 1000) * (hi - lo);
    const rank = r.filtration.rankAt(h);
    const faces = rank < 0 ? 0 : r.filtration.faces(rank);
    shape.geometry.setDrawRange(0, 3 * faces);
    markers.visible = true;
    halos.visible = true;
    // Under the level, and not only the end of pairs shorter than the persistence threshold.
    const kept = r.persistence.tooLarge ? null : diagram(r.persistence.pairs, persistenceThreshold(r)).vertices;
    for (const m of markers.children) m.visible = m.userData.rank <= rank && (!kept || kept.has(m.userData.vertex));
    drawReebLines(r, h);
    const y = reebFlat.querySelector('[data-reeb-level]');
    if (y && r.flat) {
      const at = r.flat.y(h).toFixed(1);
      y.setAttribute('y1', at);
      y.setAttribute('y2', at);
    }
    levelValue.textContent = fill(labels['height.value'], { h: num(h - lo, 2), max: num(hi - lo, 2) });
    sublevel.textContent = fill(labels.sublevel, { faces: num(faces), total: num(r.indices.length / 3), chi: rank < 0 ? 0 : euler[rank] });
    drawChi(r, h);
    levelOnBars(r, h, rank < 0 ? 0 : euler[rank]);
    render();
  }

  // --- persistence diagram (sprint 37) ----------------------------------------------------------
  // The threshold is a share of the height range, like the level: 0 to 1000 thousandths.
  function persistenceThreshold(r) {
    const { height, order } = r.elevation;
    return (Number(tau.value) / 1000) * (height[order[order.length - 1]] - height[order[0]]);
  }

  const TABLE_ROWS = 100;

  function drawPersistence() {
    if (!shape) return;
    const r = shape.userData;
    const p = r.persistence;
    const parts = [persPlot, tau.closest('.rt-slider'), persDetails, bars, barsAlive, tourBox];
    parts.forEach((el) => el.toggleAttribute('hidden', Boolean(p.tooLarge)));
    if (p.tooLarge) {
      persSummary.textContent = fill(labels['pers.toolarge'], { limit: num(p.limit) });
      persHidden.hidden = true;
      barsHidden.hidden = true;
      return;
    }
    const { height, order } = r.elevation;
    const lo = height[order[0]], hi = height[order[order.length - 1]];
    const t = persistenceThreshold(r);
    const kept = diagram(p.pairs, t);
    const all = diagram(p.pairs, 0);
    tauValue.textContent = fill(labels['height.value'], { h: num(t, 2), max: num(hi - lo, 2) });
    persSummary.textContent = fill(labels['pers.summary'], {
      h0: kept.finite[0], h1: kept.finite[1], h2: kept.finite[2], b0: p.betti[0], b1: p.betti[1], b2: p.betti[2],
    });
    persHidden.hidden = all.hidden === 0;
    persHidden.textContent = fill(labels['pers.hidden'], { n: num(all.hidden) });

    // Birth across, death up, both from the lowest to the highest vertex; the essential classes on the ∞ line.
    const span = hi > lo ? hi - lo : 1;
    const x = (v) => 40 + ((v - lo) / span) * 245;
    const y = (v) => (v === Infinity ? 14 : 285 - ((v - lo) / span) * 245);
    const dimName = (d) => labels[`pers.dim.${d}`];
    const value = (v) => (v === Infinity ? labels['pers.never'] : num(v - lo, 2));
    const marks = all.drawn.map((q) => {
      const noise = q.death - q.birth <= t;
      return `<g ${option(r, q)} class="pers-point${q.deathVertex === null ? ' is-essential' : ''}${noise ? ' is-noise' : ''}" transform="translate(${x(q.birth).toFixed(1)} ${y(q.death).toFixed(1)})">`
        + `<circle class="pers-ring" r="7"/><path class="pers-shape is-h${q.dimension}" d="${shapes[q.dimension]}"/></g>`;
    }).join('');
    // The noise band: points under the dashed line live less than the threshold.
    // Between the diagonal and the line death = birth + threshold.
    const tt = Math.min(t, span);
    const band = t > 0 ? `<path class="pers-band" d="M${x(lo)},${y(lo)} L${x(hi)},${y(hi)} L${x(hi - tt)},${y(hi)} L${x(lo)},${y(lo + tt)} Z"/>` : '';
    // Axes and labels are drawing only: inside the listbox, screen readers meet the pairs alone.
    persPlot.innerHTML = `${persPlot.querySelector('desc').outerHTML}<g aria-hidden="true">`
      + `<line class="pers-axis" x1="40" x2="285" y1="285" y2="285"/><line class="pers-axis" x1="40" x2="40" y1="40" y2="285"/>`
      + `<line class="pers-infinity" x1="40" x2="285" y1="14" y2="14"/><text x="4" y="18">∞</text>`
      + `<text x="4" y="289">0</text><text x="285" y="299" text-anchor="end">${num(hi - lo, 2)}</text>`
      + `<text x="162" y="314" text-anchor="middle">${escapeXml(labels['pers.axis.birth'])}</text>`
      + `<text transform="translate(30 162) rotate(-90)" text-anchor="middle">${escapeXml(labels['pers.axis.death'])}</text>`
      + band + `<line class="pers-diagonal" x1="${x(lo)}" y1="${y(lo)}" x2="${x(hi)}" y2="${y(hi)}"/></g>${marks}`;
    drawBars(r, t);

    // The table, for screen readers and for reading values: the kept pairs, the most persistent first.
    persRows.innerHTML = kept.drawn.slice(0, TABLE_ROWS).map((q) => `<tr data-pair="${r.pairIndex.get(q)}"><th scope="row">`
      + `<button type="button" class="pers-pick" data-pair="${r.pairIndex.get(q)}" aria-pressed="false">${escapeXml(dimName(q.dimension))}</button></th>`
      + `<td class="num">${value(q.birth)}</td><td class="num">${value(q.death)}</td>`
      + `<td class="num">${q.deathVertex === null ? '∞' : num(q.death - q.birth, 2)}</td></tr>`).join('')
      + (kept.kept.length > TABLE_ROWS ? `<tr><td colspan="4">${escapeXml(fill(labels['pers.more'], { n: num(kept.kept.length - TABLE_ROWS) }))}</td></tr>` : '');
    markPicked(r);
  }

  // --- barcode and picked pair (sprint 39) --------------------------------------------------------
  // Bars and points are options of two listboxes, reached with Tab then the arrows (roving tabindex); their index in
  // the pairs is shared with the table, so that a pair picked anywhere is marked everywhere.
  function option(r, q) {
    const label = fill(labels['pers.point'], { dim: labels[`pers.dim.${q.dimension}`], birth: value(r, q.birth), death: value(r, q.death) });
    return `role="option" tabindex="-1" aria-selected="false" aria-label="${escapeXml(label)}" data-pair="${r.pairIndex.get(q)}"`;
  }

  function value(r, v) {
    return v === Infinity ? labels['pers.never'] : num(v - r.lo, 2);
  }

  const BAR_STEP = 10;

  function drawBars(r, t) {
    const { lo, hi } = r;
    const span = hi > lo ? hi - lo : 1;
    const x = (v) => 40 + ((Math.min(v, hi) - lo) / span) * 245;
    const { groups, hidden } = barcode(r.persistence.pairs);
    let top = 4;
    let drawing = '', options = '';
    groups.forEach((group, d) => {
      if (group.length === 0) return;
      drawing += `<text class="bars-group" x="4" y="${top + 9}">${escapeXml(labels[`pers.dim.${d}`])}</text>`;
      top += 15;
      for (const q of group) {
        const essential = q.death === Infinity;
        const x0 = x(q.birth), x1 = essential ? 291 : Math.max(x(q.death), x0 + 1.5);
        const arrow = essential ? `<path class="bar-line is-h${d}" d="M291,${top + 0.5} l6,4 l-6,4Z"/>` : '';
        options += `<g ${option(r, q)} class="bar${q.death - q.birth <= t ? ' is-noise' : ''}">`
          + `<rect class="bar-hit" x="0" y="${top}" width="300" height="${BAR_STEP}"/>`
          + `<rect class="bar-line is-h${d}" x="${x0.toFixed(1)}" y="${top + 2}" width="${(x1 - x0).toFixed(1)}" height="5"/>${arrow}</g>`;
        top += BAR_STEP;
      }
    });
    const axis = top + 4;
    bars.setAttribute('viewBox', `0 0 300 ${axis + 26}`);
    bars.innerHTML = `${bars.querySelector('desc').outerHTML}<g aria-hidden="true">${drawing}`
      + `<line class="pers-axis" x1="40" x2="291" y1="${axis}" y2="${axis}"/>`
      + `<text x="40" y="${axis + 12}" text-anchor="middle">0</text><text x="285" y="${axis + 12}" text-anchor="end">${num(hi - lo, 2)}</text>`
      + `<text x="162" y="${axis + 24}" text-anchor="middle">${escapeXml(labels['bars.axis'])}</text>`
      + `<line class="bars-level" data-bars-level x1="40" x2="40" y1="0" y2="${axis}"/></g>${options}`;
    barsHidden.hidden = hidden === 0;
    barsHidden.textContent = fill(labels['bars.hidden'], { n: num(hidden) });
  }

  // The threshold across the bars: those it crosses are alive at that level (their count is chi).
  function levelOnBars(r, h, chiAt) {
    if (r.persistence.tooLarge) return;
    const line = bars.querySelector('[data-bars-level]');
    const at = (40 + ((h - r.lo) / (r.hi > r.lo ? r.hi - r.lo : 1)) * 245).toFixed(1);
    line.setAttribute('x1', at);
    line.setAttribute('x2', at);
    const { pairs } = r.persistence;
    for (const el of bars.querySelectorAll('.bar')) {
      const q = pairs[Number(el.dataset.pair)];
      el.classList.toggle('is-alive', q.birth <= h && h < q.death);
    }
    const [h0, h1, h2] = aliveAt(pairs, h);
    barsAlive.textContent = fill(labels['bars.alive'], { h0, h1, h2, chi: chiAt });
  }

  // Halos around the vertices of the picked pair, seen through the mesh.
  const halos = new Group();
  halos.renderOrder = 3;
  scene.add(halos);
  const haloBall = new SphereGeometry(0.065, 16, 12);
  // In the text colour: pistachio, the top of the height scale, would vanish on the highest vertices.
  const haloMaterial = new MeshBasicMaterial({ color: token('--text-strong'), transparent: true, opacity: 0.6, depthTest: false });

  function markPicked(r) {
    for (const el of root.querySelectorAll('[data-pair]')) {
      const on = Number(el.dataset.pair) === r.picked;
      el.classList.toggle('is-picked', on);
      if (el.getAttribute('role') === 'option') el.setAttribute('aria-selected', String(on));
      if (el.tagName === 'BUTTON') el.setAttribute('aria-pressed', String(on));
    }
    // Roving tabindex: the picked option, or the first one, is the listbox's tab stop.
    for (const list of [bars, persPlot]) {
      const options = [...list.querySelectorAll('[role=option]')];
      const stop = options.find((o) => o.classList.contains('is-picked')) ?? options[0];
      options.forEach((o) => o.setAttribute('tabindex', o === stop ? '0' : '-1'));
    }
  }

  // Picks a pair (its index in the pairs, or null): marks it everywhere, circles its vertices and, with `move`, puts
  // the height threshold between its birth and its death.
  function pick(index, { move = true } = {}) {
    if (!shape) return;
    const r = shape.userData;
    if (index === r.picked && !move) return;
    r.picked = index;
    halos.clear();
    markPicked(r);
    if (index === null) {
      pickText.hidden = true;
      render();
      return;
    }
    const q = r.persistence.pairs[index];
    for (const v of [q.birthVertex, q.deathVertex]) {
      if (v === null) continue;
      const m = new Mesh(haloBall, haloMaterial);
      m.renderOrder = 3;
      m.position.set(r.positions[3 * v], r.positions[3 * v + 1], r.positions[3 * v + 2]);
      halos.add(m);
    }
    pickText.textContent = fill(labels[q.deathVertex === null ? 'pick.never' : 'pick.dies'], {
      dim: labels[`pers.dim.${q.dimension}`], bv: q.birthVertex, dv: q.deathVertex, birth: value(r, q.birth), death: value(r, q.death),
    });
    pickText.hidden = false;
    if (move) {
      level.value = String(levelBetween(q, r.lo, r.hi));
      applyLevel();
    } else render();
  }

  const pairOf = (el) => el?.closest('[data-pair]');
  // A real move of a mouse: picking shows a sentence that shifts the page, and WebKit then sends a `pointermove` where the
  // pointer already was, for whatever slid under it (`movementX` does not tell: WebKit leaves it at 0 for real moves
  // too). The previous position is kept for the whole page; before any move, nothing is a hover. A finger picks by `click`.
  let before = null, now = null;
  document.addEventListener('pointermove', (e) => { before = now; now = `${e.clientX},${e.clientY}`; }, true);
  function hover(e) {
    if (before === null || before === now) return;
    const el = pairOf(e.target);
    if (e.pointerType === 'mouse' && el && Number(el.dataset.pair) !== shape?.userData.picked) pick(Number(el.dataset.pair));
  }
  for (const list of [bars, persPlot]) {
    list.addEventListener('pointermove', hover);
    list.addEventListener('click', (e) => { const el = pairOf(e.target); if (el) pick(Number(el.dataset.pair)); });
    // `focus` in the capture phase: WebKit sends no `focusin` for SVG elements.
    list.addEventListener('focus', (e) => { const el = pairOf(e.target); if (el && Number(el.dataset.pair) !== shape?.userData.picked) pick(Number(el.dataset.pair)); }, true);
    list.addEventListener('keydown', (e) => {
      const options = [...list.querySelectorAll('[role=option]')];
      const k = options.indexOf(document.activeElement);
      const to = { ArrowDown: k + 1, ArrowRight: k + 1, ArrowUp: k - 1, ArrowLeft: k - 1, Home: 0, End: options.length - 1 }[e.key];
      if (to === undefined || options.length === 0) return;
      e.preventDefault();
      const target = options[Math.max(0, Math.min(options.length - 1, to))];
      target.focus();
      if (Number(target.dataset.pair) !== shape?.userData.picked) pick(Number(target.dataset.pair));
    });
  }
  persRows.addEventListener('click', (e) => { const el = e.target.closest('button[data-pair]'); if (el) pick(Number(el.dataset.pair)); });
  persRows.addEventListener('pointermove', hover);

  // --- guided explanation (sprint 39) ----------------------------------------------------------------
  function showTour() {
    const r = shape?.userData;
    if (!r) return;
    const steps = r.tour;
    const k = r.tourAt;
    tourPrev.disabled = k <= 0;
    tourNext.disabled = k >= steps.length - 1;
    tourNext.textContent = k < 0 ? labels['tour.start'] : labels['tour.next'];
    if (k < 0) {
      tourStep.textContent = '';
      tourText.textContent = tourLead;
      return;
    }
    const step = steps[k];
    const q = step.pair;
    const h = q === null ? r.hi : ['merge', 'fill'].includes(step.key) ? q.death : q.birth;
    tourStep.textContent = fill(labels['tour.step'], { k: k + 1, n: steps.length });
    const [b0, b1, b2] = r.persistence.betti;
    tourText.textContent = fill(labels[`tour.${step.key}`], { h: num(h - r.lo, 2), b0, b1, b2 });
    level.value = String(step.level);
    applyLevel();
    pick(q === null ? null : r.pairIndex.get(q), { move: false });
  }

  tourNext.addEventListener('click', () => { if (shape) { shape.userData.tourAt += 1; showTour(); } });
  tourPrev.addEventListener('click', () => { if (shape) { shape.userData.tourAt -= 1; showTour(); } });

  function escapeXml(text) {
    return String(text).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
  }

  // --- Reeb graph (sprint 38) ------------------------------------------------------------------
  // Inside the mesh: the arcs below the threshold, drawn over everything (they run inside the volume), the mesh
  // made transparent while they show. One geometry, rebuilt when the threshold moves.
  const reebLines = new LineSegments(new BufferGeometry(), new LineBasicMaterial({ color: token('--text-strong'), depthTest: false, transparent: true }));
  reebLines.renderOrder = 2;
  scene.add(reebLines);

  function drawReebLines(r, h) {
    const on = reebShow.checked && !r.reeb.tooLarge;
    reebLines.visible = on;
    shape.material.transparent = on;
    shape.material.opacity = on ? 0.35 : 1;
    shape.material.depthWrite = !on;
    shape.material.needsUpdate = true;
    if (!on) return;
    const segments = [];
    for (const line of clipArcs(r.reeb, r.positions, r.elevation.height, AXES[axisSelect.value], h)) {
      for (let k = 3; k < line.length; k += 3) segments.push(...line.slice(k - 3, k + 3));
    }
    reebLines.geometry.dispose();
    reebLines.geometry = new BufferGeometry();
    reebLines.geometry.setAttribute('position', new BufferAttribute(new Float32Array(segments), 3));
  }

  // Flat, seen from the front: height up, across it the axis of the mesh that the height does not use.
  const ACROSS = { y: 0, x: 2, z: 0 };

  function drawReeb(r) {
    const g = r.reeb;
    reebFlat.toggleAttribute('hidden', Boolean(g.tooLarge));
    if (g.tooLarge) {
      reebSummary.textContent = fill(labels[`reeb.${g.tooLarge}`], { limit: num(g.limit) });
      r.flat = null;
      return;
    }
    const inv = r.invariants;
    const closed = inv.genus !== null && inv.boundaryLoops === 0;
    const check = closed ? fill(labels['reeb.genus'], { genus: inv.genus })
      : r.persistence.tooLarge ? '' : fill(labels['reeb.bound'], { b1: r.persistence.betti[1] });
    reebSummary.textContent = `${fill(labels['reeb.summary'], { nodes: num(g.nodes.length), arcs: num(g.arcs.length), loops: num(g.loops) })} ${check}`.trim();

    const { height, order } = r.elevation;
    const across = ACROSS[axisSelect.value];
    const p = r.positions;
    let left = Infinity, right = -Infinity;
    for (let k = across; k < p.length; k += 3) { left = Math.min(left, p[k]); right = Math.max(right, p[k]); }
    const lo = height[order[0]], hi = height[order[order.length - 1]];
    // Same scale both ways, so that the graph keeps the mesh's proportions.
    const span = Math.max(right - left, hi - lo, 1e-9);
    const x = (u) => 150 + ((u - (left + right) / 2) / span) * 260;
    const y = (h) => 150 - ((h - (lo + hi) / 2) / span) * 260;
    r.flat = { y };
    const dir = AXES[axisSelect.value];
    const arcs = g.arcs.map((a) => {
      const from = g.nodes[a.lower].vertex, to = g.nodes[a.upper].vertex;
      const pts = [[p[3 * from + across], height[from]]];
      for (let k = 0; k < a.path.length; k += 3) {
        pts.push([a.path[k + across], a.path[k] * dir[0] + a.path[k + 1] * dir[1] + a.path[k + 2] * dir[2]]);
      }
      pts.push([p[3 * to + across], height[to]]);
      return `<polyline class="reeb-arc" points="${pts.map(([u, h]) => `${x(u).toFixed(1)},${y(h).toFixed(1)}`).join(' ')}"/>`;
    }).join('');
    const nodes = g.nodes.map((n) => `<circle class="reeb-node is-${nodeKind(n)}" cx="${x(p[3 * n.vertex + across]).toFixed(1)}" cy="${y(height[n.vertex]).toFixed(1)}" r="4"/>`).join('');
    reebFlat.innerHTML = `${reebFlat.querySelector('desc').outerHTML}`
      + `<line class="reeb-level" data-reeb-level x1="10" x2="290" y1="${y(hi).toFixed(1)}" y2="${y(hi).toFixed(1)}"/>${arcs}${nodes}`;
  }

  function describeCritical(r) {
    const c = r.counts;
    const check = c.other === 0 ? fill(labels['height.check'], { sum: c.sum }) : fill(labels['height.sum'], { sum: c.sum });
    criticalText.textContent = `${fill(labels['height.counts'], { min: c.min, saddle: c.saddle, max: c.max })} ${check}`;
  }

  function applyMode() {
    if (!shape) return;
    const r = shape.userData;
    const height = mode() === 'height';
    heightPanel.hidden = !height;
    legend.hidden = height;
    shape.geometry.setAttribute('color', new BufferAttribute(height ? r.heightColours : r.curvatureColours, 3));
    if (height) {
      drawPersistence();
      drawReeb(r);
      showTour();
    }
    applyLevel();
  }

  modes.forEach((m) => m.addEventListener('change', applyMode));
  level.addEventListener('input', applyLevel);
  reebShow.addEventListener('change', applyLevel);
  tau.addEventListener('input', () => {
    drawPersistence();
    applyLevel();
  });
  axisSelect.addEventListener('change', () => {
    if (!shape) return;
    const r = shape.userData;
    computeHeight(r);
    shape.geometry.setIndex(new BufferAttribute(r.filtration.indices, 1));
    placeMarkers(r);
    describeCritical(r);
    applyMode();
  });

  function show(r, name) {
    if (shape) {
      scene.remove(shape);
      shape.geometry.dispose();
      shape.material.dispose();
    }
    computeHeight(r);
    const geometry = new BufferGeometry();
    geometry.setAttribute('position', new BufferAttribute(r.positions, 3));
    // Triangles in the order of the filtration: a sublevel set is drawn as a prefix (setDrawRange).
    geometry.setIndex(new BufferAttribute(r.filtration.indices, 1));
    geometry.computeVertexNormals();
    const { out, ticks } = colours(r);
    r.curvatureColours = out;
    geometry.setAttribute('color', new BufferAttribute(out, 3));
    shape = new Mesh(geometry, new MeshStandardMaterial({ vertexColors: true, side: DoubleSide, roughness: 0.7 }));
    shape.userData = r;
    scene.add(shape);
    placeMarkers(r);
    describeCritical(r);
    canvas.setAttribute('aria-label', fill(labels.canvas, { name }));
    // Ticks sit at their quantile on each side of the centre: 50 % -> 25 % / 75 % of the bar, 90 % -> 5 % / 95 %.
    for (const el of legend.querySelectorAll('[data-tick]')) {
      const { at, value } = ticks[Number(el.dataset.tick)];
      const side = Number(el.dataset.side);
      el.textContent = `${side < 0 ? '−' : '+'}${value.toLocaleString(lang, { maximumSignificantDigits: 2 })}`;
      el.style.left = `${50 + side * at * 50}%`;
    }
    applyMode();
  }

  function invariants(r) {
    const inv = r.invariants;
    const yesNo = (b) => (b ? labels.yes : labels.no);
    const rows = [
      ['components', num(inv.components)],
      ['boundary', num(inv.boundaryLoops)],
      ['euler', num(inv.euler)],
      ['genus', inv.genus === null ? '—' : num(inv.genus)],
      ['orientable', yesNo(inv.orientable)],
      ['manifold', yesNo(inv.manifold)],
      ['total', `${num(turns(r.totalCurvature))} × 2π`],
    ];
    result.replaceChildren(...rows.map(([key, value]) => {
      const item = document.createElement('div');
      const dt = Object.assign(document.createElement('dt'), { textContent: labels[key] });
      const dd = Object.assign(document.createElement('dd'), { textContent: value });
      dd.dataset.field = key;
      item.append(dt, dd);
      return item;
    }));
  }

  function error(r) {
    const where = r.line > 0 ? fill(labels.atline, { line: r.line }) : labels.error;
    errorBox.textContent = `${where} ${labels[`error.${r.status}`] ?? r.message ?? ''}`;
    errorBox.hidden = false;
  }

  async function load(name, bytes) {
    errorBox.hidden = true;
    try {
      const l = await topo();
      const data = new Uint8Array(await bytes);
      const { value: r, ms } = timed(() => readTopology(l, data));
      if (!r.ok) return error(r);
      showTime('read', ms);
      r.lib = l;
      invariants(r);
      show(r, name);
    } catch {
      lib = undefined;
      error({ status: 'load', line: 0 });
    }
  }

  // --- inputs --------------------------------------------------------------------------------
  const samples = [...root.querySelectorAll('[data-sample]')];
  const loadSample = (button) => {
    samples.forEach((b) => b.setAttribute('aria-pressed', String(b === button)));
    const url = new URL(button.dataset.sample, document.baseURI);
    load(button.textContent.trim(), fetch(url).then((res) => {
      if (!res.ok) throw new Error(res.statusText);
      return res.arrayBuffer();
    }));
  };
  samples.forEach((b) => b.addEventListener('click', () => loadSample(b)));
  const input = root.querySelector('input[type=file]');
  input.addEventListener('change', () => {
    const file = input.files[0];
    if (!file) return;
    samples.forEach((b) => b.setAttribute('aria-pressed', 'false'));
    load(file.name, file.arrayBuffer());
  });
  loadSample(samples[0]);

  // --- hover: curvature of the nearest vertex --------------------------------------------------
  const ray = new Raycaster();
  const pointer = new Vector2();
  let pending = false;
  canvas.addEventListener('pointerleave', () => { tip.hidden = true; });
  canvas.addEventListener('pointermove', (e) => {
    if (!shape || pending) return;
    pending = true;
    requestAnimationFrame(() => {
      pending = false;
      const box = canvas.getBoundingClientRect();
      pointer.set(((e.clientX - box.left) / box.width) * 2 - 1, -((e.clientY - box.top) / box.height) * 2 + 1);
      ray.setFromCamera(pointer, camera);
      const hit = ray.intersectObject(shape)[0];
      if (!hit) {
        tip.hidden = true;
        return;
      }
      const { positions, curvature, defect, boundary, elevation: e } = shape.userData;
      const d2 = (v) => hit.point.distanceToSquared({ x: positions[3 * v], y: positions[3 * v + 1], z: positions[3 * v + 2] });
      const v = [hit.face.a, hit.face.b, hit.face.c].reduce((a, b) => (d2(a) <= d2(b) ? a : b));
      const degrees = num((defect[v] * 180) / Math.PI, 1);
      tip.textContent = mode() === 'height'
        ? fill(labels['tip.height'], { h: num(e.height[v] - e.height[e.order[0]], 2) })
        : boundary[v] ? fill(labels['tip.boundary'], { d: degrees }) : fill(labels.tip, { k: num(curvature[v], 2), d: degrees });
      tip.style.left = `${e.clientX - box.left + 12}px`;
      tip.style.top = `${e.clientY - box.top + 12}px`;
      tip.hidden = false;
    });
  });
}
