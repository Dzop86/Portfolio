// Othello against the AI (D28): the engine of projects/othello in WebAssembly, loaded when the board
// comes into view. The board is 64 buttons with one tab stop (arrows move it, Enter or Space plays).
import { BLACK, aiMove, loadOthello, pass, play, reset, squareName, state, undo } from './othello-api.js';

/** Pause before the AI answers, so its move can be seen. */
const AI_DELAY_MS = 350;

const root = document.querySelector('[data-othello]');
if (root) {
  const labels = JSON.parse(root.dataset.labels);
  const board = root.querySelector('[data-board]');
  const cells = [...board.querySelectorAll('[data-sq]')];
  const status = root.querySelector('[data-status]');
  const score = root.querySelector('[data-score]');
  const levelSelect = root.querySelector('[data-level]');
  const undoButton = root.querySelector('[data-undo]');
  const fill = (text, vars) => text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));
  let lib = null;
  let human = BLACK;
  let depth = 3;
  let last = -1;
  let busy = false;
  let focus = 19; // d3, the first legal move

  function draw() {
    const s = state(lib);
    cells.forEach((cell, sq) => {
      const c = s.cells[sq];
      const legal = s.legal[sq] && s.toMove === human && !busy;
      cell.dataset.disc = c === 1 ? 'black' : c === 2 ? 'white' : '';
      cell.classList.toggle('is-legal', legal);
      cell.classList.toggle('is-last', sq === last);
      const parts = [squareName(sq), c === 1 ? labels.black : c === 2 ? labels.white : labels.empty];
      if (legal) parts.push(labels.legal);
      if (sq === last) parts.push(labels.last);
      cell.setAttribute('aria-label', parts.join(', '));
      cell.tabIndex = sq === focus ? 0 : -1;
    });
    score.textContent = fill(labels.score, s);
    undoButton.disabled = busy || s.plies === 0;
    return s;
  }

  function announceEnd(s) {
    const mine = human === BLACK ? s.black : s.white;
    const theirs = human === BLACK ? s.white : s.black;
    status.textContent = mine > theirs ? labels['over-win'] : mine < theirs ? labels['over-lose'] : labels['over-draw'];
  }

  // Plays forced passes and the AI's moves until it is the person's turn or the game is over.
  function advance(message) {
    let s = draw();
    if (s.over) return announceEnd(s);
    if (s.mustPass) {
      const passer = s.toMove;
      pass(lib);
      s = draw();
      if (s.over) return announceEnd(s);
      if (passer === human) return aiTurn(labels['pass-you']);
      status.textContent = labels['pass-ai'];
      return undefined;
    }
    if (s.toMove !== human) return aiTurn(message);
    status.textContent = message ?? labels['your-turn'];
    return undefined;
  }

  function aiTurn(prefix) {
    busy = true;
    draw();
    status.textContent = prefix ? `${prefix} ${labels['ai-thinking']}` : labels['ai-thinking'];
    setTimeout(() => {
      const sq = aiMove(lib, depth);
      play(lib, sq);
      last = sq;
      busy = false;
      advance(fill(labels['ai-played'], { square: squareName(sq) }));
    }, AI_DELAY_MS);
  }

  function newGame() {
    human = Number(root.querySelector('input[name="oth-colour"]:checked').value);
    depth = Number(levelSelect.value);
    reset(lib);
    last = -1;
    busy = false;
    advance();
  }

  function humanPlays(sq) {
    if (!lib || busy) return;
    const s = state(lib);
    if (s.over || s.toMove !== human || !s.legal[sq]) return;
    play(lib, sq);
    last = sq;
    advance(fill(labels['you-played'], { square: squareName(sq) }));
  }

  cells.forEach((cell, sq) => {
    cell.addEventListener('click', () => {
      focus = sq;
      humanPlays(sq);
    });
  });
  // Roving tab stop: arrows move the focus around the board (it stops at the edges).
  board.addEventListener('keydown', (e) => {
    const moves = { ArrowLeft: [0, -1], ArrowRight: [0, 1], ArrowUp: [-1, 0], ArrowDown: [1, 0] };
    if (!(e.key in moves)) return;
    e.preventDefault();
    const [dr, dc] = moves[e.key];
    const row = Math.min(7, Math.max(0, Math.floor(focus / 8) + dr));
    const col = Math.min(7, Math.max(0, (focus % 8) + dc));
    focus = row * 8 + col;
    cells.forEach((c, sq) => { c.tabIndex = sq === focus ? 0 : -1; });
    cells[focus].focus();
  });
  root.querySelector('[data-new]').addEventListener('click', () => lib && newGame());
  undoButton.addEventListener('click', () => {
    if (!lib || busy) return;
    // Back to the person's last turn: undo the AI's answer (and any passes), then the person's move.
    do {
      if (!undo(lib)) break;
    } while (state(lib).toMove !== human || state(lib).mustPass);
    last = -1;
    advance();
  });

  new IntersectionObserver(async (entries, observer) => {
    if (!entries.some((e) => e.isIntersecting)) return;
    observer.disconnect();
    try {
      lib = await loadOthello((await import('./wasm/othello.js')).default);
      newGame();
    } catch {
      status.textContent = labels['error.load'];
    }
  }).observe(root);
}
