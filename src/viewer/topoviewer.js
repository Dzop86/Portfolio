// Topology viewer: a mesh coloured by Gaussian curvature, or by its height with the height filtration and
// its critical points (sprint 36), with its invariants, all computed by the C++ library of
// projects/topologie compiled to WebAssembly. Bundled with three.js at build time (D17).
import {
  AmbientLight, BufferAttribute, BufferGeometry, Color, DirectionalLight, DoubleSide, Group, Mesh,
  MeshBasicMaterial, MeshStandardMaterial, PerspectiveCamera, Raycaster, Scene, SphereGeometry, Vector2, WebGLRenderer,
} from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import {
  AXES, criticalCounts, elevation, filtration, interiorCurvature, loadTopo, quantileScale, readTopology, turns,
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
    r.elevation = elevation(r.lib, AXES[axisSelect.value]);
    r.filtration = filtration(r.indices, r.elevation);
    r.heightColours = heightColours(r.elevation);
    r.counts = criticalCounts(r.elevation.critical);
  }

  // One sphere and one material per kind, shared by every marker and every mesh: clearing the group frees
  // nothing on the GPU because nothing new was allocated.
  const ball = new SphereGeometry(0.03, 12, 8);
  const markerMaterial = Object.fromEntries(Object.entries({ min: '--crit-min', saddle: '--crit-saddle', max: '--crit-max', other: '--crit-saddle' })
    .map(([kind, name]) => [kind, new MeshBasicMaterial({ color: token(name) })]));

  function placeMarkers(r) {
    markers.clear();
    for (const p of r.elevation.critical) {
      const m = new Mesh(ball, markerMaterial[p.kind]);
      m.position.set(r.positions[3 * p.vertex], r.positions[3 * p.vertex + 1], r.positions[3 * p.vertex + 2]);
      m.userData.rank = r.filtration.rank[p.vertex];
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
    for (const m of markers.children) m.visible = m.userData.rank <= rank;
    levelValue.textContent = fill(labels['height.value'], { h: num(h - lo, 2), max: num(hi - lo, 2) });
    sublevel.textContent = fill(labels.sublevel, { faces: num(faces), total: num(r.indices.length / 3), chi: rank < 0 ? 0 : euler[rank] });
    drawChi(r, h);
    render();
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
    applyLevel();
  }

  modes.forEach((m) => m.addEventListener('change', applyMode));
  level.addEventListener('input', applyLevel);
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
      const r = readTopology(l, new Uint8Array(await bytes));
      if (!r.ok) return error(r);
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
