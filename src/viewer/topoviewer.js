// Topology viewer: a mesh coloured by Gaussian curvature, with its invariants, computed by the C++
// library of projects/topologie compiled to WebAssembly. Bundled with three.js at build time (D17).
import {
  AmbientLight, BufferAttribute, BufferGeometry, Color, DirectionalLight, DoubleSide, Mesh,
  MeshStandardMaterial, PerspectiveCamera, Raycaster, Scene, Vector2, WebGLRenderer,
} from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';
import { curvatureScale, interiorCurvature, loadTopo, readTopology, turns } from '../assets/topo-api.js';

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

  // Symmetric scale clamped at the 95th percentile of interior vertices, so that a few sharp corners
  // and the turning of the boundary do not wash out the rest.
  function colours(r) {
    const values = interiorCurvature(r.curvature, r.boundary, r.indices);
    const kmax = curvatureScale(r.curvature, r.boundary);
    const [neg, zero, pos] = ['--curv-neg', '--curv-zero', '--curv-pos'].map(token);
    const out = new Float32Array(3 * values.length);
    const c = new Color();
    values.forEach((k, i) => {
      const t = Math.max(-1, Math.min(1, k / kmax));
      c.copy(zero).lerp(t < 0 ? neg : pos, Math.abs(t));
      out.set([c.r, c.g, c.b], 3 * i);
    });
    return { out, kmax };
  }

  function show(r, name) {
    if (shape) {
      scene.remove(shape);
      shape.geometry.dispose();
      shape.material.dispose();
    }
    const geometry = new BufferGeometry();
    geometry.setAttribute('position', new BufferAttribute(r.positions, 3));
    geometry.setIndex(new BufferAttribute(r.indices, 1));
    geometry.computeVertexNormals();
    const { out, kmax } = colours(r);
    geometry.setAttribute('color', new BufferAttribute(out, 3));
    shape = new Mesh(geometry, new MeshStandardMaterial({ vertexColors: true, side: DoubleSide, roughness: 0.7 }));
    shape.userData = r;
    scene.add(shape);
    canvas.setAttribute('aria-label', fill(labels.canvas, { name }));
    legend.querySelector('[data-legend-min]').textContent = `−${num(kmax, 2)}`;
    legend.querySelector('[data-legend-max]').textContent = `+${num(kmax, 2)}`;
    legend.hidden = false;
    render();
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
      const r = readTopology(await topo(), new Uint8Array(await bytes));
      if (!r.ok) return error(r);
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
      const { positions, curvature, defect, boundary } = shape.userData;
      const d2 = (v) => hit.point.distanceToSquared({ x: positions[3 * v], y: positions[3 * v + 1], z: positions[3 * v + 2] });
      const v = [hit.face.a, hit.face.b, hit.face.c].reduce((a, b) => (d2(a) <= d2(b) ? a : b));
      const degrees = num((defect[v] * 180) / Math.PI, 1);
      tip.textContent = boundary[v] ? fill(labels['tip.boundary'], { d: degrees }) : fill(labels.tip, { k: num(curvature[v], 2), d: degrees });
      tip.style.left = `${e.clientX - box.left + 12}px`;
      tip.style.top = `${e.clientY - box.top + 12}px`;
      tip.hidden = false;
    });
  });
}
