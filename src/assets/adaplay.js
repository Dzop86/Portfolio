// The Ada crossroads on its project page (D46): loads the automaton computed by the Ada program and replays
// it second by second (crossroads-core.js); this script only draws the lights, the waiting cars and the
// timing diagram, and sends the visitor's requests.
import { Crossroads, validate } from './crossroads-core.js';

const root = document.querySelector('[data-crossroads]');
if (root) start(root);

async function start(root) {
  const labels = JSON.parse(root.dataset.labels);
  const fill = (text, values) => text.replace(/\{(\w+)\}/g, (_, k) => values[k]);
  const map = root.querySelector('[data-map]');
  const clock = root.querySelector('[data-clock]');
  const bars = root.querySelector('[data-bars]');
  const live = root.querySelector('[data-live]');
  const note = root.querySelector('[data-note]');
  const pause = root.querySelector('[data-pause]');
  const step = root.querySelector('[data-step]');
  const restart = root.querySelector('[data-restart]');
  const speed = root.querySelector('[data-speed]');
  const requests = [...root.querySelectorAll('[data-request]')];
  const controls = [pause, step, restart, speed, ...requests];
  controls.forEach((c) => { c.disabled = true; });

  let c;
  try {
    const response = await fetch(new URL(root.dataset.crossroads, document.baseURI));
    if (!response.ok) throw new Error(response.statusText);
    c = new Crossroads(validate(await response.json()));
  } catch {
    live.textContent = labels.error;
    map.setAttribute('aria-label', labels.error);
    return;
  }
  note.append(` ${fill(labels.states, { n: c.states.length })}`);

  let paused = false;
  let timer = 0;
  let lastPhase = '';

  function draw() {
    const s = c.state;
    ['ns', 'ew'].forEach((axis, k) => {
      for (const lamp of map.querySelectorAll(`[data-light="${axis}"] [data-lamp]`)) {
        lamp.classList.toggle('is-on', lamp.dataset.lamp === s.lights[k]);
      }
      // An SVG element has no hidden property: the attribute itself (style.css hides it).
      map.querySelector(`[data-car="${axis}"]`).toggleAttribute('hidden', !s.pending[k]);
      const button = requests[k];
      button.setAttribute('aria-pressed', String(s.pending[k]));
    });
    map.setAttribute('aria-label', fill(labels.svg, { ns: labels[`color.${s.lights[0]}`], ew: labels[`color.${s.lights[1]}`] }));
    clock.textContent = fill(labels.status, { t: c.time, phase: labels[`phase.${s.phase}`], elapsed: s.elapsed });
    // A new phase is announced at normal speed, or second by second; faster, it would flood the reader.
    if (s.phase !== lastPhase && (paused || speed.value === '1')) live.textContent = labels[`phase.${s.phase}`];
    lastPhase = s.phase;
    drawChart();
  }

  // One column per second of history, north-south on top, east-west below.
  function drawChart() {
    const width = 580 / c.keep;
    bars.replaceChildren(...c.history.flatMap(({ lights }, i) => [0, 1].map((k) => {
      const rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
      rect.setAttribute('x', String(60 + i * width));
      rect.setAttribute('y', String(10 + k * 30));
      rect.setAttribute('width', String(width));
      rect.setAttribute('height', '20');
      rect.setAttribute('class', `cross-bar is-${lights[k]}`);
      return rect;
    })));
  }

  function second() {
    c.tick();
    draw();
  }

  function schedule() {
    clearInterval(timer);
    timer = paused ? 0 : setInterval(second, 1000 / Number(speed.value));
  }

  // A request on the axis that is green changes nothing in the Ada controller: say so rather than "waiting".
  requests.forEach((button, k) => button.addEventListener('click', () => {
    c.request(button.dataset.request);
    draw();
    live.textContent = fill(c.state.pending[k] ? labels.pending : labels.green, { axis: button.textContent });
  }));
  pause.addEventListener('click', () => {
    paused = !paused;
    pause.textContent = paused ? labels.resume : labels.pause;
    pause.setAttribute('aria-pressed', String(paused));
    schedule();
  });
  step.addEventListener('click', second);
  restart.addEventListener('click', () => {
    c.reset();
    draw();
  });
  speed.addEventListener('change', schedule);

  controls.forEach((ctl) => { ctl.disabled = false; });
  draw();
  schedule();
}
