// Tic-tac-toe against the AI (D34): the AI's moves come from the move book computed by projects/morpion,
// loaded when the board comes into view. The board is 9 buttons with one tab stop (arrows move it, Enter or
// Space plays).
import { START, aiMove, isOver, legalMoves, loadBook, play, rowCol, toMove, winner, winningLine } from './morpion-api.js';

/** Pause before the AI answers, so its move can be seen. */
const AI_DELAY_MS = 350;

const root = document.querySelector('[data-morpion]');
if (root) {
  const labels = JSON.parse(root.dataset.labels);
  const board = root.querySelector('[data-board]');
  const cells = [...board.querySelectorAll('[data-cell]')];
  const status = root.querySelector('[data-status]');
  const levelSelect = root.querySelector('[data-level]');
  const fill = (text, vars) => text.replace(/\{(\w+)\}/g, (_, k) => String(vars[k]));
  const where = (cell) => fill(labels.square, rowCol(cell));
  let book = null;
  let pos = START;
  let human = 'X';
  let level = 'unbeatable';
  let last = -1;
  let busy = false;
  let focus = 4; // the centre

  function draw() {
    const line = winningLine(pos) ?? [];
    const moves = toMove(pos) === human && !busy ? legalMoves(pos) : [];
    cells.forEach((cell, i) => {
      const mark = pos[i] === '.' ? '' : pos[i];
      cell.dataset.mark = mark;
      cell.textContent = mark;
      cell.classList.toggle('is-legal', moves.includes(i));
      cell.classList.toggle('is-last', i === last);
      cell.classList.toggle('is-win', line.includes(i));
      const parts = [where(i), mark || labels.empty];
      if (i === last) parts.push(labels.last);
      cell.setAttribute('aria-label', parts.join(', '));
      cell.tabIndex = i === focus ? 0 : -1;
    });
  }

  // The AI answers if it is its turn; otherwise the person plays, or the game is over.
  function advance(message) {
    draw();
    if (isOver(pos)) {
      const w = winner(pos);
      const end = w === null ? labels['over-draw'] : w === human ? labels['over-win'] : labels['over-lose'];
      status.textContent = message ? `${message} ${end}` : end;
      return;
    }
    if (toMove(pos) !== human) {
      aiTurn();
      return;
    }
    status.textContent = message ? `${message} ${labels['your-turn']}` : labels['your-turn'];
  }

  function aiTurn() {
    busy = true;
    draw();
    status.textContent = labels['ai-thinking'];
    setTimeout(() => {
      const cell = aiMove(book, pos, level);
      pos = play(pos, cell);
      last = cell;
      busy = false;
      advance(fill(labels['ai-played'], { square: where(cell) }));
    }, AI_DELAY_MS);
  }

  function newGame() {
    human = root.querySelector('input[name="ttt-mark"]:checked').value;
    level = levelSelect.value;
    pos = START;
    last = -1;
    busy = false;
    advance();
  }

  function humanPlays(cell) {
    if (!book || busy || isOver(pos) || toMove(pos) !== human || !legalMoves(pos).includes(cell)) return;
    pos = play(pos, cell);
    last = cell;
    advance(fill(labels['you-played'], { square: where(cell) }));
  }

  cells.forEach((cell, i) => {
    cell.addEventListener('click', () => {
      focus = i;
      humanPlays(i);
    });
  });
  // Roving tab stop: arrows move the focus around the board (it stops at the edges), from whichever square
  // has the focus.
  board.addEventListener('focusin', (e) => {
    const i = cells.indexOf(e.target);
    if (i >= 0) focus = i;
  });
  board.addEventListener('keydown', (e) => {
    const moves = { ArrowLeft: [0, -1], ArrowRight: [0, 1], ArrowUp: [-1, 0], ArrowDown: [1, 0] };
    if (!(e.key in moves)) return;
    e.preventDefault();
    const [dr, dc] = moves[e.key];
    const row = Math.min(2, Math.max(0, Math.floor(focus / 3) + dr));
    const col = Math.min(2, Math.max(0, (focus % 3) + dc));
    focus = row * 3 + col;
    cells.forEach((c, i) => { c.tabIndex = i === focus ? 0 : -1; });
    cells[focus].focus();
  });
  root.querySelector('[data-new]').addEventListener('click', () => book && !busy && newGame());

  new IntersectionObserver(async (entries, observer) => {
    if (!entries.some((e) => e.isIntersecting)) return;
    observer.disconnect();
    try {
      book = await loadBook(new URL('./samples/morpion/book.json', import.meta.url));
      newGame();
    } catch {
      status.textContent = labels['error.load'];
    }
  }).observe(root);
}
