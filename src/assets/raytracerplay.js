// Ray tracer on its project page: the C++ engine in WebAssembly runs in several web workers
// (assets/raytracer-worker.js), each rendering its own bands of rows (raytracer-bands.js); the page puts the
// bands together in a canvas. Controls: scene, finish, number of workers, orbit (sliders, or dragging the
// image), pause, default view.
import { bandsFor, defaultWorkers, MAX_WORKERS, MAX_SAMPLES, PassCounter } from './raytracer-bands.js';

const root = document.querySelector('[data-raytracer]');
if (root) start(root);

function start(root) {
  const labels = JSON.parse(root.dataset.labels);
  const fill = (text, values) => text.replace(/\{(\w+)\}/g, (_, k) => values[k]);
  const canvas = root.querySelector('canvas');
  const ctx = canvas.getContext('2d');
  const sceneSelect = root.querySelector('[data-scene]');
  const finishSelect = root.querySelector('[data-finish]');
  const workersSelect = root.querySelector('[data-workers]');
  const sliders = Object.fromEntries([...root.querySelectorAll('[data-view]')].map((s) => [s.dataset.view, s]));
  const outputs = Object.fromEntries([...root.querySelectorAll('[data-view-value]')].map((o) => [o.dataset.viewValue, o]));
  const pause = root.querySelector('[data-pause]');
  const reset = root.querySelector('[data-reset]');
  const progress = root.querySelector('[data-progress]');
  const live = root.querySelector('[data-live]');
  const controls = [sceneSelect, finishSelect, workersSelect, ...Object.values(sliders), pause, reset];
  const { width, height } = canvas;
  let defaultView = null;
  let paused = false;
  let generation = 0; // numbers each scene or view sent, so that bands of an older one are dropped
  let counter = new PassCounter(height);
  let readies = 0, dones = 0, errorShown = false, passStart = 0, drawn = true;
  let showDefault = true; // a new scene shows its default view once ready
  let shown = { samples: 0, ms: 0 }; // the passes of the image drawn, and the time of the last one
  const image = new ImageData(width, height);

  if (typeof Worker === 'undefined') {
    live.textContent = labels.unsupported;
    return;
  }
  const enable = (on) => {
    controls.forEach((c) => { c.disabled = !on; });
    if (on) finishSelect.disabled = sceneSelect.value === 'spheres';
  };
  enable(false);

  // As many workers as the visitor asks for, up to the cores of the device.
  const most = Math.max(1, Math.min(MAX_WORKERS, navigator.hardwareConcurrency || 1));
  workersSelect.replaceChildren(...Array.from({ length: most }, (_, i) => new Option(String(i + 1), String(i + 1))));
  workersSelect.value = String(defaultWorkers(navigator.hardwareConcurrency));
  const pool = [];
  const active = () => pool.slice(0, Number(workersSelect.value));

  function grow(n) {
    while (pool.length > n) pool.pop().terminate();
    const loading = [];
    while (pool.length < n) {
      const worker = new Worker(new URL('./raytracer-worker.js', import.meta.url), { type: 'module' });
      loading.push(new Promise((resolve) => {
        worker.onmessage = ({ data }) => {
          if (data.type === 'loaded') {
            worker.onmessage = receive;
            resolve();
          }
        };
      }));
      worker.onerror = () => { live.textContent = labels.unsupported; };
      pool.push(worker);
    }
    return Promise.all(loading);
  }

  const describe = () => {
    const scene = sceneSelect.selectedOptions[0].textContent;
    const finish = sceneSelect.value === 'spheres' ? '' : `, ${finishSelect.selectedOptions[0].textContent.toLowerCase()}`;
    return `${scene}${finish}`;
  };
  const showView = (view) => {
    for (const [key, slider] of Object.entries(sliders)) {
      slider.value = String(Math.round(view[key] * 10) / 10);
      outputs[key].textContent = fill(labels[`unit.${key}`], { v: slider.value });
    }
  };
  const currentView = () => Object.fromEntries(Object.entries(sliders).map(([k, s]) => [k, Number(s.value)]));

  // A new scene or view starts again from no pass: the count must not show the previous image's.
  const restart = () => {
    generation += 1;
    counter = new PassCounter(height);
    dones = 0;
    errorShown = false;
    passStart = performance.now();
    shown = { samples: 0, ms: 0 };
    root.dataset.samples = '0';
    paused = false;
    pause.textContent = labels.pause;
    pause.setAttribute('aria-pressed', 'false');
  };

  // keepView: the same scene with another finish or number of workers keeps the camera where it is.
  async function sendScene(keepView) {
    enable(false);
    restart();
    const mine = generation;
    readies = 0;
    showDefault = !keepView || !defaultView;
    progress.textContent = labels.loading;
    live.textContent = fill(labels.rendering, { scene: describe() });
    await grow(Number(workersSelect.value));
    if (mine !== generation) return;
    const option = sceneSelect.selectedOptions[0];
    const mesh = option.dataset.mesh ? { url: new URL(option.dataset.mesh, document.baseURI).href } : null;
    const workers = active();
    workers.forEach((worker, k) => worker.postMessage({
      type: 'scene', generation, mesh, finish: finishSelect.value, width, height,
      bands: bandsFor(height, workers.length, k), view: showDefault ? null : currentView(),
    }));
  }

  function sendView() {
    const view = currentView();
    showView(view);
    restart();
    pause.disabled = false;
    for (const worker of active()) {
      worker.postMessage({ type: 'resume' });
      worker.postMessage({ type: 'view', view, generation });
    }
  }

  // The count and the progress change with the image drawn, not with the bands received.
  function draw() {
    drawn = true;
    ctx.putImageData(image, 0, 0);
    if (Number(root.dataset.samples) === shown.samples) return;
    root.dataset.samples = String(shown.samples);
    progress.textContent = fill(labels.progress, { n: shown.samples, max: MAX_SAMPLES, ms: shown.ms, workers: active().length });
  }

  function receive({ data }) {
    if (data.generation !== generation) return;
    const workers = active().length;
    if (data.type === 'ready') {
      if (++readies < workers) return;
      defaultView = data.defaultView;
      if (showDefault) showView(defaultView);
      enable(true);
      canvas.setAttribute('aria-label', fill(labels.canvas, { scene: describe() }));
    } else if (data.type === 'band') {
      image.data.set(data.pixels, data.y0 * width * 4);
      if (drawn) {
        drawn = false;
        requestAnimationFrame(draw);
      }
      if (counter.record(data.y0, data.samples)) {
        const now = performance.now();
        shown = { samples: counter.samples, ms: Math.round(now - passStart) };
        passStart = now;
      }
    } else if (data.type === 'done') {
      if (++dones < workers) return;
      live.textContent = fill(labels.done, { scene: describe(), n: counter.samples });
      pause.disabled = true;
    } else if (data.type === 'error') {
      if (errorShown) return;
      errorShown = true;
      live.textContent = fill(labels.error, { message: data.message });
      enable(true);
    }
  }

  sceneSelect.addEventListener('change', () => sendScene(false));
  finishSelect.addEventListener('change', () => sendScene(true));
  workersSelect.addEventListener('change', () => sendScene(true));
  for (const slider of Object.values(sliders)) slider.addEventListener('input', sendView);
  reset.addEventListener('click', () => {
    if (!defaultView) return;
    showView(defaultView);
    sendView();
  });
  pause.addEventListener('click', () => {
    paused = !paused;
    pause.textContent = paused ? labels.resume : labels.pause;
    pause.setAttribute('aria-pressed', String(paused));
    for (const worker of active()) worker.postMessage({ type: paused ? 'pause' : 'resume' });
  });

  // Dragging the image orbits the camera: horizontally the rotation, vertically the height (with a mouse
  // or a pen; on a touch screen vertical moves scroll the page, see touch-action in the CSS).
  let drag = null;
  canvas.addEventListener('pointerdown', (e) => {
    if (sliders.yaw.disabled) return;
    drag = { x: e.clientX, y: e.clientY, yaw: Number(sliders.yaw.value), pitch: Number(sliders.pitch.value), touch: e.pointerType === 'touch' };
    canvas.setPointerCapture(e.pointerId);
  });
  canvas.addEventListener('pointermove', (e) => {
    if (!drag) return;
    const scale = 360 / canvas.getBoundingClientRect().width;
    let yaw = drag.yaw - (e.clientX - drag.x) * scale;
    yaw = ((yaw + 180) % 360 + 360) % 360 - 180;
    sliders.yaw.value = String(Math.round(yaw));
    if (!drag.touch) {
      const pitch = drag.pitch + (e.clientY - drag.y) * scale * 0.5;
      sliders.pitch.value = String(Math.round(Math.min(Number(sliders.pitch.max), Math.max(Number(sliders.pitch.min), pitch))));
    }
    sendView();
  });
  const stop = () => { drag = null; };
  canvas.addEventListener('pointerup', stop);
  canvas.addEventListener('pointercancel', stop);

  sendScene(false);
}
