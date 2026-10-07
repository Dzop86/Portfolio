// Ray tracer on its project page: the C++ engine in WebAssembly runs in a web worker
// (assets/raytracer-worker.js) and sends back the image after every pass; the page shows it in a canvas.
// Controls: scene, finish, orbit (sliders, or dragging the image), pause, default view.
const root = document.querySelector('[data-raytracer]');
if (root) start(root);

function start(root) {
  const labels = JSON.parse(root.dataset.labels);
  const fill = (text, values) => text.replace(/\{(\w+)\}/g, (_, k) => values[k]);
  const canvas = root.querySelector('canvas');
  const ctx = canvas.getContext('2d');
  const sceneSelect = root.querySelector('[data-scene]');
  const finishSelect = root.querySelector('[data-finish]');
  const sliders = Object.fromEntries([...root.querySelectorAll('[data-view]')].map((s) => [s.dataset.view, s]));
  const outputs = Object.fromEntries([...root.querySelectorAll('[data-view-value]')].map((o) => [o.dataset.viewValue, o]));
  const pause = root.querySelector('[data-pause]');
  const reset = root.querySelector('[data-reset]');
  const progress = root.querySelector('[data-progress]');
  const live = root.querySelector('[data-live]');
  const controls = [sceneSelect, finishSelect, ...Object.values(sliders), pause, reset];
  let defaultView = null;
  let paused = false;
  let generation = 0; // numbers each scene or view sent, so that frames of an older one are dropped

  if (typeof Worker === 'undefined') {
    live.textContent = labels.unsupported;
    return;
  }
  const worker = new Worker(new URL('./raytracer-worker.js', import.meta.url), { type: 'module' });
  const enable = (on) => controls.forEach((c) => { c.disabled = !on; });
  enable(false);

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
    root.dataset.samples = '0';
    paused = false;
    pause.textContent = labels.pause;
    pause.setAttribute('aria-pressed', 'false');
  };

  const sendScene = () => {
    const option = sceneSelect.selectedOptions[0];
    finishSelect.disabled = sceneSelect.value === 'spheres';
    restart();
    progress.textContent = labels.loading;
    live.textContent = fill(labels.rendering, { scene: describe() });
    worker.postMessage({
      type: 'scene',
      generation,
      mesh: option.dataset.mesh ? new URL(option.dataset.mesh, document.baseURI).href : null,
      finish: finishSelect.value,
      width: canvas.width,
      height: canvas.height,
    });
  };
  const sendView = () => {
    const view = currentView();
    showView(view);
    restart();
    worker.postMessage({ type: 'resume' });
    worker.postMessage({ type: 'view', view, generation });
  };

  worker.onmessage = ({ data }) => {
    if (data.type === 'loaded') {
      sendScene();
    } else if (data.type === 'ready') {
      defaultView = data.view;
      showView(data.view);
      enable(true);
      finishSelect.disabled = sceneSelect.value === 'spheres';
      canvas.setAttribute('aria-label', fill(labels.canvas, { scene: describe() }));
    } else if (data.type === 'frame') {
      if (data.generation !== generation) return;
      ctx.putImageData(new ImageData(new Uint8ClampedArray(data.pixels.buffer), canvas.width, canvas.height), 0, 0);
      root.dataset.samples = String(data.samples);
      progress.textContent = fill(labels.progress, { n: data.samples, max: 256, ms: Math.round(data.ms) });
    } else if (data.type === 'done') {
      if (data.generation !== generation) return;
      live.textContent = fill(labels.done, { scene: describe(), n: data.samples });
      pause.disabled = true;
    } else if (data.type === 'error') {
      live.textContent = fill(labels.error, { message: data.message });
      enable(true);
    }
  };
  worker.onerror = () => { live.textContent = labels.unsupported; };

  sceneSelect.addEventListener('change', () => { enable(false); sendScene(); });
  finishSelect.addEventListener('change', () => { enable(false); sendScene(); });
  for (const slider of Object.values(sliders)) slider.addEventListener('input', () => { pause.disabled = false; sendView(); });
  reset.addEventListener('click', () => {
    if (!defaultView) return;
    showView(defaultView);
    pause.disabled = false;
    sendView();
  });
  pause.addEventListener('click', () => {
    paused = !paused;
    pause.textContent = paused ? labels.resume : labels.pause;
    pause.setAttribute('aria-pressed', String(paused));
    worker.postMessage({ type: paused ? 'pause' : 'resume' });
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
    pause.disabled = false;
    sendView();
  });
  const stop = () => { drag = null; };
  canvas.addEventListener('pointerup', stop);
  canvas.addEventListener('pointercancel', stop);
}
