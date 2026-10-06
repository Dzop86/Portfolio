// Thin wrapper over the WebAssembly build of the Othello engine (projects/othello), shared by the page
// and the tests. The C side keeps the game and its history; this side reads it back as plain values.

export const BLACK = 0;
export const WHITE = 1;

export function loadOthello(createOthello, options = {}) {
  return createOthello(options);
}

/** "d3" style name of a square 0..63. */
export const squareName = (sq) => `${'abcdefgh'[sq % 8]}${Math.floor(sq / 8) + 1}`;

/** The whole game state: cells (0 empty, 1 black, 2 white), legal squares, side to move, counts. */
export function state(lib) {
  const cells = Array.from({ length: 64 }, (_, sq) => lib._othjs_cell(sq));
  const legal = cells.map((_, sq) => lib._othjs_is_legal(sq) === 1);
  return {
    cells,
    legal,
    toMove: lib._othjs_to_move(),
    mustPass: lib._othjs_must_pass() === 1,
    over: lib._othjs_game_over() === 1,
    black: lib._othjs_count(BLACK),
    white: lib._othjs_count(WHITE),
    plies: lib._othjs_plies(),
  };
}

export const reset = (lib) => lib._othjs_reset();
export const play = (lib, sq) => lib._othjs_play(sq) === 0;
export const pass = (lib) => lib._othjs_pass() === 0;
export const undo = (lib) => lib._othjs_undo() === 0;
export const aiMove = (lib, depth) => lib._othjs_ai_move(depth);
export const perft = (lib, depth) => lib._othjs_perft(depth);
