// lib-c demo: reads an OBJ or PLY file with the C library compiled to WebAssembly, loaded on first use.
import { loadMeshLib, readMesh } from './meshlib-api.js';

const root = document.querySelector('[data-mesh-demo]');
if (root) {
  const labels = JSON.parse(root.dataset.labels);
  const lang = document.documentElement.lang;
  const out = root.querySelector('[data-result]');
  const input = root.querySelector('input[type=file]');
  const drop = root.querySelector('[data-drop]');
  const fill = (text, vars) => text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));
  const num = (n) => n.toLocaleString(lang, { maximumFractionDigits: 3 });
  let lib;

  const el = (tag, attrs = {}, text = '') => {
    const node = document.createElement(tag);
    Object.entries(attrs).forEach(([k, v]) => node.setAttribute(k, v));
    node.textContent = text;
    return node;
  };

  function render(name, r) {
    out.replaceChildren();
    if (!r.ok) {
      const where = r.line > 0 ? fill(labels.atline, { line: r.line }) : labels.error;
      out.append(el('p', { class: 'notice demo-error' }, `${where} ${labels[`error.${r.status}`] ?? r.message ?? ''}`));
      return;
    }
    out.append(el('p', { class: 'meta' }, `${name} · ${r.format}`));
    const dl = el('dl', { class: 'demo-stats' });
    const rows = [
      ['vertices', num(r.vertices)], ['polygons', num(r.polygons)], ['triangles', num(r.triangles)],
      ['edges', num(r.edges)], ['boundary', num(r.boundaryEdges)], ['euler', num(r.euler)],
      ['bbox', `[${r.bbox.min.map(num).join(', ')}] → [${r.bbox.max.map(num).join(', ')}]`],
    ];
    for (const [key, value] of rows) {
      const item = el('div');
      item.append(el('dt', {}, labels[key]), el('dd', { 'data-field': key }, value));
      dl.append(item);
    }
    out.append(dl);
    let note;
    if (r.boundaryEdges > 0) note = fill(labels.open, { b: num(r.boundaryEdges) });
    else if (r.euler <= 2 && r.euler % 2 === 0) note = fill(labels.genus, { g: (2 - r.euler) / 2 });
    else note = fill(labels['closed.other'], { euler: num(r.euler) });
    if (r.triangles > 0) out.append(el('p', { class: 'notice' }, note));
  }

  async function show(name, bytes) {
    out.replaceChildren(el('p', { class: 'meta' }, labels.loading));
    try {
      lib ??= loadMeshLib((await import('./wasm/meshlib.js')).default);
      render(name, readMesh(await lib, new Uint8Array(await bytes)));
    } catch {
      lib = undefined;
      out.replaceChildren(el('p', { class: 'notice demo-error' }, labels['error.load']));
    }
  }

  const readFile = (file) => file && show(file.name, file.arrayBuffer());
  input.addEventListener('change', () => readFile(input.files[0]));
  root.querySelectorAll('[data-sample]').forEach((button) => button.addEventListener('click', () => {
    const url = new URL(button.dataset.sample, document.baseURI);
    show(url.pathname.split('/').pop(), fetch(url).then((res) => {
      if (!res.ok) throw new Error(res.statusText);
      return res.arrayBuffer();
    }));
  }));
  drop.addEventListener('dragover', (e) => { e.preventDefault(); drop.classList.add('is-over'); });
  drop.addEventListener('dragleave', () => drop.classList.remove('is-over'));
  drop.addEventListener('drop', (e) => {
    e.preventDefault();
    drop.classList.remove('is-over');
    readFile(e.dataTransfer.files[0]);
  });
}
