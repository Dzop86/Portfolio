// Interactive part of the generalized maps course (D24): select a dart of the cube net, move with
// alpha0, alpha1, alpha2, show the links of the current dart and one of its orbits; check the quiz.
// Bundled by esbuild with projects/gcartes into assets/gcourse.js.
import { ORBITS } from '../../projects/gcartes/src/gmap.js';
import { cubeNetMap, dartGeometry } from '../../projects/gcartes/src/net.js';

const SVG = 'http://www.w3.org/2000/svg';
const fill = (text, vars) => text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));

const figure = document.querySelector('[data-gmap-figure]');
if (figure) {
  const labels = JSON.parse(figure.dataset.labels);
  const { map, darts } = cubeNetMap();
  const geometry = dartGeometry(darts);
  const dartEls = [...figure.querySelectorAll('[data-dart]')];
  const links = figure.querySelector('[data-links]');
  const status = figure.querySelector('[data-status]');
  const orbitButtons = [...figure.querySelectorAll('[data-orbit]')];
  let current = null;
  let shown = null;

  const inv = map.invariants();
  figure.querySelector('[data-counts]').textContent = fill(labels.counts, inv);

  const middle = (d) => {
    const g = geometry[d];
    return [(g.start[0] + g.end[0]) / 2, (g.start[1] + g.end[1]) / 2];
  };

  function draw() {
    const orbit = current !== null && shown ? new Set(map.orbit(current, ORBITS[shown])) : new Set();
    dartEls.forEach((el, d) => {
      el.classList.toggle('is-current', d === current);
      el.classList.toggle('in-orbit', orbit.has(d));
    });
    links.replaceChildren();
    if (current !== null) {
      for (let i = 0; i <= 2; i++) {
        const other = map.alpha[i][current];
        if (other === current) continue;
        const [x1, y1] = middle(current);
        const [x2, y2] = middle(other);
        const line = document.createElementNS(SVG, 'line');
        Object.entries({ x1, y1, x2, y2, class: `gm-link gm-l${i}` }).forEach(([k, v]) => line.setAttribute(k, v));
        links.append(line);
      }
      const d = darts[current];
      let text = fill(labels.dart, { d: current, face: labels[`face.${d.square.key}`], letter: d.letter });
      if (shown) {
        const name = figure.querySelector(`[data-orbit="${shown}"]`).textContent;
        text += ` ${fill(labels.highlight, { name, d: current, n: orbit.size })}`;
      }
      status.textContent = text;
    }
    orbitButtons.forEach((b) => b.setAttribute('aria-pressed', String(b.dataset.orbit === shown)));
  }

  dartEls.forEach((el, d) => el.addEventListener('click', () => {
    current = d;
    draw();
  }));
  for (const button of figure.querySelectorAll('[data-alpha]')) {
    button.addEventListener('click', () => {
      current = map.alpha[Number(button.dataset.alpha)][current ?? 0];
      draw();
    });
  }
  for (const button of orbitButtons) {
    button.addEventListener('click', () => {
      shown = shown === button.dataset.orbit ? null : button.dataset.orbit;
      current ??= 0;
      draw();
    });
  }
}

// Decomposition of two squares: one step shown at a time (all of them without JavaScript).
const decompose = document.querySelector('[data-decompose]');
if (decompose) {
  const steps = [...decompose.querySelectorAll('[data-step]')];
  const buttons = [...decompose.querySelectorAll('[data-goto-step]')];
  const show = (k) => {
    steps.forEach((el) => { el.hidden = Number(el.dataset.step) !== k; });
    buttons.forEach((b) => b.setAttribute('aria-pressed', String(Number(b.dataset.gotoStep) === k)));
  };
  buttons.forEach((b) => b.addEventListener('click', () => show(Number(b.dataset.gotoStep))));
  show(0);
}

const quiz = document.querySelector('[data-gmap-course] [data-quiz]');
if (quiz) {
  const labels = JSON.parse(document.querySelector('[data-gmap-figure]').dataset.labels);
  quiz.addEventListener('submit', (e) => {
    e.preventDefault();
    const questions = [...quiz.querySelectorAll('fieldset')];
    let right = 0;
    for (const q of questions) {
      const chosen = q.querySelector('input:checked');
      const ok = chosen !== null && Number(chosen.value) === Number(q.dataset.answer);
      if (ok) right++;
      const feedback = q.querySelector('[data-feedback]');
      feedback.textContent = chosen === null ? labels.unanswered : ok ? labels.correct : labels.wrong;
      feedback.dataset.state = ok ? 'ok' : 'ko';
      feedback.hidden = false;
      q.querySelector('[data-explain]').hidden = false;
    }
    quiz.querySelector('[data-score]').textContent = fill(labels.score, { n: right, total: questions.length });
  });
}
