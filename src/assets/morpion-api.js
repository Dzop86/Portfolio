// Tic-tac-toe rules in JavaScript and the AI's moves read from the move book of projects/morpion (D34),
// shared by the page and the tests. A position is the Python one: nine cells, "X", "O" or ".", row by row.
// Book entries: "X", "O" or "=" for a finished game; otherwise [score for the player to move, best moves...].

export const EMPTY = '.';
export const START = EMPTY.repeat(9);
export const LINES = [
  [0, 1, 2], [3, 4, 5], [6, 7, 8], // rows
  [0, 3, 6], [1, 4, 7], [2, 5, 8], // columns
  [0, 4, 8], [2, 4, 6], // diagonals
];

/** The winning line, if any. */
export function winningLine(pos) {
  return LINES.find(([a, b, c]) => pos[a] !== EMPTY && pos[a] === pos[b] && pos[b] === pos[c]) ?? null;
}

/** "X" or "O" with three in a row, or null. */
export function winner(pos) {
  const line = winningLine(pos);
  return line ? pos[line[0]] : null;
}

export const isOver = (pos) => winner(pos) !== null || !pos.includes(EMPTY);

/** X always starts, so X moves whenever both have played as often. */
export const toMove = (pos) => ([...pos].filter((c) => c === 'X').length === [...pos].filter((c) => c === 'O').length ? 'X' : 'O');

/** Empty cells, in order; none once the game is over. */
export function legalMoves(pos) {
  if (isOver(pos)) return [];
  return [...pos].flatMap((c, i) => (c === EMPTY ? [i] : []));
}

/** The position after the player to move takes `cell`. */
export function play(pos, cell) {
  if (!legalMoves(pos).includes(cell)) throw new Error(`illegal move ${cell} in ${pos}`);
  return pos.slice(0, cell) + toMove(pos) + pos.slice(cell + 1);
}

/** The AI's move: any legal move for a beginner, one of the book's best moves otherwise. */
export function aiMove(book, pos, level, random = Math.random) {
  const entry = book.positions[pos];
  const moves = level === 'beginner' ? legalMoves(pos) : Array.isArray(entry) ? entry.slice(1) : [];
  if (moves.length === 0) throw new Error(`no move in ${pos}`);
  return moves[Math.floor(random() * moves.length)];
}

/** Row and column, 1 to 3, of a cell 0..8. */
export const rowCol = (cell) => ({ row: Math.floor(cell / 3) + 1, col: (cell % 3) + 1 });

export async function loadBook(url, fetchImpl = fetch) {
  const response = await fetchImpl(url);
  if (!response.ok) throw new Error(`${url}: ${response.status}`);
  return response.json();
}
